using System;
using System.Collections.Generic;
using System.IO;
using HostTemplate.Core.Abstractions;
using HostTemplate.Core.Configuration;
using HostTemplate.Core.Diagnostics;
using HostTemplate.Core.Flow.Process;
using HostTemplate.Core.Runtime.Live;
using HostTemplate.Core.Security;

namespace HostTemplate.Core.Runtime.State
{
    /// <summary>
    /// 取代原项目 static Global 的运行时上下文。
    /// 所有模块通过构造函数拿到它，不再访问全局静态字段，便于单测与多工站复用。
    /// </summary>
    public sealed class StationContext
    {
        private Dictionary<string, AddrOption> _addressIndex;

        public StationContext(
            ConfigManager config,
            IAppLogger logger,
            string baseDirectory = null)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));

            BaseDirectory = string.IsNullOrWhiteSpace(baseDirectory)
                ? AppDomain.CurrentDomain.BaseDirectory
                : baseDirectory;

            _addressIndex = config.Plc.BuildIndex(out var warning);
            if (!string.IsNullOrEmpty(warning))
            {
                Logger.Warn(warning.Trim());
            }

            State = new StationState();
            Alarms = new AlarmManager();
            Trace = new ProcessTrace();
            Live = new CycleLiveData();
            Session = UserSession.Anonymous;
        }

        public ConfigManager Config { get; }

        public AppConfig App => Config.App;

        public PlcConfig Plc => Config.Plc;

        public ApiConfig Api => Config.Api;

        public IAppLogger Logger { get; }

        public AlarmManager Alarms { get; }

        public StationState State { get; }

        public ProcessTrace Trace { get; }

        /// <summary>
        /// 可选的当前周期数据袋。引擎不读写它，「实时数据」页也不必使用。
        /// 工站若只要表格或曲线，可以自己往里面推；图像、标注、组合画面则完全不用它。
        /// </summary>
        public CycleLiveData Live { get; }

        /// <summary>当前登录会话。未启用登录时为匿名管理员，所有权限判断放行。</summary>
        public UserSession Session { get; set; }

        public string BaseDirectory { get; }

        public IPlcClient PlcClient { get; set; }

        public IApiClient ApiClient { get; set; }

        /// <summary>
        /// 从磁盘重新加载配置并重建地址索引。
        /// 配置编辑页保存后调用，无需重启程序即可让新地址表生效。
        /// </summary>
        public bool ReloadConfig(out string message)
        {
            var ok = Config.Load(out message);

            var index = Config.Plc.BuildIndex(out var warning);
            _addressIndex = index;

            if (!string.IsNullOrWhiteSpace(warning))
            {
                Logger.Warn(warning.Trim());
                message = (message ?? string.Empty) + warning;
            }

            return ok;
        }

        /// <summary>
        /// 校验地址键是否齐全，返回缺失的键名（用于配置页提示）。
        /// </summary>
        public IReadOnlyList<string> FindMissingAddressKeys(params string[] keys)
        {
            var missing = new List<string>();

            if (keys == null)
            {
                return missing;
            }

            foreach (var key in keys)
            {
                if (!TryGetAddress(key, out var option) || string.IsNullOrWhiteSpace(option.Address))
                {
                    missing.Add(key);
                }
            }

            return missing;
        }

        /// <summary>
        /// 相对路径落在配置目录下，避免数据库跟着被替换的程序目录一起丢掉。
        /// </summary>
        public string DatabasePath =>
            Path.IsPathRooted(App.DatabasePath)
                ? App.DatabasePath
                : Path.Combine(Config.ConfigDirectory, App.DatabasePath);

        public bool TryGetAddress(string key, out AddrOption option)
        {
            option = null;
            return !string.IsNullOrEmpty(key) && _addressIndex.TryGetValue(key, out option);
        }

        /// <summary>
        /// 取地址字符串。键名未配置时返回 null，并记录一次告警。
        /// </summary>
        public string GetAddress(string key)
        {
            if (TryGetAddress(key, out var option) && !string.IsNullOrWhiteSpace(option.Address))
            {
                return option.Address;
            }

            Logger.Warn($"地址表缺少键名：{key}");
            return null;
        }
    }
}
