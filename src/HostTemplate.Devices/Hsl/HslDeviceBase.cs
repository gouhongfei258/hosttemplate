using System;
using System.Net.NetworkInformation;
using HslCommunication;
using HslCommunication.Core.Device;
using HostTemplate.Core.Abstractions;

namespace HostTemplate.Devices.Hsl
{
    /// <summary>
    /// HslCommunication 设备统一基类。
    ///
    /// 为什么要这一层：HSL 的 S7 / Modbus / 三菱 / 欧姆龙 / 汇川等协议，
    /// 公开方法全部落在 <see cref="DeviceCommunication"/> 上（ReadBool / ReadInt32 /
    /// ReadFloat / Write / Wait …），差异只在「怎么连」。所以把连接和读写拆开：
    ///
    ///   基类：连接守卫 + 串行锁 + 全类型读写（同步），返回 bool + out，不抛异常；
    ///   子类：只负责 new 出具体协议对象、给出 ConnectAction / CloseAction。
    ///
    /// 新增一个品牌的 PLC 只要 20 行（见 <see cref="ModbusTcpClient"/>）。
    ///
    /// 设计取舍：
    ///   1. 全部返回 bool + out error，和 <see cref="IPlcClient"/> 一致，主循环不需要 try/catch；
    ///   2. HSL 对象不是线程安全的，所有读写都在 _gate 内串行；
    ///   3. 未连接时直接返回失败，不隐式重连——重连节奏由引擎控制，避免现场抖动时反复建连；
    ///   4. 异步接口没包：HSL 的 Async 在多数设备上是同步包装，50ms 轮询用不上。
    /// </summary>
    public abstract class HslDeviceBase : IPlcClient
    {
        private readonly object _gate = new object();
        private bool _disposed;

        /// <summary>底层 HSL 设备对象。子类在构造里赋值。</summary>
        protected DeviceCommunication Device { get; set; }

        /// <summary>建连动作，由子类按协议提供。</summary>
        protected Func<OperateResult> ConnectAction { get; set; }

        /// <summary>断开动作，由子类按协议提供。</summary>
        protected Func<OperateResult> CloseAction { get; set; }

        /// <summary>设备名，用于日志。</summary>
        public string Name { get; protected set; } = "HslDevice";

        public string Address { get; protected set; } = string.Empty;

        public bool IsConnected { get; protected set; }

        /// <summary>最近一次失败的原因，便于界面直接展示。</summary>
        public string LastError { get; private set; } = string.Empty;

        /// <summary>原始 HSL 对象。要调本类没包到的方法时用它。</summary>
        public DeviceCommunication RawDevice => Device;

        public bool Open(out string error)
        {
            error = string.Empty;

            lock (_gate)
            {
                if (Device == null)
                {
                    error = "设备未初始化";
                    LastError = error;
                    return false;
                }

                try
                {
                    var result = ConnectAction?.Invoke();
                    if (result != null && !result.IsSuccess)
                    {
                        error = result.Message;
                        LastError = error;
                        IsConnected = false;
                        return false;
                    }

                    IsConnected = true;
                    LastError = string.Empty;
                    return true;
                }
                catch (Exception ex)
                {
                    error = ex.Message;
                    LastError = error;
                    IsConnected = false;
                    return false;
                }
            }
        }

        public void Close()
        {
            lock (_gate)
            {
                IsConnected = false;

                try
                {
                    CloseAction?.Invoke();
                }
                catch
                {
                    // 关闭失败不影响下次重连。
                }
            }
        }

