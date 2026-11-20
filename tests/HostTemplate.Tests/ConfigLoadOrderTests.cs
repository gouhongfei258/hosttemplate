using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HostTemplate.Contracts;
using HostTemplate.Core.Configuration;
using HostTemplate.Core.Configuration.Loading;
using HostTemplate.Core.Configuration.Store;
using HostTemplate.Core.Updates;
using Xunit;

namespace HostTemplate.Tests
{
    public class ConfigLoadOrderTests : IDisposable
    {
        private readonly string _root;

        public ConfigLoadOrderTests()
        {
            _root = Path.Combine(Path.GetTempPath(), "HostTemplateConfigLoad", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_root);
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(_root, true);
            }
            catch (IOException)
            {
            }
        }

        [Fact]
        public async Task Resolve_UsesLocalConfig_AndDoesNotCallRemote()
        {
            var configDir = Path.Combine(_root, "config");
            WriteStation(configDir, "本地工站");
            var client = new ThrowingClient();
            var bootstrap = Bootstrap("http://127.0.0.1:9", "OP010", configDir);

            var result = await StationConfigLoader.ResolveAsync(bootstrap, Path.Combine(_root, "templates"), client, CancellationToken.None);

            Assert.Equal(ConfigLoadSource.Local, result.Source);
            Assert.Equal("本地工站", result.Manager.App.WorkStationName);
            Assert.Equal(0, client.ConfigCalls);
        }

        [Fact]
        public async Task Resolve_FetchesRemote_WhenLocalFilesAreMissing()
        {
            var configDir = Path.Combine(_root, "config");
            var remoteDir = Path.Combine(_root, "remote");
            WriteStation(remoteDir, "远端工站");
            var client = new ScriptedClient { Config = ConfigPackage.FromDirectory("OP010", remoteDir) };
            var bootstrap = Bootstrap("http://127.0.0.1:9", "OP010", configDir);

            var result = await StationConfigLoader.ResolveAsync(bootstrap, Path.Combine(_root, "empty-templates"), client, CancellationToken.None);

            Assert.Equal(ConfigLoadSource.Remote, result.Source);
            Assert.Equal(1, client.ConfigCalls);
            Assert.Equal("远端工站", result.Manager.App.WorkStationName);
            Assert.Equal("远端工站", new ConfigManager(configDir).LoadName());
        }

        [Fact]
        public async Task Resolve_CopiesTemplates_WhenRemoteIsEmpty()
        {
            var configDir = Path.Combine(_root, "config");
            var templates = Path.Combine(_root, "templates");
            WriteStation(templates, "模板工站");
            var client = new ScriptedClient { Config = new ConfigPackage() };
            var bootstrap = Bootstrap("http://127.0.0.1:9", "ST-07", configDir);

            var result = await StationConfigLoader.ResolveAsync(bootstrap, templates, client, CancellationToken.None);

            Assert.Equal(ConfigLoadSource.Template, result.Source);
            Assert.Equal("模板工站", result.Manager.App.WorkStationName);
            Assert.Equal("ST-07", result.Manager.App.EquId);
            Assert.True(File.Exists(Path.Combine(configDir, StationFiles.Plc)));
        }

        [Fact]
        public async Task Resolve_UsesCodeDefaults_WhenTemplatesAndRemoteAreMissing()
        {
            var configDir = Path.Combine(_root, "config");
            var client = new ScriptedClient { Error = new InvalidOperationException("服务不可达") };
            var bootstrap = Bootstrap("http://127.0.0.1:9", "OP010", configDir);

            var result = await StationConfigLoader.ResolveAsync(bootstrap, Path.Combine(_root, "no-such-templates"), client, CancellationToken.None);

            Assert.Equal(ConfigLoadSource.Template, result.Source);
            Assert.Equal("上位机模板工站", result.Manager.App.WorkStationName);
            Assert.True(ConfigManager.HasRequiredFiles(configDir));
        }

        [Fact]
        public async Task Resolve_SkipsRemote_WhenServiceUrlIsEmpty()
        {
            var configDir = Path.Combine(_root, "config");
            var client = new ThrowingClient();
            var bootstrap = Bootstrap(string.Empty, "OP010", configDir);

            var result = await StationConfigLoader.ResolveAsync(bootstrap, Path.Combine(_root, "templates"), client, CancellationToken.None);

            Assert.Equal(ConfigLoadSource.Template, result.Source);
            Assert.Equal(0, client.ConfigCalls);
            Assert.True(ConfigManager.HasRequiredFiles(configDir));
        }

        [Fact]
        public async Task Resolve_KeepsExistingFile_WhenOnlySomeFilesAreMissing()
        {
            var configDir = Path.Combine(_root, "config");
            Directory.CreateDirectory(configDir);
            var manager = new ConfigManager(configDir);
            manager.App.WorkStationName = "保留本地";
            Assert.True(manager.SaveApp(out var error), error);
            var client = new ScriptedClient { Error = new InvalidOperationException("离线") };
            var bootstrap = Bootstrap("http://127.0.0.1:9", string.Empty, configDir);

            var result = await StationConfigLoader.ResolveAsync(bootstrap, Path.Combine(_root, "templates"), client, CancellationToken.None);

            Assert.Equal(ConfigLoadSource.Template, result.Source);
            Assert.Equal("保留本地", result.Manager.App.WorkStationName);
            Assert.True(File.Exists(result.Manager.PlcPath));
            Assert.True(File.Exists(result.Manager.ApiPath));
        }

