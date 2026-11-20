using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using HostTemplate.Backend.Models;
using HostTemplate.Contracts;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace HostTemplate.Backend.Infrastructure
{
    /// <summary>
    /// 配置和安装包都落在磁盘上。重启服务不会丢。
    /// packages/{version}/ 是版本库，stations/{id}/ 是工站配置，inbox/ 是投放目录。
    /// </summary>
    public sealed class FileUpdateStore
    {
        private static readonly Regex PackageName = new Regex(
            @"^(?<id>.+)-(?<ver>\d+\.\d+\.\d+(?:\.\d+)?)-full\.nupkg$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        private static readonly JsonSerializerOptions MetaJson = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
        };

        private readonly object _gate = new object();
        private readonly string _packageId;

        public FileUpdateStore(IOptions<UpdateServiceOptions> options, IHostEnvironment environment)
        {
            var configured = options?.Value ?? new UpdateServiceOptions();
            _packageId = string.IsNullOrWhiteSpace(configured.PackageId) ? "HostTemplate" : configured.PackageId.Trim();

            var root = string.IsNullOrWhiteSpace(configured.DataRoot) ? "data" : configured.DataRoot.Trim();
            if (!Path.IsPathRooted(root))
            {
                var contentRoot = environment?.ContentRootPath ?? AppContext.BaseDirectory;
                root = Path.Combine(contentRoot, root);
            }

            DataRoot = Path.GetFullPath(root);
            EnsureLayout();
        }

        public string DataRoot { get; }

        public string PackageId => _packageId;

        public string StationsRoot => Path.Combine(DataRoot, "stations");

        public string PackagesRoot => Path.Combine(DataRoot, "packages");

        public string InboxRoot => Path.Combine(DataRoot, "inbox");

        public void EnsureLayout()
        {
            Directory.CreateDirectory(StationsRoot);
            Directory.CreateDirectory(PackagesRoot);
            Directory.CreateDirectory(InboxRoot);
            Directory.CreateDirectory(Path.Combine(InboxRoot, "stations"));
            Directory.CreateDirectory(Path.Combine(DataRoot, "inbox-processed"));
        }

        public int ImportInbox()
        {
            lock (_gate)
            {
                EnsureLayout();
                var imported = 0;
                imported += ImportPackageFiles(InboxRoot);
                imported += ImportPackageDirectories(InboxRoot);
                imported += ImportStationInbox();
                return imported;
            }
        }

        public void SavePackage(string version, string fileName, byte[] content)
        {
            if (!IsSafeVersion(version))
            {
                throw new InvalidOperationException("版本号不合法：" + version);
            }

            if (content == null || content.Length == 0)
            {
                throw new InvalidOperationException("安装包是空的。");
            }

            fileName = Path.GetFileName(fileName ?? string.Empty);
            if (string.IsNullOrWhiteSpace(fileName) || !fileName.EndsWith(".nupkg", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("只接受 Velopack 的 .nupkg 全量包。");
            }

            lock (_gate)
            {
                var directory = PackageDirectory(version);
                Directory.CreateDirectory(directory);

                WritePackageFiles(directory, version, fileName, content);
            }
        }

        public IReadOnlyList<PackageRecord> ListPackages()
        {
            lock (_gate)
            {
                ImportInboxUnlocked();
                return ScanPackages();
            }
        }

        public bool TryGetPackage(string version, out PackageRecord record)
        {
            record = null;
            if (!IsSafeVersion(version))
            {
                return false;
            }

            lock (_gate)
            {
                ImportInboxUnlocked();
                foreach (var package in ScanPackages())
                {
                    if (string.Equals(package.Version, version, StringComparison.OrdinalIgnoreCase))
                    {
                        record = package;
                        return true;
                    }
                }
            }

            return false;
        }

        public string GetPackagePath(PackageRecord record)
        {
            if (record == null || !IsSafeVersion(record.Version))
            {
                return null;
            }

            var path = Path.Combine(PackageDirectory(record.Version), record.FileName);
            return File.Exists(path) ? path : null;
        }

        public string BuildFeedJson(PackageRecord record)
        {
            return VelopackFeed.ToJson(_packageId, record);
        }

        public void SaveStationConfig(string stationId, ConfigPackage package)
        {
            if (!StationFiles.IsSafeId(stationId))
            {
                throw new InvalidOperationException("工站编号不合法。");
            }

            if (package == null || !package.HasRequiredFiles())
            {
                throw new InvalidOperationException("配置包缺少 app.xml、plc.xml 或 api.xml。");
            }

            lock (_gate)
            {
                var directory = StationDirectory(stationId);
                Directory.CreateDirectory(directory);
                package.WriteToDirectory(directory, overwrite: true);
            }
        }

        public ConfigPackage ReadStationConfig(string stationId)
        {
            if (!StationFiles.IsSafeId(stationId))
            {
                return null;
            }

            lock (_gate)
            {
                ImportStationInbox();
                var package = ConfigPackage.FromDirectory(stationId, StationDirectory(stationId));
                return package.Files.Count == 0 ? null : package;
            }
        }

        public static bool IsSafeVersion(string version)
        {
            return AppVersion.TryParse(version, out _) && StationFiles.IsSafeId(version);
        }

        public static bool TryParsePackageFileName(string fileName, out string version)
        {
            version = null;
            var name = Path.GetFileName(fileName ?? string.Empty);
            var match = PackageName.Match(name);
            if (!match.Success)
            {
                return false;
            }

            version = match.Groups["ver"].Value;
            return IsSafeVersion(version);
        }

        private void ImportInboxUnlocked()
        {
            ImportPackageFiles(InboxRoot);
            ImportPackageDirectories(InboxRoot);
            ImportStationInbox();
        }

        private int ImportPackageFiles(string directory)
        {
            if (!Directory.Exists(directory))
            {
                return 0;
            }

            var count = 0;
            foreach (var file in Directory.GetFiles(directory, "*.nupkg"))
            {
                if (TryImportPackageFile(file, null))
                {
                    count++;
                }
            }

            return count;
        }

        private int ImportPackageDirectories(string inbox)
        {
            if (!Directory.Exists(inbox))
            {
                return 0;
            }

            var count = 0;
            foreach (var dir in Directory.GetDirectories(inbox))
            {
                var name = Path.GetFileName(dir);
                if (string.Equals(name, "stations", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                foreach (var file in Directory.GetFiles(dir, "*.nupkg"))
                {
                    var version = IsSafeVersion(name) ? name : null;
                    if (TryImportPackageFile(file, version))
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        private bool TryImportPackageFile(string file, string versionHint)
        {
            var fileName = Path.GetFileName(file);
            if (!TryParsePackageFileName(fileName, out var fromName) && string.IsNullOrWhiteSpace(versionHint))
            {
                return false;
            }

            var version = !string.IsNullOrWhiteSpace(versionHint) ? versionHint : fromName;
            if (!string.IsNullOrWhiteSpace(fromName)
                && !string.IsNullOrWhiteSpace(versionHint)
                && !string.Equals(fromName, versionHint, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var bytes = File.ReadAllBytes(file);
            SavePackageUnlocked(version, fileName, bytes);
            MoveProcessed(file);
            return true;
        }

        private void SavePackageUnlocked(string version, string fileName, byte[] content)
        {
            var directory = PackageDirectory(version);
            Directory.CreateDirectory(directory);
            WritePackageFiles(directory, version, fileName, content);
        }

        private static void WritePackageFiles(string directory, string version, string fileName, byte[] content)
        {
            var path = Path.Combine(directory, fileName);
            var temp = path + ".tmp";
            File.WriteAllBytes(temp, content);
            File.Move(temp, path, overwrite: true);

            foreach (var old in Directory.GetFiles(directory, "*.nupkg"))
            {
                if (!string.Equals(old, path, StringComparison.OrdinalIgnoreCase))
                {
                    File.Delete(old);
                }
            }

            var record = new PackageRecord
            {
                Version = version,
                FileName = fileName,
                Size = content.Length,
                Sha1 = PackageHash.Sha1(content),
                Sha256 = PackageHash.Sha256(content),
                PublishedAtUtc = DateTime.UtcNow,
            };
            WriteMeta(directory, record);
        }

        private int ImportStationInbox()
        {
            var root = Path.Combine(InboxRoot, "stations");
            if (!Directory.Exists(root))
            {
                return 0;
            }

            var count = 0;
            foreach (var dir in Directory.GetDirectories(root))
            {
                var stationId = Path.GetFileName(dir);
                if (!StationFiles.IsSafeId(stationId))
                {
                    continue;
                }

                var package = ConfigPackage.FromDirectory(stationId, dir);
                if (!package.HasRequiredFiles())
                {
                    continue;
                }

                var destination = StationDirectory(stationId);
                Directory.CreateDirectory(destination);
                package.WriteToDirectory(destination, overwrite: true);
                MoveProcessed(dir);
                count++;
            }

            return count;
        }

        private List<PackageRecord> ScanPackages()
        {
            var list = new List<PackageRecord>();
            if (!Directory.Exists(PackagesRoot))
            {
                return list;
            }

            foreach (var dir in Directory.GetDirectories(PackagesRoot))
            {
                var version = Path.GetFileName(dir);
                if (!IsSafeVersion(version))
                {
                    continue;
                }

                string nupkg = null;
                foreach (var file in Directory.GetFiles(dir, "*.nupkg"))
                {
                    nupkg = file;
                    break;
                }

                if (nupkg == null)
                {
                    continue;
                }

                list.Add(LoadOrCreateRecord(version, nupkg));
            }

            list.Sort((left, right) => AppVersion.Compare(right.Version, left.Version));
            return list;
        }

        private PackageRecord LoadOrCreateRecord(string version, string nupkgPath)
        {
            var metaPath = Path.Combine(Path.GetDirectoryName(nupkgPath) ?? string.Empty, "release.json");
            if (File.Exists(metaPath))
            {
                try
                {
                    var meta = JsonSerializer.Deserialize<PackageMeta>(File.ReadAllText(metaPath), MetaJson);
                    if (meta != null
                        && string.Equals(meta.FileName, Path.GetFileName(nupkgPath), StringComparison.OrdinalIgnoreCase)
                        && meta.Size == new FileInfo(nupkgPath).Length)
                    {
                        meta.Version = version;
                        return meta.ToRecord();
                    }
                }
                catch (JsonException)
                {
                }
                catch (IOException)
                {
                }
            }

            var info = new FileInfo(nupkgPath);
            var record = new PackageRecord
            {
                Version = version,
                FileName = info.Name,
                Size = info.Length,
                Sha1 = PackageHash.Sha1File(nupkgPath),
                Sha256 = PackageHash.Sha256File(nupkgPath),
                PublishedAtUtc = info.CreationTimeUtc,
            };
            WriteMeta(info.DirectoryName, record);
            return record;
        }

        private static void WriteMeta(string directory, PackageRecord record)
        {
            var path = Path.Combine(directory, "release.json");
            File.WriteAllText(path, JsonSerializer.Serialize(PackageMeta.FromRecord(record), MetaJson));
        }

        private void MoveProcessed(string path)
        {
            var processed = Path.Combine(DataRoot, "inbox-processed");
            Directory.CreateDirectory(processed);
            var name = Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            var destination = Path.Combine(processed, DateTime.UtcNow.ToString("yyyyMMddHHmmssfff") + "-" + name);

            if (Directory.Exists(path))
            {
                Directory.Move(path, destination);
                return;
            }

            File.Move(path, destination, overwrite: true);
        }

        private string PackageDirectory(string version)
        {
            return Path.Combine(PackagesRoot, version);
        }

        private string StationDirectory(string stationId)
        {
            return Path.Combine(StationsRoot, stationId);
        }
    }
}
