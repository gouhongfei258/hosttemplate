using System;
using HostTemplate.Core.Abstractions;
using HostTemplate.Core.Configuration;
using HostTemplate.Devices.Plc.Melsec;
using HostTemplate.Devices.Plc.Modbus;
using HostTemplate.Devices.Plc.Omron;
using HostTemplate.Devices.Plc.Siemens;

namespace HostTemplate.Devices.Hsl
{
    /// <summary>
    /// 按配置创建 PLC 客户端。新增品牌时这里加一个 case、<see cref="PlcDeviceCatalog"/> 里加一条，
    /// 上层（Program / 主循环 / 配置页）完全不用改。
    /// </summary>
    public static class HslDeviceFactory
    {
        public const string ProtocolSiemensS7 = "SiemensS7";
        public const string ProtocolModbusTcp = "ModbusTcp";
        public const string ProtocolInovanceTcp = "InovanceTcp";
        public const string ProtocolDeltaTcp = "DeltaTcp";
        public const string ProtocolMelsecMc = "MelsecMc";
        public const string ProtocolOmronFins = "OmronFins";

        /// <summary>支持的协议列表，来自设备清单，避免两处维护。</summary>
        public static string[] SupportedProtocols => PlcDeviceCatalog.Protocols;

        /// <summary>
        /// 创建客户端。协议名不认识时抛 <see cref="NotSupportedException"/>，
        /// 由调用方决定是降级还是报错退出。
        /// </summary>
        public static IPlcClient Create(PlcConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            switch ((config.Protocol ?? string.Empty).Trim().ToUpperInvariant())
            {
                case "MODBUSTCP":
                case "MODBUS":
                    return new ModbusTcpClient(
                        config.Address,
                        config.Port <= 0 ? 502 : config.Port,
                        ToStation(config.StationNo),
                        config.ConnectTimeoutMs,
                        config.ReceiveTimeoutMs);

                case "INOVANCETCP":
                case "INOVANCE":
                    return new InovanceTcpClient(
                        config.Address,
                        config.Port <= 0 ? 502 : config.Port,
                        ToStation(config.StationNo),
                        config.ConnectTimeoutMs,
                        config.ReceiveTimeoutMs);

                case "DELTATCP":
                case "DELTA":
                    return new DeltaTcpClient(
                        config.Address,
                        config.Port <= 0 ? 502 : config.Port,
                        ToStation(config.StationNo),
                        config.ConnectTimeoutMs,
                        config.ReceiveTimeoutMs);

                case "MELSECMC":
                case "MELSEC":
                case "MC":
                    return new MelsecMcClient(
                        config.Address,
                        config.Port <= 0 ? 6000 : config.Port,
                        config.ConnectTimeoutMs,
                        config.ReceiveTimeoutMs);

                case "OMRONFINS":
                case "OMRON":
                case "FINS":
                    return new OmronFinsClient(
                        config.Address,
                        config.Port <= 0 ? 9600 : config.Port,
                        config.StationNo,
                        config.ConnectTimeoutMs,
                        config.ReceiveTimeoutMs);

                case "SIEMENSS7":
                case "SIEMENS":
                case "S7":
                case "":
                    return new Siemens7Client(
                        config.Address,
                        config.Port <= 0 ? 102 : config.Port,
                        config.CpuType,
                        config.ConnectTimeoutMs,
                        config.ReceiveTimeoutMs,
                        config.Rack,
                        config.Slot);

                default:
                    throw new NotSupportedException($"不支持的 PLC 协议：{config.Protocol}");
            }
        }

        /// <summary>
        /// 安全版本：失败时返回 false 并给出原因，不抛异常。
        /// </summary>
        public static bool TryCreate(PlcConfig config, out IPlcClient client, out string error)
        {
            client = null;
            error = string.Empty;

            try
            {
                client = Create(config);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private static byte ToStation(int stationNo)
        {
            return (byte)Math.Max(1, Math.Min(247, stationNo));
        }
    }
}
