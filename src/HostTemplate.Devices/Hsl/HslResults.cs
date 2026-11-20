using HslCommunication;
using HostTemplate.Core.Abstractions;

namespace HostTemplate.Devices.Hsl
{
    /// <summary>
    /// 把 HSL 的 <see cref="OperateResult"/> 收成模板自己的 <see cref="DeviceResult"/>。
    /// </summary>
    internal static class HslResults
    {
        public static DeviceResult From(OperateResult result)
        {
            if (result == null)
            {
                return DeviceResult.Fail("HSL 未返回结果");
            }

            return result.IsSuccess ? DeviceResult.Ok() : DeviceResult.Fail(result.Message);
        }

        public static DeviceResult<T> From<T>(OperateResult<T> result)
        {
            if (result == null)
            {
                return DeviceResult<T>.Fail("HSL 未返回结果");
            }

            return result.IsSuccess
                ? DeviceResult<T>.Ok(result.Content)
                : DeviceResult<T>.Fail(result.Message);
        }
    }
}
