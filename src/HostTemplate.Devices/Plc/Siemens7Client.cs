using System;
using HslCommunication.Profinet.Siemens;
using HostTemplate.Devices.Hsl;

namespace HostTemplate.Devices.Plc
{
    /// <summary>
    /// 西门子 S7 客户端（S1200 / S1500 / S300 / S400 / S200Smart）。
    ///
    /// 通用读写全部继承自 <see cref="HslDeviceBase"/>，这里只负责三件事：
    /// 建 SiemensS7Net、给出连接/断开动作、补 S7 独有的读写（日期时间、订货号）。
    /// </summary>
    public sealed class Siemens7Client : HslDeviceBase
    {
        private readonly SiemensS7Net _s7;

        public Siemens7Client(
            string ipAddress,
            int port = 102,
            string cpuType = "S1200",
            int connectTimeoutMs = 2000,
            int receiveTimeoutMs = 2000,
            int rack = 0,
            int slot = 0)
        {
            if (string.IsNullOrWhiteSpace(ipAddress))
            {
                throw new ArgumentNullException(nameof(ipAddress));
            }

            Name = "SiemensS7";
            Address = ipAddress;

            _s7 = new SiemensS7Net(ParseCpu(cpuType), ipAddress)
            {
                Port = port <= 0 ? 102 : port,
                ConnectTimeOut = connectTimeoutMs,
                ReceiveTimeOut = receiveTimeoutMs,
                Rack = (byte)rack,
                Slot = (byte)slot,
            };

            Device = _s7;
            ConnectAction = () => _s7.ConnectServer();
            CloseAction = () => _s7.ConnectClose();
        }

        /// <summary>底层 S7 对象，需要调本类未封装的方法时用。</summary>
        public SiemensS7Net S7 => _s7;

        /// <summary>读 PLC 时钟（DB 中的 DATE_AND_TIME）。</summary>
        public bool ReadDateTime(string address, out DateTime value, out string error)
        {
            return TryRead(address, () => _s7.ReadDateTime(address), out value, out error);
        }

        /// <summary>写 PLC 时钟。</summary>
        public bool WriteDateTime(string address, DateTime value, out string error)
        {
            return TryWrite(address, () => _s7.Write(address, value), out error);
        }

        /// <summary>读 CPU 订货号，用于上线自检日志。</summary>
        public bool ReadOrderNumber(out string value, out string error)
        {
            return TryCall(() => _s7.ReadOrderNumber(), out value, out error);
        }

        private static SiemensPLCS ParseCpu(string cpu)
        {
            switch ((cpu ?? string.Empty).Trim().ToUpperInvariant())
            {
                case "S1500":
                case "1500":
                    return SiemensPLCS.S1500;
                case "S300":
                case "300":
                    return SiemensPLCS.S300;
                case "S400":
                case "400":
                    return SiemensPLCS.S400;
                case "S200SMART":
                case "200SMART":
                    return SiemensPLCS.S200Smart;
                default:
                    return SiemensPLCS.S1200;
            }
        }
    }
}