        [Fact]
        public void RemoteSync_DoesNotTouchLocal_UntilConfirmed()
        {
            var configDir = Path.Combine(_root, "config");
            WriteStation(configDir, "本地工站");
            var before = File.ReadAllText(Path.Combine(configDir, StationFiles.App));
            var remoteDir = Path.Combine(_root, "remote");
            WriteStation(remoteDir, "远端工站");
            var package = ConfigPackage.FromDirectory("OP010", remoteDir);

            var cancelled = RemoteConfigSync.Apply(false, package, configDir);

            Assert.True(cancelled.Cancelled);
            Assert.False(cancelled.Applied);
            Assert.Equal(before, File.ReadAllText(Path.Combine(configDir, StationFiles.App)));
            Assert.Contains("本地工站", before);
        }

        [Fact]
        public void RemoteSync_Overwrites_AfterConfirm()
        {
            var configDir = Path.Combine(_root, "config");
            WriteStation(configDir, "本地工站");
            var remoteDir = Path.Combine(_root, "remote");
            WriteStation(remoteDir, "远端工站");
            var users = Encoding.UTF8.GetBytes("<Users />");
            var package = ConfigPackage.FromDirectory("OP010", remoteDir);
            package.Files[StationFiles.Users] = users;

            var applied = RemoteConfigSync.Apply(true, package, configDir);

            Assert.True(applied.Applied);
            Assert.Contains("远端工站", File.ReadAllText(Path.Combine(configDir, StationFiles.App)));
            Assert.Equal(users, File.ReadAllBytes(Path.Combine(configDir, StationFiles.Users)));
        }

        [Fact]
        public void RemoteSync_DoesNotWrite_WhenPackageIsIncomplete()
        {
            var configDir = Path.Combine(_root, "config");
            WriteStation(configDir, "本地工站");
            var package = new ConfigPackage();
            package.Files[StationFiles.App] = Encoding.UTF8.GetBytes("<AppConfig />");

            var result = RemoteConfigSync.Apply(true, package, configDir);

            Assert.False(result.Applied);
            Assert.Contains("本地工站", File.ReadAllText(Path.Combine(configDir, StationFiles.App)));
        }

        [Fact]
        public void Bootstrap_RestoresFromStableCopy_WhenExeCopyIsMissing()
        {
            var exeDir = Path.Combine(_root, "app");
            var stable = Path.Combine(_root, "stable", "bootstrap.xml");
            Directory.CreateDirectory(exeDir);
            var first = BootstrapStore.LoadOrCreate(exeDir, stable, out _);
            first.StationId = "OP110";
            first.ServiceBaseUrl = "http://10.0.0.8:5080";
            first.ConfigDirectory = Path.Combine(_root, "station-config");
            XmlConfigStore.Save(Path.Combine(exeDir, BootstrapStore.FileName), first, out _);
            BootstrapStore.LoadOrCreate(exeDir, stable, out _);

            File.Delete(Path.Combine(exeDir, BootstrapStore.FileName));
            var restored = BootstrapStore.LoadOrCreate(exeDir, stable, out var message);

            Assert.Contains("恢复", message);
            Assert.Equal("OP110", restored.StationId);
            Assert.Equal("http://10.0.0.8:5080", restored.ServiceBaseUrl);
            Assert.Equal(Path.Combine(_root, "station-config"), restored.ConfigDirectory);
        }

        private static BootstrapConfig Bootstrap(string url, string stationId, string configDir)
        {
            return new BootstrapConfig
            {
                ServiceBaseUrl = url,
                StationId = stationId,
                ConfigDirectory = configDir,
                LogDirectory = Path.Combine(configDir, "..", "Logs"),
            };
        }

        private static void WriteStation(string directory, string stationName)
        {
            var manager = new ConfigManager(directory);
            manager.Load(out _);
            manager.App.WorkStationName = stationName;
            Assert.True(manager.SaveApp(out var error), error);
        }

        private sealed class ThrowingClient : IStationServiceClient
        {
            public int ConfigCalls { get; private set; }

            public Task<ConfigPackage> GetConfigAsync(string stationId, CancellationToken cancellationToken)
            {
                ConfigCalls++;
                throw new InvalidOperationException("不应该访问远端。");
            }

            public Task<VersionListResponse> GetVersionsAsync(string stationId, CancellationToken cancellationToken)
            {
                throw new InvalidOperationException("不应该访问远端。");
            }

            public Task<TargetVersion> GetLatestAsync(string stationId, CancellationToken cancellationToken)
            {
                throw new InvalidOperationException("不应该访问远端。");
            }
        }

        private sealed class ScriptedClient : IStationServiceClient
        {
            public int ConfigCalls { get; private set; }

            public ConfigPackage Config { get; set; }

            public Exception Error { get; set; }

            public Task<ConfigPackage> GetConfigAsync(string stationId, CancellationToken cancellationToken)
            {
                ConfigCalls++;
                if (Error != null)
                {
                    throw Error;
                }

                return Task.FromResult(Config);
            }

            public Task<VersionListResponse> GetVersionsAsync(string stationId, CancellationToken cancellationToken)
            {
                throw new InvalidOperationException("这个测试不该列版本。");
            }

            public Task<TargetVersion> GetLatestAsync(string stationId, CancellationToken cancellationToken)
            {
                throw new InvalidOperationException("这个测试不该查最新版本。");
            }
        }
    }

    internal static class ConfigManagerTestExtensions
    {
        public static string LoadName(this ConfigManager manager)
        {
            var reloaded = new ConfigManager(manager.ConfigDirectory);
            reloaded.Load(out _);
            return reloaded.App.WorkStationName;
        }
    }
}
