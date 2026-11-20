using System;
using log4net;

namespace HostTemplate.Core.Diagnostics
{
    /// <summary>
    /// log4net 实现。日志同时写盘并通过事件推送到界面。
    /// </summary>
    public sealed class Log4NetLogger : IAppLogger
    {
        private readonly ILog _log;

        public Log4NetLogger(string loggerName = "HostTemplate")
        {
            _log = LogManager.GetLogger(loggerName);
        }

        public event EventHandler<LogEventArgs> Written;

        public void Debug(string message)
        {
            _log.Debug(message);
            Raise(LogLevel.Debug, message);
        }

        public void Info(string message)
        {
            _log.Info(message);
            Raise(LogLevel.Info, message);
        }

        public void Warn(string message)
        {
            _log.Warn(message);
            Raise(LogLevel.Warn, message);
        }

        public void Error(string message)
        {
            _log.Error(message);
            Raise(LogLevel.Error, message);
        }

        public void Error(string message, Exception exception)
        {
            var content = exception == null ? message : $"{message} :: {exception.Message}";
            _log.Error(message, exception);
            Raise(LogLevel.Error, content);
        }

        private void Raise(LogLevel level, string message)
        {
            try
            {
                Written?.Invoke(this, new LogEventArgs(level, message, DateTime.Now));
            }
            catch
            {
                // 界面日志订阅者异常不得影响业务主循环。
            }
        }
    }
}