        /// <summary>Ping 一下设备 IP，用来区分「网线没插」和「PLC 没开」。</summary>
        public bool Ping()
        {
            try
            {
                return Device is DeviceTcpNet tcp && tcp.IpAddressPing() == IPStatus.Success;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>长连接模式。部分交换机/网关会主动断空闲连接，开这个可减少重连。</summary>
        public void EnablePersistentConnection()
        {
            try
            {
#pragma warning disable CS0612 // HSL 标记为过时，但现场仍在用，保留这个开关。
                (Device as DeviceTcpNet)?.SetPersistentConnection();
#pragma warning restore CS0612
            }
            catch
            {
                // 个别协议不支持，忽略。
            }
        }

        // ---------------- 读 ----------------

        public bool ReadBool(string address, out bool value, out string error)
        {
            return TryRead(address, () => Device.ReadBool(address), out value, out error);
        }

        public bool ReadBoolArray(string address, int count, out bool[] values, out string error)
        {
            return TryRead(address, () => Device.ReadBool(address, ToUShort(count)), out values, out error);
        }

        /// <summary>
        /// 读单个字节。HSL 只在部分协议上直接提供 ReadByte，
        /// 这里统一用「读 1 字节」实现，所有协议行为一致。
        /// </summary>
        public bool ReadByte(string address, out byte value, out string error)
        {
            value = 0;

            if (!ReadBytes(address, 1, out var bytes, out error) || bytes == null || bytes.Length == 0)
            {
                return false;
            }

            value = bytes[0];
            return true;
        }

        public bool ReadBytes(string address, int length, out byte[] values, out string error)
        {
            return TryRead(address, () => Device.Read(address, ToUShort(length)), out values, out error);
        }

        public bool ReadInt16(string address, out short value, out string error)
        {
            return TryRead(address, () => Device.ReadInt16(address), out value, out error);
        }

        public bool ReadInt16Array(string address, int count, out short[] values, out string error)
        {
            return TryRead(address, () => Device.ReadInt16(address, ToUShort(count)), out values, out error);
        }

        public bool ReadWord(string address, out ushort value, out string error)
        {
            return ReadUInt16(address, out value, out error);
        }

        public bool ReadUInt16(string address, out ushort value, out string error)
        {
            return TryRead(address, () => Device.ReadUInt16(address), out value, out error);
        }

        public bool ReadWordArray(string address, int count, out ushort[] values, out string error)
        {
            return ReadUInt16Array(address, count, out values, out error);
        }

        public bool ReadUInt16Array(string address, int count, out ushort[] values, out string error)
        {
            return TryRead(address, () => Device.ReadUInt16(address, ToUShort(count)), out values, out error);
        }

        public bool ReadInt32(string address, out int value, out string error)
        {
            return TryRead(address, () => Device.ReadInt32(address), out value, out error);
        }

        public bool ReadInt32Array(string address, int count, out int[] values, out string error)
        {
            return TryRead(address, () => Device.ReadInt32(address, ToUShort(count)), out values, out error);
        }

        public bool ReadUInt32(string address, out uint value, out string error)
        {
            return TryRead(address, () => Device.ReadUInt32(address), out value, out error);
        }

        public bool ReadUInt32Array(string address, int count, out uint[] values, out string error)
        {
            return TryRead(address, () => Device.ReadUInt32(address, ToUShort(count)), out values, out error);
        }

        public bool ReadInt64(string address, out long value, out string error)
        {
            return TryRead(address, () => Device.ReadInt64(address), out value, out error);
        }

        public bool ReadUInt64(string address, out ulong value, out string error)
        {
            return TryRead(address, () => Device.ReadUInt64(address), out value, out error);
        }

        public bool ReadReal(string address, out float value, out string error)
        {
            return ReadFloat(address, out value, out error);
        }

        public bool ReadFloat(string address, out float value, out string error)
        {
            return TryRead(address, () => Device.ReadFloat(address), out value, out error);
        }

        public bool ReadFloatArray(string address, int count, out float[] values, out string error)
        {
            return TryRead(address, () => Device.ReadFloat(address, ToUShort(count)), out values, out error);
        }

        public bool ReadDouble(string address, out double value, out string error)
        {
            return TryRead(address, () => Device.ReadDouble(address), out value, out error);
        }

        public bool ReadDoubleArray(string address, int count, out double[] values, out string error)
        {
            return TryRead(address, () => Device.ReadDouble(address, ToUShort(count)), out values, out error);
        }

        public bool ReadString(string address, int length, out string value, out string error)
        {
            value = string.Empty;

            if (!TryRead(address, () => Device.ReadString(address, ToUShort(length)), out var result, out error))
            {
                return false;
            }

            value = result ?? string.Empty;
            return true;
        }

        /// <summary>按结构体读取（HSL 的 ReadStruct）。T 为自定义类，字段顺序与 PLC 内存布局一致。</summary>
        public bool ReadStruct<T>(string address, int length, out T value, out string error) where T : class, new()
        {
            return TryRead(address, () => Device.ReadStruct<T>(address, ToUShort(length)), out value, out error);
        }

        // ---------------- 写 ----------------

        public bool WriteBool(string address, bool value, out string error)
        {
            return TryWrite(address, () => Device.Write(address, value), out error);
        }

        public bool WriteBoolArray(string address, bool[] values, out string error)
        {
            return TryWrite(address, () => Device.Write(address, values), out error);
        }

        public bool WriteByte(string address, byte value, out string error)
        {
            return TryWrite(address, () => Device.Write(address, value), out error);
        }

        public bool WriteBytes(string address, byte[] values, out string error)
        {
            return TryWrite(address, () => Device.Write(address, values), out error);
        }

        public bool WriteInt16(string address, short value, out string error)
        {
            return TryWrite(address, () => Device.Write(address, value), out error);
        }

        public bool WriteWord(string address, ushort value, out string error)
        {
            return WriteUInt16(address, value, out error);
        }

        public bool WriteUInt16(string address, ushort value, out string error)
        {
            return TryWrite(address, () => Device.Write(address, value), out error);
        }

        public bool WriteInt32(string address, int value, out string error)
        {
            return TryWrite(address, () => Device.Write(address, value), out error);
        }

        public bool WriteUInt32(string address, uint value, out string error)
        {
            return TryWrite(address, () => Device.Write(address, value), out error);
        }

        public bool WriteInt64(string address, long value, out string error)
        {
            return TryWrite(address, () => Device.Write(address, value), out error);
        }

        public bool WriteUInt64(string address, ulong value, out string error)
        {
            return TryWrite(address, () => Device.Write(address, value), out error);
        }

        public bool WriteReal(string address, float value, out string error)
        {
            return WriteFloat(address, value, out error);
        }

        public bool WriteFloat(string address, float value, out string error)
        {
            return TryWrite(address, () => Device.Write(address, value), out error);
        }

        public bool WriteDouble(string address, double value, out string error)
        {
            return TryWrite(address, () => Device.Write(address, value), out error);
        }

        public bool WriteString(string address, string value, out string error)
        {
            return TryWrite(address, () => Device.Write(address, value ?? string.Empty), out error);
        }

        /// <summary>定长写字符串。PLC 侧是 String[n] 时必须用这个，避免长度不匹配。</summary>
        public bool WriteString(string address, string value, int length, out string error)
        {
            return TryWrite(address, () => Device.Write(address, value ?? string.Empty, length), out error);
        }

        // ---------------- 等待 ----------------

        /// <summary>
        /// 等某个位变成期望值。返回 true 表示在超时前等到，elapsed 是实际耗时。
        /// 气缸到位、夹具夹紧这类握手用它比手写轮询清楚。
        /// </summary>
        public bool WaitBool(string address, bool expect, int timeoutMs, out TimeSpan elapsed, out string error)
        {
            elapsed = TimeSpan.Zero;

            if (!TryRead(address, () => Device.Wait(address, expect, 20, timeoutMs), out var span, out error))
            {
                return false;
            }

            elapsed = span;
            return true;
        }

        public bool WaitWord(string address, ushort expect, int timeoutMs, out TimeSpan elapsed, out string error)
        {
            elapsed = TimeSpan.Zero;

            if (!TryRead(address, () => Device.Wait(address, expect, 20, timeoutMs), out var span, out error))
            {
                return false;
            }

            elapsed = span;
            return true;
        }

        public bool WaitInt32(string address, int expect, int timeoutMs, out TimeSpan elapsed, out string error)
        {
            elapsed = TimeSpan.Zero;

            if (!TryRead(address, () => Device.Wait(address, expect, 20, timeoutMs), out var span, out error))
            {
                return false;
            }

            elapsed = span;
            return true;
        }

        // ---------------- 内部 ----------------

        /// <summary>带连接守卫与串行锁的读。子类需要自定义读取时复用它。</summary>
        protected bool TryRead<T>(string address, Func<OperateResult<T>> action, out T value, out string error)
        {
            value = default;
            error = string.Empty;

            lock (_gate)
            {
                if (!EnsureReady(address, out error))
                {
                    return false;
                }

                try
                {
                    var result = action();
                    if (!result.IsSuccess)
                    {
                        error = result.Message;
                        LastError = error;
                        return false;
                    }

                    value = result.Content;
                    return true;
                }
                catch (Exception ex)
                {
                    error = ex.Message;
                    LastError = error;
                    return false;
                }
            }
        }

        /// <summary>不需要地址的调用（如读订货号）。</summary>
        protected bool TryCall<T>(Func<OperateResult<T>> action, out T value, out string error)
        {
            value = default;
            error = string.Empty;

            lock (_gate)
            {
                if (!EnsureReady(null, out error))
                {
                    return false;
                }

                try
                {
                    var result = action();
                    if (!result.IsSuccess)
                    {
                        error = result.Message;
                        LastError = error;
                        return false;
                    }

                    value = result.Content;
                    return true;
                }
                catch (Exception ex)
                {
                    error = ex.Message;
                    LastError = error;
                    return false;
                }
            }
        }

        /// <summary>带连接守卫与串行锁的写。</summary>
        protected bool TryWrite(string address, Func<OperateResult> action, out string error)
        {
            error = string.Empty;

            lock (_gate)
            {
                if (!EnsureReady(address, out error))
                {
                    return false;
                }

                try
                {
                    var result = action();
                    if (!result.IsSuccess)
                    {
                        error = result.Message;
                        LastError = error;
                        return false;
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    error = ex.Message;
                    LastError = error;
                    return false;
                }
            }
        }

        private bool EnsureReady(string address, out string error)
        {
            error = string.Empty;

            if (!IsConnected || Device == null)
            {
                error = "设备未连接";
                LastError = error;
                return false;
            }

            if (address != null && string.IsNullOrWhiteSpace(address))
            {
                error = "地址为空";
                LastError = error;
                return false;
            }

            return true;
        }

        private static ushort ToUShort(int count)
        {
            if (count <= 0)
            {
                return 0;
            }

            return count > ushort.MaxValue ? ushort.MaxValue : (ushort)count;
        }

        public virtual void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            Close();
        }
    }
}
