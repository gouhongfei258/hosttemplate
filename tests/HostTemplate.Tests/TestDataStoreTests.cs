using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HostTemplate.Core.Data;
using Xunit;

namespace HostTemplate.Tests
{
    public class TestDataStoreTests : IDisposable
    {
        private readonly string _tempDirectory;
        private readonly string _databasePath;

        public TestDataStoreTests()
        {
            _tempDirectory = Path.Combine(Path.GetTempPath(), "HostTemplateDataTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDirectory);
            _databasePath = Path.Combine(_tempDirectory, "host.db");
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(_tempDirectory, true);
            }
            catch
            {
                // 忽略清理异常。
            }
        }

        [Fact]
        public async Task SaveAndQuery_ShouldRoundTrip()
        {
            var store = new TestDataStore(_databasePath);
            store.EnsureCreated();

            var record = new TestDataRecord
            {
                LineCode = "LINE-01",
                StationCode = "OP010",
                StationName = "测试工站",
                SnNumber = "DEMO00000000000001",
                Result = true,
                StartTime = DateTime.Now,
                EndTime = DateTime.Now.AddSeconds(1),
            };

            record.Items.Add(new TestDataItemRecord
            {
                ItemName = "电压",
                Standard = "11.5~12.5",
                Value = "12.0",
                Result = true,
            });

            var id = await store.SaveAsync(record, null);
            Assert.True(id > 0);

            var rows = await store.QueryRecentAsync(10);
            Assert.Single(rows);
            Assert.Equal("DEMO00000000000001", rows[0].SnNumber);
            Assert.True(rows[0].Result);
        }

        [Fact]
        public async Task SaveAlarm_ShouldPersist()
        {
            var store = new TestDataStore(_databasePath);
            store.EnsureCreated();

            await store.SaveAlarmAsync(new AlarmRecord
            {
                StationCode = "OP010",
                AlarmKey = "连接PLC失败",
                Content = "timeout",
                RaisedAt = DateTime.Now,
            });

            using (var db = HostDbContext.Create(_databasePath))
            {
                Assert.Equal(1, db.AlarmSet.Count());
            }
        }
    }
}
