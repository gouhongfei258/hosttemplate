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
    }
}
