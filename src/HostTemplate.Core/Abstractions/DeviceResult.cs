namespace HostTemplate.Core.Abstractions
{
    /// <summary>
    /// 一次通信的结果。失败时带原因，不抛异常，主循环不用为单次读写包 try/catch。
    /// </summary>
    public readonly struct DeviceResult
    {
        public DeviceResult(bool success, string error)
        {
            Success = success;
            Error = error ?? string.Empty;
        }

        public bool Success { get; }

        public string Error { get; }

        public static DeviceResult Ok()
        {
            return new DeviceResult(true, string.Empty);
        }

        public static DeviceResult Fail(string error)
        {
            return new DeviceResult(false, error);
        }
    }

    /// <summary>
    /// 带返回值的通信结果。失败时 <see cref="Value"/> 为 default。
    /// </summary>
    public readonly struct DeviceResult<T>
    {
        public DeviceResult(bool success, T value, string error)
        {
            Success = success;
            Value = value;
            Error = error ?? string.Empty;
        }

        public bool Success { get; }

        public T Value { get; }

        public string Error { get; }

        public static DeviceResult<T> Ok(T value)
        {
            return new DeviceResult<T>(true, value, string.Empty);
        }

        public static DeviceResult<T> Fail(string error)
        {
            return new DeviceResult<T>(false, default(T), error);
        }
    }
}
