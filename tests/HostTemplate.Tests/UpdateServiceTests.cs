using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HostTemplate.Contracts;
using HostTemplate.Core.Configuration;
using HostTemplate.Core.Updates;
using HostTemplate.UpdateService;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Velopack;
using Xunit;

namespace HostTemplate.Tests
{
    public class UpdateServiceTests : IDisposable
    {
        private readonly string _root;

        public UpdateServiceTests()
        {
            _root = Path.Combine(Path.GetTempPath(), "HostTemplateUpdateService", Guid.NewGuid().ToString("N"));
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
        public async Task Config_RoundTrips_AndSurvivesANewProcess()
        {
            var data = Path.Combine(_root, "data-a");
            var stationDir = Path.Combine(_root, "station");
            WriteStation(stationDir, "服务工站");
            var package = ConfigPackage.FromDirectory("OP010", stationDir);

            using (var factory = new ServiceFactory(data, token: null))
            {
                var http = factory.CreateClient();
                var body = new ByteArrayContent(package.ToZip());
                body.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
                var put = await http.PutAsync("/api/stations/OP010/config", body);
                Assert.Equal(HttpStatusCode.OK, put.StatusCode);

                var client = new HttpStationServiceClient(http);
                var loaded = await client.GetConfigAsync("OP010", CancellationToken.None);
                Assert.NotNull(loaded);
                Assert.True(loaded.HasRequiredFiles());
                loaded.WriteToDirectory(Path.Combine(_root, "out"), overwrite: true);
                var manager = new ConfigManager(Path.Combine(_root, "out"));
                manager.Load(out _);
                Assert.Equal("服务工站", manager.App.WorkStationName);
            }

            using (var restarted = new ServiceFactory(data, token: null))
            {
                var again = await restarted.CreateClient().GetAsync("/api/stations/OP010/config");
                Assert.Equal(HttpStatusCode.OK, again.StatusCode);
                Assert.True(ConfigPackage.FromZip("OP010", await again.Content.ReadAsByteArrayAsync()).HasRequiredFiles());
            }
        }

        [Fact]
        public async Task Versions_ListEveryPackage_AndLatestIsNewestUntilPolicyChanges()
        {
            var data = Path.Combine(_root, "data-b");
            using (var factory = new ServiceFactory(data, token: null))
            {
                var http = factory.CreateClient();
                await PutPackage(http, "1.0.2", Encoding.UTF8.GetBytes("pkg-old"));
                await PutPackage(http, "1.0.9", Encoding.UTF8.GetBytes("pkg-new"));

                var client = new HttpStationServiceClient(http);
                var list = await client.GetVersionsAsync("OP010", CancellationToken.None);
                var latest = await client.GetLatestAsync("OP010", CancellationToken.None);

                Assert.Equal(2, list.Versions.Count);
                Assert.Equal("1.0.9", list.Versions[0].Version);
                Assert.Equal("1.0.9", latest.Version);
                Assert.Equal("newest", latest.Policy);
                Assert.Contains("/api/feeds/1.0.9/", latest.FeedUrl);
                Assert.EndsWith("HostTemplate-1.0.9-full.nupkg", latest.DownloadUrl);

                var feed = await http.GetStringAsync("/api/feeds/1.0.2/releases.win.json");
                var parsed = VelopackAssetFeed.FromJson(feed);
                Assert.Single(parsed.Assets);
                Assert.Equal("1.0.2", parsed.Assets[0].Version.ToString());
                Assert.Equal(VelopackAssetType.Full, parsed.Assets[0].Type);
                Assert.Equal("HostTemplate-1.0.2-full.nupkg", parsed.Assets[0].FileName);
                Assert.Equal(Encoding.UTF8.GetByteCount("pkg-old"), parsed.Assets[0].Size);
                Assert.Equal(
                    BitConverter.ToString(System.Security.Cryptography.SHA1.HashData(Encoding.UTF8.GetBytes("pkg-old"))).Replace("-", string.Empty),
                    parsed.Assets[0].SHA1);

                var bytes = await http.GetByteArrayAsync(latest.DownloadUrl);
                Assert.Equal(Encoding.UTF8.GetBytes("pkg-new"), bytes);
            }

            using (var restarted = new ServiceFactory(data, token: null))
            {
                var latest = await new HttpStationServiceClient(restarted.CreateClient()).GetLatestAsync("OP010", CancellationToken.None);
                Assert.Equal("1.0.9", latest.Version);
            }
        }

        [Fact]
        public async Task InboxDrop_IsImportedOnTheNextQuery()
        {
            var data = Path.Combine(_root, "data-c");
            using (var factory = new ServiceFactory(data, token: null))
            {
                factory.CreateClient();
                var inbox = Path.Combine(data, "inbox");
                Directory.CreateDirectory(inbox);
                File.WriteAllBytes(Path.Combine(inbox, "HostTemplate-1.0.4-full.nupkg"), Encoding.UTF8.GetBytes("dropped"));

                var latest = await new HttpStationServiceClient(factory.CreateClient()).GetLatestAsync("OP010", CancellationToken.None);
                Assert.Equal("1.0.4", latest.Version);
                Assert.False(File.Exists(Path.Combine(inbox, "HostTemplate-1.0.4-full.nupkg")));
                Assert.True(File.Exists(Path.Combine(data, "packages", "1.0.4", "HostTemplate-1.0.4-full.nupkg")));
            }
        }

        [Fact]
        public async Task Token_IsRequiredExceptForHealth()
        {
            using (var factory = new ServiceFactory(Path.Combine(_root, "data-d"), "line-token"))
            {
                var http = factory.CreateClient();
                var health = await http.GetAsync("/api/health");
                Assert.Equal(HttpStatusCode.OK, health.StatusCode);

                var denied = await http.GetAsync("/api/stations/OP010/versions");
                Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);

                http.DefaultRequestHeaders.Add(TokenAuthMiddleware.HeaderName, "line-token");
                var allowed = await http.GetAsync("/api/stations/OP010/versions");
                Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
            }
        }

        private static void WriteStation(string directory, string name)
        {
            var manager = new ConfigManager(directory);
            manager.Load(out _);
            manager.App.WorkStationName = name;
            Assert.True(manager.SaveApp(out var error), error);
        }

        private static async Task PutPackage(HttpClient http, string version, byte[] body)
        {
            var content = new ByteArrayContent(body);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            var response = await http.PutAsync("/api/packages/" + version + "?fileName=HostTemplate-" + version + "-full.nupkg", content);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        private sealed class ServiceFactory : WebApplicationFactory<Program>
        {
            private readonly string _dataRoot;
            private readonly string _token;

            public ServiceFactory(string dataRoot, string token)
            {
                _dataRoot = dataRoot;
                _token = token ?? string.Empty;
            }

            protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
            {
                builder.UseSetting(HostDefaults.EnvironmentKey, Environments.Development);
                builder.ConfigureAppConfiguration((_, config) =>
                {
                    config.AddInMemoryCollection(new System.Collections.Generic.Dictionary<string, string>
                    {
                        ["UpdateService:DataRoot"] = _dataRoot,
                        ["UpdateService:Token"] = _token,
                        ["UpdateService:PackageId"] = "HostTemplate",
                    });
                });
            }
        }
    }
}
