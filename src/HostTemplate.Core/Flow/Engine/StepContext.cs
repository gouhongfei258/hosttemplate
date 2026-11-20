using System.Threading;
using System.Threading.Tasks;
using HostTemplate.Core.Abstractions;
using HostTemplate.Core.Configuration;
using HostTemplate.Core.Diagnostics;
using HostTemplate.Core.Flow.Process;
using HostTemplate.Core.Runtime.Live;
using HostTemplate.Core.Runtime.State;

namespace HostTemplate.Core.Flow.Engine
{
    /// <summary>
    /// 步骤执行上下文。步骤类只通过它访问 PLC / 接口 / 日志 / 状态，不碰任何全局静态对象。
    /// </summary>
    public sealed class StepContext
    {
        public StepContext(StationContext station, ushort word, CancellationToken cancellationToken)
        {
            Station = station;
            Word = word;
            CancellationToken = cancellationToken;
        }

        public StationContext Station { get; }

        public ushort Word { get; }

        public CancellationToken CancellationToken { get; }

        public IPlcClient Plc => Station.PlcClient;

        public IApiClient Api => Station.ApiClient;

        public IAppLogger Log => Station.Logger;

        public AlarmManager Alarms => Station.Alarms;

        public StationState State => Station.State;

        public ProcessTrace Trace => Station.Trace;

        /// <summary>
        /// 可选的当前周期数据袋。页面不读它也完全可以。
        /// </summary>
        public CycleLiveData Live => Station.Live;

        public AppConfig App => Station.App;

        public PlcConfig PlcConfig => Station.Plc;

        /// <summary>
        /// 按地址键读字符串，失败返回空串。
        /// </summary>
        public async Task<string> ReadPlcStringAsync(string key, int length, CancellationToken cancellationToken = default)
        {
            var address = Station.GetAddress(key);
            if (string.IsNullOrEmpty(address) || Plc == null)
            {
                return string.Empty;
            }

            var read = await Plc.ReadStringAsync(address, length, cancellationToken).ConfigureAwait(false);
            return read.Success ? read.Value ?? string.Empty : string.Empty;
        }

        /// <summary>
        /// 按地址键写 Word，失败时自动置报警。
        /// </summary>
        public async Task<bool> WritePlcWordAsync(string key, ushort value, CancellationToken cancellationToken = default)
        {
            var address = Station.GetAddress(key);
            if (string.IsNullOrEmpty(address) || Plc == null)
            {
                return false;
            }

            var written = await Plc.WriteWordAsync(address, value, cancellationToken).ConfigureAwait(false);
            if (written.Success)
            {
                Alarms.Set($"写入失败:{key}", false);
                return true;
            }

            Alarms.Set($"写入失败:{key}", true, written.Error);
            Log.Error($"写入 {key}({address}) 失败：{written.Error}");
            return false;
        }

        /// <summary>
        /// 主动回写审查码（多数场景由引擎在步骤结束后统一回写，此方法供中间态使用）。
        /// </summary>
        public async Task<bool> WriteReviewAsync(ReviewCode code, CancellationToken cancellationToken = default)
        {
            var address = Station.GetAddress(AddressKeys.ReviewResult);
            if (string.IsNullOrEmpty(address) || Plc == null)
            {
                return false;
            }

            State.ReviewCode = (ushort)code;
            var written = await Plc.WriteWordAsync(address, (ushort)code, cancellationToken).ConfigureAwait(false);
            return written.Success;
        }

        /// <summary>
        /// 可选：写入或更新当前周期的一行（项目 / 标准 / 实测 / 合格）。同名项目覆盖。
        /// 实时页面不必是表格。
        /// </summary>
        public void PublishItem(string itemName, string standard, string value, bool passed)
        {
            Live.PublishItem(itemName, standard, value, passed);
        }

        /// <summary>
        /// 可选：给当前周期的一条曲线追加一个点。新的一件开始前由工站自己 <see cref="CycleLiveData.BeginCycle"/>。
        /// </summary>
        public void PublishPoint(string seriesName, double x, double y)
        {
            Live.PublishPoint(seriesName, x, y);
        }

        /// <summary>
        /// 可选：记下当前产品结论。本周期一旦 NG，后续 OK 不会盖掉。
        /// 怎么画由工站自己的页面决定，引擎不补这一笔。
        /// </summary>
        public void PublishVerdict(bool passed, string reason)
        {
            Live.PublishVerdict(passed, reason);
        }
    }
}
