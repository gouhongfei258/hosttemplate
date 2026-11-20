using System.Threading;
using HostTemplate.Core.Abstractions;
using HostTemplate.Core.Configuration;
using HostTemplate.Core.Diagnostics;
using HostTemplate.Core.Runtime;

namespace HostTemplate.Core.Flow
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

        /// <summary>当前周期实时数据。步骤只往这里推，不关心界面用什么控件画。</summary>
        public CycleLiveData Live => Station.Live;

        public AppConfig App => Station.App;

        public PlcConfig PlcConfig => Station.Plc;

        /// <summary>
        /// 按地址键读字符串，失败返回空串。
        /// </summary>
        public string ReadPlcString(string key, int length)
        {
            var address = Station.GetAddress(key);
            if (string.IsNullOrEmpty(address) || Plc == null)
            {
                return string.Empty;
            }

            return Plc.ReadString(address, length, out var value, out _) ? value : string.Empty;
        }

        /// <summary>
        /// 按地址键写 Word，失败时自动置报警。
        /// </summary>
        public bool WritePlcWord(string key, ushort value)
        {
            var address = Station.GetAddress(key);
            if (string.IsNullOrEmpty(address) || Plc == null)
            {
                return false;
            }

            if (Plc.WriteWord(address, value, out var error))
            {
                Alarms.Set($"写入失败:{key}", false);
                return true;
            }

            Alarms.Set($"写入失败:{key}", true, error);
            Log.Error($"写入 {key}({address}) 失败：{error}");
            return false;
        }

        /// <summary>
        /// 主动回写审查码（多数场景由引擎在步骤结束后统一回写，此方法供中间态使用）。
        /// </summary>
        public bool WriteReview(ReviewCode code)
        {
            var address = Station.GetAddress(AddressKeys.ReviewResult);
            if (string.IsNullOrEmpty(address) || Plc == null)
            {
                return false;
            }

            State.ReviewCode = (ushort)code;
            return Plc.WriteWord(address, (ushort)code, out _);
        }

        /// <summary>
        /// 写入或更新当前周期的一行（项目 / 标准 / 实测 / 合格）。同名项目覆盖。
        /// </summary>
        public void PublishItem(string itemName, string standard, string value, bool passed)
        {
            Live.PublishItem(itemName, standard, value, passed);
        }

        /// <summary>
        /// 给当前周期的一条曲线追加一个点。同名系列会累积，进站时由引擎清空。
        /// </summary>
        public void PublishPoint(string seriesName, double x, double y)
        {
            Live.PublishPoint(seriesName, x, y);
        }

        /// <summary>
        /// 给出当前产品结论（大号 OK / NG）。本周期一旦 NG，后续 OK 不会盖掉。
        /// 步骤漏调时，引擎仍会在失败结果上补一次 NG。
        /// </summary>
        public void PublishVerdict(bool passed, string reason)
        {
            Live.PublishVerdict(passed, reason);
        }
    }
}
