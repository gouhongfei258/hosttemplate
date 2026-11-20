using System;
using System.IO;
using log4net;
using log4net.Appender;
using log4net.Repository.Hierarchy;

namespace HostTemplate.App.Updates
{
    /// <summary>
    /// log4net.config 仍跟程序走，日志文件改写到配置目录旁边，避免被版本目录替换清掉。
    /// </summary>
    public static class LogDirectoryConfigurator
    {
        public static void UseDirectory(string logDirectory)
        {
            if (string.IsNullOrWhiteSpace(logDirectory))
            {
                return;
            }

            Directory.CreateDirectory(logDirectory);

            if (!(LogManager.GetRepository() is Hierarchy hierarchy))
            {
                return;
            }

            foreach (var appender in hierarchy.GetAppenders())
            {
                if (appender is RollingFileAppender rolling)
                {
                    rolling.File = Path.Combine(logDirectory, "host.log");
                    rolling.ActivateOptions();
                }
            }
        }
    }
}
