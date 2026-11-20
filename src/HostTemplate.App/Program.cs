using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using HostTemplate.App.Forms;
using HostTemplate.App.Steps;
using HostTemplate.Core.Abstractions;
using HostTemplate.Core.Configuration;
using HostTemplate.Core.Data;
using HostTemplate.Core.Diagnostics;
using HostTemplate.Core.Flow;
using HostTemplate.Core.Runtime;
using HostTemplate.Core.Security;
using HostTemplate.Devices.Api;
using HostTemplate.Devices.Hsl;
using HostTemplate.Devices.Plc;
using log4net.Config;

namespace HostTemplate.App
{
    internal static class Program
    {
        private const string MutexName = @"Global\HostTemplate-{8B0F2C11-4F0B-4A3E-9F0E-7A3F9C2D1E44}";

        [STAThread]
        private static void Main(string[] args)
        {
            using (var mutex = new Mutex(true, MutexName, out var createdNew))
            {
                if (!createdNew)
                {
                    MessageBox.Show("程序已经在运行。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                ApplicationConfiguration.Initialize();

                var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

                if (!InitLogger(baseDirectory, out var logger))
                {
                    return;
                }

                try
                {
                    Run(logger, baseDirectory, args);
                }
                catch (Exception ex)
                {
                    logger.Error("程序启动失败", ex);
                    MessageBox.Show(ex.ToString(), "启动失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private static bool InitLogger(string baseDirectory, out Log4NetLogger logger)
        {
            logger = null;

            try
            {
                var configFile = Path.Combine(baseDirectory, "log4net.config");
                if (File.Exists(configFile))
                {
                    XmlConfigurator.Configure(new FileInfo(configFile));
                }

                logger = new Log4NetLogger();
                var instance = logger;

                Application.ThreadException += (s, e) =>
                {
                    instance.Error("UI 线程未处理异常", e.Exception);
                    MessageBox.Show(e.Exception.Message, "未处理异常", MessageBoxButtons.OK, MessageBoxIcon.Error);
                };

                AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                {
                    instance.Error("后台线程未处理异常", e.ExceptionObject as Exception);
                };

                TaskScheduler_UnobservedTaskException(instance);

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("初始化日志失败：" + ex.Message, "启动失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private static void TaskScheduler_UnobservedTaskException(Log4NetLogger logger)
        {
            System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                logger.Error("未观察的任务异常", e.Exception);
                e.SetObserved();
            };
        }

        private static void Run(Log4NetLogger logger, string baseDirectory, string[] args)
        {
            logger.Info("================ 程序启动 ================");

            // 1. 配置
            var configDirectory = Path.Combine(baseDirectory, "config");
            var config = new ConfigManager(configDirectory);
            config.Load(out var configMessage);

            if (!string.IsNullOrWhiteSpace(configMessage))
            {
                logger.Warn(configMessage.Trim());
            }

            // 命令行 --simulate 可临时覆盖配置，便于演示与无硬件调试。
            if (args != null && Array.IndexOf(args, "--simulate") >= 0)
            {
                config.App.Simulate = true;
                if (config.App.SimulateStepSeconds <= 0)
                {
                    config.App.SimulateStepSeconds = 3;
                }

                logger.Info("命令行参数 --simulate：已强制启用仿真模式。");
            }

            // 2. 运行时上下文（取代原来的 static Global）
            var context = new StationContext(config, logger, baseDirectory);

            // 3. 登录鉴权。默认不启用（app.xml → [权限] 01.启用登录），
            //    未启用时 Session 为匿名管理员，权限判断全部放行。
            if (!TryLogin(context, configDirectory, logger))
            {
                return;
            }

            // --config：直接打开配置页后退出。现场调试时不用先进主界面。
            if (args != null && Array.IndexOf(args, "--config") >= 0)
            {
                using (var configForm = new ConfigForm(context))
                {
                    configForm.ShowDialog();
                }

                return;
            }

            // 4. 通信设备
            context.PlcClient = CreatePlcClient(context);
            context.ApiClient = CreateApiClient(context);

            // 5. 数据库
            var store = new TestDataStore(context.DatabasePath);
            try
            {
                store.EnsureCreated();
                logger.Info($"本地数据库就绪：{context.DatabasePath}");
            }
            catch (Exception ex)
            {
                logger.Error("初始化本地数据库失败，测试数据将不会落库", ex);
            }

            // 6. 流程引擎 + 业务步骤
            var engine = new StationEngine(context, DemoSteps.Create(store));

            logger.Info($"工站：{context.App.WorkStationName}（{context.App.EquId}），仿真模式={context.App.Simulate}");

            var autoStart = args != null && Array.IndexOf(args, "--autostart") >= 0;

            Application.Run(new MainForm(context, engine, CreatePlcClient, CreateApiClient) { AutoStart = autoStart });
        }

        /// <summary>
        /// 登录。app.xml 未开启「启用登录」时直接返回 true，不弹窗。
        /// </summary>
        private static bool TryLogin(StationContext context, string configDirectory, Log4NetLogger logger)
        {
            if (!context.App.EnableLogin)
            {
                return true;
            }

            var auth = new AuthService(Path.Combine(configDirectory, "users.xml"));
            auth.Load(out var message);

            if (!string.IsNullOrWhiteSpace(message))
            {
                logger.Info(message.Trim());
            }

            using (var form = new LoginForm(auth, context.App.WorkStationName))
            {
                if (form.ShowDialog() != DialogResult.OK)
                {
                    logger.Info("用户取消登录，程序退出。");
                    return false;
                }

                context.Session = form.Session;
            }

            logger.Info($"登录成功：{context.Session.DisplayName}（{context.Session.Role}）");
            return true;
        }

        private static IApiClient CreateApiClient(StationContext context)
        {
            return new HttpApiClient(context.Api, context.Logger);
        }

        private static IPlcClient CreatePlcClient(StationContext context)
        {
            if (context.App.Simulate)
            {
                var commandAddress = context.GetAddress(AddressKeys.Command) ?? "MW200";
                var fake = new FakePlcClient(commandAddress);

                if (context.App.SimulateStepSeconds > 0)
                {
                    fake.StartAutoFlow(TimeSpan.FromSeconds(context.App.SimulateStepSeconds));
                    context.Logger.Info($"仿真 PLC 已启用，每 {context.App.SimulateStepSeconds}s 自动推进流程字。");
                }
                else
                {
                    context.Logger.Info("仿真 PLC 已启用，等待手动推进流程字。");
                }

                return fake;
            }

            context.Logger.Info($"PLC：{context.Plc.Protocol} {context.Plc.Address}:{context.Plc.Port}（{context.Plc.CpuType}）");

            if (HslDeviceFactory.TryCreate(context.Plc, out var client, out var error))
            {
                return client;
            }

            context.Logger.Error($"创建 PLC 客户端失败：{error}，已降级为 SiemensS7。");

            return new Siemens7Client(
                context.Plc.Address,
                context.Plc.Port,
                context.Plc.CpuType,
                context.Plc.ConnectTimeoutMs,
                context.Plc.ReceiveTimeoutMs,
                context.Plc.Rack,
                context.Plc.Slot);
        }
    }
}
