using System;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using HslCommunication.Core.Pipe;
using HostTemplate.Core.Abstractions;
using HostTemplate.Devices.Hsl;

namespace HostTemplate.Devices.Hsl.Serial
{
    /// <summary>
    /// 原始串口客户端，包在 HSL <see cref="PipeSerialPort"/> 上。
    /// 12.8.3 的 <c>DeviceSerialPort.Open</c> 只有同步方法；管道上的 OpenCommunicationAsync / SendAsync
    /// 会调用管道基类的异步实现（内部再进到串口的 Open / Send）。
    /// <c>DeviceSerialPort</c> 是按地址读写的串口设备基类，和这层字节流不是同一件事。
    /// </summary>
    public sealed class HslSerialClient : HslPipeChannel
    {
        private readonly string _initError;

        public HslSerialClient(
            string portName,
            int baudRate = 9600,
            int dataBits = 8,
            StopBits stopBits = StopBits.One,
            Parity parity = Parity.None,
            int timeoutMs = 2000)
            : this(Build(portName, baudRate, dataBits, stopBits, parity, timeoutMs), timeoutMs)
        {
        }

        private HslSerialClient(SerialBuild build, int timeoutMs)
            : base("Serial", build.Pipe, timeoutMs)
        {
            _initError = build.Error;
            PortName = build.PortName;
            BaudRate = build.BaudRate;
        }

        public string PortName { get; }

        public int BaudRate { get; }

        /// <summary>原始串口管道。</summary>
        public PipeSerialPort RawSerial => RawPipe as PipeSerialPort;

        public override Task<DeviceResult> OpenAsync(CancellationToken cancellationToken = default)
        {
            if (!string.IsNullOrEmpty(_initError))
            {
                return Task.FromResult(DeviceResult.Fail(_initError));
            }

            return base.OpenAsync(cancellationToken);
        }

        private static SerialBuild Build(
            string portName,
            int baudRate,
            int dataBits,
            StopBits stopBits,
            Parity parity,
            int timeoutMs)
        {
            if (string.IsNullOrWhiteSpace(portName))
            {
                throw new ArgumentNullException(nameof(portName));
            }

            var pipe = new PipeSerialPort
            {
                ReceiveTimeOut = timeoutMs > 0 ? timeoutMs : 2000,
            };

            var build = new SerialBuild
            {
                Pipe = pipe,
                PortName = portName,
                BaudRate = baudRate > 0 ? baudRate : 9600,
            };

            try
            {
                pipe.SerialPortInni(portName, build.BaudRate, dataBits, stopBits, parity);
            }
            catch (Exception ex)
            {
                build.Error = ex.Message;
            }

            return build;
        }

        private sealed class SerialBuild
        {
            public PipeSerialPort Pipe { get; set; }

            public string Error { get; set; }

            public string PortName { get; set; }

            public int BaudRate { get; set; }
        }
    }
}
