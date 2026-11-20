using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HostTemplate.Contracts;
using HostTemplate.Core.Runtime;
using HostTemplate.Core.Updates;
using Xunit;

namespace HostTemplate.Tests
{
    public class UpdateControllerTests
    {
        [Fact]
        public void FormatDisplay_KeepsSevenCharacterRevision()
        {
            Assert.Equal("1.0.42+abcdef0", AppVersion.FormatDisplay("1.0.42+abcdef012345", new Version(1, 0, 42)));
            Assert.Equal("1.0.0", AppVersion.FormatDisplay(null, new Version(1, 0, 0, 0)));
            Assert.Equal("1.0.3", AppVersion.FormatDisplay("1.0.3", new Version(1, 0, 3)));
        }

        [Fact]
        public void Compare_IgnoresBuildMetadata()
        {
            Assert.True(AppVersion.IsNewer("1.0.10", "1.0.9+abcdef0"));
            Assert.False(AppVersion.IsNewer("1.0.2+zzzzzzz", "v1.0.2"));
            Assert.True(AppVersion.Compare("1.0.1", "1.0.2") < 0);
        }

        [Fact]
        public async Task PullConfig_WithoutConfirm_DoesNotCallServiceOrChangeFiles()
        {
            var root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "HostTemplatePull", Guid.NewGuid().ToString("N"));
            System.IO.Directory.CreateDirectory(root);
            try
            {
                var manager = new Core.Configuration.ConfigManager(root);
                manager.Load(out _);
                manager.App.WorkStationName = "不能被覆盖";
                Assert.True(manager.SaveApp(out var error), error);
                var client = new FakeServiceClient();
                var controller = CreateController(client, idle: true, auto: false, current: "1.0.0");

                var result = await controller.PullConfigAsync(false, root, () => throw new InvalidOperationException("不该重载"), CancellationToken.None);

                Assert.Equal(UpdateStatus.Cancelled, result.Status);
                Assert.Equal(0, client.ConfigCalls);
                var reloaded = new Core.Configuration.ConfigManager(root);
                reloaded.Load(out _);
                Assert.Equal("不能被覆盖", reloaded.App.WorkStationName);
            }
            finally
            {
                System.IO.Directory.Delete(root, true);
            }
        }

