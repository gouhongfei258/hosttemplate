using HostTemplate.Core.Runtime.Live;
using Xunit;

namespace HostTemplate.Tests
{
    public class CycleLiveDataTests
    {
        [Fact]
        public void PublishItem_ShouldUpsertByName()
        {
            var live = new CycleLiveData();

            live.PublishItem("电压", "11.5~12.5", "12.0", true);
            live.PublishItem("电压", "11.5~12.5", "13.2", false);
            live.PublishItem("电流", "<1", "0.4", true);

            var snapshot = live.Snapshot();
            Assert.Equal(2, snapshot.Rows.Count);
            Assert.Equal("13.2", snapshot.Rows[0].Value);
            Assert.False(snapshot.Rows[0].Passed);
            Assert.Equal("电流", snapshot.Rows[1].ItemName);
        }

        [Fact]
        public void PublishPoint_ShouldAppendToNamedSeries()
        {
            var live = new CycleLiveData();

            live.PublishPoint("电压", 0, 11.8);
            live.PublishPoint("电压", 1, 12.1);
            live.PublishPoint("电流", 0, 0.4);

            var snapshot = live.Snapshot();
            Assert.Equal(2, snapshot.Series.Count);
            Assert.Equal("电压", snapshot.Series[0].Name);
            Assert.Equal(2, snapshot.Series[0].Points.Count);
            Assert.Equal(12.1, snapshot.Series[0].Points[1].Y);
            Assert.Equal("电流", snapshot.Series[1].Name);
        }

        [Fact]
        public void BeginCycle_ShouldClearCurrentPieceOnly()
        {
            var live = new CycleLiveData();
            live.RememberSerialNumber("SN-1");
            live.PublishItem("电压", "1", "1", true);
            live.PublishPoint("电压", 0, 1);
            live.PublishVerdict(false, "超限");

            live.BeginCycle();

            var snapshot = live.Snapshot();
            Assert.Equal(string.Empty, snapshot.SerialNumber);
            Assert.Equal(ProductVerdict.None, snapshot.Verdict);
            Assert.Equal(string.Empty, snapshot.Reason);
            Assert.Empty(snapshot.Rows);
            Assert.Empty(snapshot.Series);
        }

        [Fact]
        public void PublishVerdict_ShouldKeepNgUntilNextCycle()
        {
            var live = new CycleLiveData();

            live.PublishVerdict(false, "第一次不良");
            live.PublishVerdict(true, "后面又合格了");
            Assert.Equal(ProductVerdict.Ng, live.Snapshot().Verdict);
            Assert.Equal("第一次不良", live.Snapshot().Reason);

            live.PublishVerdict(false, "第二次不良");
            Assert.Equal("第二次不良", live.Snapshot().Reason);

            live.BeginCycle();
            live.PublishVerdict(true, "合格");
            Assert.Equal(ProductVerdict.Ok, live.Snapshot().Verdict);
            Assert.Equal("合格", live.Snapshot().Reason);
        }

        [Fact]
        public void Snapshot_ShouldNotSeeLaterWrites()
        {
            var live = new CycleLiveData();
            live.PublishPoint("电压", 0, 1);

            var snapshot = live.Snapshot();
            live.PublishPoint("电压", 1, 2);

            Assert.Single(snapshot.Series[0].Points);
            Assert.Equal(2, live.Snapshot().Series[0].Points.Count);
        }
    }
}
