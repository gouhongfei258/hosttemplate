using System;

namespace HostTemplate.Core.Diagnostics
{
    public enum LogLevel
    {
        Debug = 0,
        Info = 1,
        Warn = 2,
        Error = 3,
        Fatal = 4,
    }

    public sealed class LogEventArgs : EventArgs
    {
        public LogEventArgs(LogLevel level, string message, DateTime time)
        {
            Level = level;
            Message = message;
            Time = time;
        }

        public LogLevel Level { get; }

        public string Message { get; }

        public DateTime Time { get; }
    }

    /// <summary>
    /// 日志抽象。实现方负责落盘，同时通过 Written 事件把日志推给界面。
    /// 引擎只依赖这个接口，便于替换成 NLog / Serilog。
    /// </summary>
    public interface IAppLogger
    {
        event EventHandler<LogEventArgs> Written;

        void Debug(string message);

        void Info(string message);

        void Warn(string message);

        void Error(string message);

        void Error(string message, Exception exception);
    }
}