        [Fact]
        public async Task PullConfig_AfterConfirm_OverwritesAndReloads()
        {
            var root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "HostTemplatePull", Guid.NewGuid().ToString("N"));
            var remote = System.IO.Path.Combine(root, "remote");
            var local = System.IO.Path.Combine(root, "local");
            System.IO.Directory.CreateDirectory(root);
            try
            {
                WriteNamed(local, "本地");
                WriteNamed(remote, "远端");
                var client = new FakeServiceClient { Config = ConfigPackage.FromDirectory("OP010", remote) };
                var controller = CreateController(client, idle: true, auto: false, current: "1.0.0");
                var reloaded = 0;

                var result = await controller.PullConfigAsync(true, local, () =>
                {
                    reloaded++;
                    return Task.CompletedTask;
                }, CancellationToken.None);

                Assert.Equal(UpdateStatus.Ready, result.Status);
                Assert.Equal(1, client.ConfigCalls);
                Assert.Equal(1, reloaded);
                Assert.Contains("远端", System.IO.File.ReadAllText(System.IO.Path.Combine(local, StationFiles.App)));
            }
            finally
            {
                System.IO.Directory.Delete(root, true);
            }
        }

        [Fact]
        public async Task Install_WithoutConfirm_DoesNotDownload()
        {
            var client = new FakeServiceClient();
            var installer = new FakeInstaller();
            var controller = CreateController(client, installer, idle: true, auto: false, current: "1.0.1");

            var result = await controller.InstallAsync("1.0.2", false, CancellationToken.None);

            Assert.Equal(UpdateStatus.Cancelled, result.Status);
            Assert.Equal(0, client.VersionCalls);
            Assert.Empty(installer.Downloaded);
            Assert.Empty(installer.Applied);
        }

        [Fact]
        public async Task Install_SelectedVersion_DefersUntilIdle_ThenApplies()
        {
            var client = Catalog("1.0.4", "1.0.2", "1.0.4");
            var installer = new FakeInstaller();
            var idle = new MutableIdle { Idle = false };
            var controller = CreateController(client, installer, idle, auto: false, current: "1.0.1");

            var waiting = await controller.InstallAsync("1.0.2", true, CancellationToken.None);

            Assert.Equal(UpdateStatus.Deferred, waiting.Status);
            Assert.Equal(new[] { "1.0.2" }, installer.Downloaded);
            Assert.Empty(installer.Applied);
            Assert.Contains("空闲", waiting.Message);

            idle.Idle = true;
            var applied = await controller.TickAsync(CancellationToken.None);

            Assert.Equal(UpdateStatus.Applied, applied.Status);
            Assert.Equal(new[] { "1.0.2" }, installer.Applied);
            Assert.Equal(0, client.LatestCalls);
        }

        [Fact]
        public async Task Install_AppliesImmediately_WhenStationIsIdle()
        {
            var client = Catalog("1.0.4", "1.0.4");
            var installer = new FakeInstaller();
            var controller = CreateController(client, installer, idle: true, auto: false, current: "1.0.1");

            var result = await controller.InstallAsync("1.0.4", true, CancellationToken.None);

            Assert.Equal(UpdateStatus.Applied, result.Status);
            Assert.Equal(new[] { "1.0.4" }, installer.Downloaded);
            Assert.Equal(new[] { "1.0.4" }, installer.Applied);
        }

        [Fact]
        public async Task AutoUpdate_UsesLatestEndpoint_NotTheHighestListedVersion()
        {
            var client = new FakeServiceClient
            {
                Latest = Version("1.0.5", "gray"),
                Versions = new VersionListResponse
                {
                    Latest = Version("1.0.5", "gray"),
                    Versions = new List<TargetVersion>
                    {
                        Version("1.0.9", string.Empty),
                        Version("1.0.5", "gray"),
                    },
                },
            };
            var installer = new FakeInstaller();
            var controller = CreateController(client, installer, idle: false, auto: true, current: "1.0.4", interval: TimeSpan.Zero);

            var waiting = await controller.TickAsync(CancellationToken.None);

            Assert.Equal(UpdateStatus.Deferred, waiting.Status);
            Assert.Equal(1, client.LatestCalls);
            Assert.Equal(0, client.VersionCalls);
            Assert.Equal(new[] { "1.0.5" }, installer.Downloaded);
            Assert.Empty(installer.Applied);

            controller = null;
            var idleController = CreateController(client, installer, idle: true, auto: true, current: "1.0.4", interval: TimeSpan.Zero);
            installer.Downloaded.Clear();
            var applied = await idleController.TickAsync(CancellationToken.None);

            Assert.Equal(UpdateStatus.Applied, applied.Status);
            Assert.Equal(new[] { "1.0.5" }, installer.Applied);
        }

        [Fact]
        public async Task AutoUpdate_DoesNotDownload_WhenTargetIsNotNewer()
        {
            var client = new FakeServiceClient { Latest = Version("1.0.2", "newest") };
            var installer = new FakeInstaller();
            var controller = CreateController(client, installer, idle: true, auto: true, current: "1.0.2+abcdef0", interval: TimeSpan.Zero);

            var result = await controller.TickAsync(CancellationToken.None);

            Assert.Equal(UpdateStatus.Noop, result.Status);
            Assert.Empty(installer.Downloaded);
            Assert.Empty(installer.Applied);
        }

        [Fact]
        public async Task AutoUpdate_StaysOff_WhenTheSettingIsDisabled()
        {
            var client = new FakeServiceClient { Latest = Version("9.0.0", "newest") };
            var installer = new FakeInstaller();
            var controller = CreateController(client, installer, idle: true, auto: false, current: "1.0.0", interval: TimeSpan.Zero);

            var result = await controller.TickAsync(CancellationToken.None);

            Assert.Equal(UpdateStatus.Noop, result.Status);
            Assert.Equal(0, client.LatestCalls);
            Assert.Empty(installer.Downloaded);
        }

        [Fact]
        public void TryPick_AllowsRollback_AndRejectsTheRunningVersion()
        {
            var catalog = new VersionListResponse
            {
                Versions = new List<TargetVersion> { Version("1.0.1", string.Empty), Version("1.0.4", "newest") },
            };

            Assert.True(VersionSelection.TryPick(catalog, "1.0.1", "1.0.4", out var picked, out _));
            Assert.Equal("1.0.1", picked.Version);
            Assert.False(VersionSelection.TryPick(catalog, "1.0.4", "1.0.4+abc", out _, out var error));
            Assert.Contains("已经是", error);
            Assert.False(VersionSelection.ShouldAutoDownload(Version("1.0.1", "newest"), "1.0.4"));
            Assert.True(VersionSelection.ShouldAutoDownload(Version("1.0.5", "gray"), "1.0.4"));
        }

        private static void WriteNamed(string directory, string name)
        {
            var manager = new Core.Configuration.ConfigManager(directory);
            manager.Load(out _);
            manager.App.WorkStationName = name;
            Assert.True(manager.SaveApp(out var error), error);
        }

        private static FakeServiceClient Catalog(string latest, params string[] versions)
        {
            var list = new VersionListResponse { Latest = Version(latest, "newest") };
            foreach (var version in versions)
            {
                list.Versions.Add(Version(version, string.Equals(version, latest, StringComparison.Ordinal) ? "newest" : string.Empty));
            }

            return new FakeServiceClient { Versions = list, Latest = list.Latest };
        }

        private static TargetVersion Version(string version, string policy)
        {
            return new TargetVersion
            {
                StationId = "OP010",
                Version = version,
                Policy = policy,
                FeedUrl = "http://127.0.0.1/api/feeds/" + version + "/",
                DownloadUrl = "http://127.0.0.1/api/feeds/" + version + "/HostTemplate-" + version + "-full.nupkg",
                FileName = "HostTemplate-" + version + "-full.nupkg",
            };
        }

        private static StationUpdateController CreateController(FakeServiceClient client, bool idle, bool auto, string current)
        {
            return CreateController(client, new FakeInstaller(), idle, auto, current, TimeSpan.FromMinutes(1));
        }

        private static StationUpdateController CreateController(
            FakeServiceClient client,
            FakeInstaller installer,
            bool idle,
            bool auto,
            string current,
            TimeSpan? interval = null)
        {
            return CreateController(client, installer, new MutableIdle { Idle = idle }, auto, current, interval);
        }

        private static StationUpdateController CreateController(
            FakeServiceClient client,
            FakeInstaller installer,
            MutableIdle idle,
            bool auto,
            string current,
            TimeSpan? interval = null)
        {
            return new StationUpdateController(
                "http://127.0.0.1:5080",
                () => "OP010",
                () => auto,
                () => current,
                client,
                idle,
                installer,
                interval ?? TimeSpan.FromMinutes(1));
        }

        private sealed class MutableIdle : IStationIdle
        {
            public bool Idle { get; set; }

            public bool IsIdle => Idle;
        }

        private sealed class FakeInstaller : IUpdateInstaller
        {
            public bool IsInstalled { get; set; } = true;

            public List<string> Downloaded { get; } = new List<string>();

            public List<string> Applied { get; } = new List<string>();

            public Task DownloadAsync(TargetVersion version, CancellationToken cancellationToken)
            {
                Downloaded.Add(version.Version);
                return Task.CompletedTask;
            }

            public void ApplyAndRestart(TargetVersion version)
            {
                Applied.Add(version.Version);
            }
        }

        private sealed class FakeServiceClient : IStationServiceClient
        {
            public int ConfigCalls { get; private set; }

            public int VersionCalls { get; private set; }

            public int LatestCalls { get; private set; }

            public ConfigPackage Config { get; set; }

            public VersionListResponse Versions { get; set; } = new VersionListResponse();

            public TargetVersion Latest { get; set; }

            public Task<ConfigPackage> GetConfigAsync(string stationId, CancellationToken cancellationToken)
            {
                ConfigCalls++;
                return Task.FromResult(Config);
            }

            public Task<VersionListResponse> GetVersionsAsync(string stationId, CancellationToken cancellationToken)
            {
                VersionCalls++;
                return Task.FromResult(Versions);
            }

            public Task<TargetVersion> GetLatestAsync(string stationId, CancellationToken cancellationToken)
            {
                LatestCalls++;
                return Task.FromResult(Latest);
            }
        }
    }
}
