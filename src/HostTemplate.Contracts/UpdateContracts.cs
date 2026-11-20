using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace HostTemplate.Contracts
{
    /// <summary>
    /// 服务返回给工站的一个可安装版本。
    /// Policy 现在固定是 newest；以后按工站灰度时只改服务端选出的 Version / FeedUrl，客户端不用改。
    /// </summary>
    public sealed class TargetVersion
    {
        public string StationId { get; set; } = string.Empty;

        public string Version { get; set; } = string.Empty;

        public string Policy { get; set; } = string.Empty;

        public string FeedUrl { get; set; } = string.Empty;

        public string DownloadUrl { get; set; } = string.Empty;

        public string FileName { get; set; } = string.Empty;

        public long Size { get; set; }

        public DateTime PublishedAt { get; set; }
    }

    public sealed class VersionListResponse
    {
        public string StationId { get; set; } = string.Empty;

        public TargetVersion Latest { get; set; }

        public List<TargetVersion> Versions { get; set; } = new List<TargetVersion>();
    }

    /// <summary>
    /// 一份工站配置包：app.xml / plc.xml / api.xml，以及可选的 users.xml。
    /// </summary>
    public sealed class ConfigPackage
    {
        public string StationId { get; set; } = string.Empty;

        public Dictionary<string, byte[]> Files { get; set; } =
            new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);

        public bool HasRequiredFiles()
        {
            foreach (var name in StationFiles.Required)
            {
                if (!Files.TryGetValue(name, out var bytes) || bytes == null || bytes.Length == 0)
                {
                    return false;
                }
            }

            return true;
        }

        public byte[] ToZip()
        {
            using (var stream = new MemoryStream())
            {
                using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
                {
                    foreach (var pair in Files)
                    {
                        var name = Path.GetFileName(pair.Key);
                        if (!StationFiles.IsPackageFile(name) || pair.Value == null)
                        {
                            continue;
                        }

                        var entry = archive.CreateEntry(name, CompressionLevel.Optimal);
                        using (var entryStream = entry.Open())
                        {
                            entryStream.Write(pair.Value, 0, pair.Value.Length);
                        }
                    }
                }

                return stream.ToArray();
            }
        }

        public static ConfigPackage FromZip(string stationId, byte[] zip)
        {
            var package = new ConfigPackage { StationId = stationId ?? string.Empty };

            if (zip == null || zip.Length == 0)
            {
                return package;
            }

            using (var stream = new MemoryStream(zip))
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                foreach (var entry in archive.Entries)
                {
                    if (string.IsNullOrEmpty(entry.Name))
                    {
                        continue;
                    }

                    var name = Path.GetFileName(entry.FullName);
                    if (!StationFiles.IsPackageFile(name))
                    {
                        continue;
                    }

                    using (var entryStream = entry.Open())
                    using (var buffer = new MemoryStream())
                    {
                        entryStream.CopyTo(buffer);
                        package.Files[name] = buffer.ToArray();
                    }
                }
            }

            return package;
        }

        public static ConfigPackage FromDirectory(string stationId, string directory)
        {
            var package = new ConfigPackage { StationId = stationId ?? string.Empty };

            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
            {
                return package;
            }

            foreach (var name in StationFiles.Package)
            {
                var path = Path.Combine(directory, name);
                if (File.Exists(path))
                {
                    package.Files[name] = File.ReadAllBytes(path);
                }
            }

            return package;
        }

        /// <summary>
        /// 把包里的已知文件写到目录。overwrite 为 false 时已存在的文件不动。
        /// </summary>
        public void WriteToDirectory(string directory, bool overwrite)
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                throw new ArgumentException("配置目录为空。", nameof(directory));
            }

            Directory.CreateDirectory(directory);

            foreach (var pair in Files)
            {
                var name = Path.GetFileName(pair.Key);
                if (!StationFiles.IsPackageFile(name) || pair.Value == null)
                {
                    continue;
                }

                var destination = Path.Combine(directory, name);
                if (!overwrite && File.Exists(destination))
                {
                    continue;
                }

                var temp = destination + ".tmp";
                File.WriteAllBytes(temp, pair.Value);

                if (File.Exists(destination))
                {
                    File.Replace(temp, destination, destination + ".bak", ignoreMetadataErrors: true);
                }
                else
                {
                    File.Move(temp, destination);
                }
            }
        }
    }
}
