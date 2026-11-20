using System;
using System.Collections.Generic;
using System.IO;
using HostTemplate.Core.Abstractions;

namespace HostTemplate.Core.Configuration
{
    /// <summary>
    /// 配置总入口。三类配置分文件存放，缺文件时用默认值自动生成，
    /// 保证程序首次拷到现场机就能启动。
    /// </summary>
    public sealed class ConfigManager
    {
        public const string AppFileName = "app.xml";
        public const string PlcFileName = "plc.xml";
        public const string ApiFileName = "api.xml";

        public ConfigManager(string configDirectory)
        {
            ConfigDirectory = string.IsNullOrWhiteSpace(configDirectory)
                ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config")
                : configDirectory;
        }

        public string ConfigDirectory { get; }

        public AppConfig App { get; private set; } = new AppConfig();

        public PlcConfig Plc { get; private set; } = new PlcConfig();

        public ApiConfig Api { get; private set; } = new ApiConfig();

        public string AppPath => Path.Combine(ConfigDirectory, AppFileName);

        public string PlcPath => Path.Combine(ConfigDirectory, PlcFileName);

        public string ApiPath => Path.Combine(ConfigDirectory, ApiFileName);

        /// <summary>
        /// 加载全部配置。文件缺失或损坏时回落到默认值并重新生成，不阻断启动。
        /// </summary>
        public bool Load(out string message)
        {
            message = string.Empty;
            var ok = true;

            if (!XmlConfigStore.Load(AppPath, out AppConfig app, out var appError))
            {
                message += $"读取 {AppFileName} 失败，已使用默认值：{appError}\r\n";
                App = CreateDefaultApp();
                XmlConfigStore.Save(AppPath, App, out _);
            }
            else
            {
                App = app;
            }

            if (!XmlConfigStore.Load(PlcPath, out PlcConfig plc, out var plcError))
            {
                message += $"读取 {PlcFileName} 失败，已使用默认值：{plcError}\r\n";
                Plc = CreateDefaultPlc();
                XmlConfigStore.Save(PlcPath, Plc, out _);
            }
            else
            {
                Plc = plc;
            }

            if (!XmlConfigStore.Load(ApiPath, out ApiConfig api, out var apiError))
            {
                message += $"读取 {ApiFileName} 失败，已使用默认值：{apiError}\r\n";
                Api = CreateDefaultApi();
                XmlConfigStore.Save(ApiPath, Api, out _);
            }
            else
            {
                Api = api;
            }

            return ok;
        }

        /// <summary>
        /// 用编辑后的副本整体替换并落盘。三份文件分别写，失败项汇总返回。
        /// </summary>
        public bool SaveAll(AppConfig app, PlcConfig plc, ApiConfig api, out string error)
        {
            error = string.Empty;

            if (app == null || plc == null || api == null)
            {
                error = "配置对象为空，保存已取消。";
                return false;
            }

            App = app;
            Plc = plc;
            Api = api;

            var messages = new List<string>();

            if (!SaveApp(out var appError))
            {
                messages.Add($"{AppFileName}：{appError}");
            }

            if (!SavePlc(out var plcError))
            {
                messages.Add($"{PlcFileName}：{plcError}");
            }

            if (!SaveApi(out var apiError))
            {
                messages.Add($"{ApiFileName}：{apiError}");
            }

            error = string.Join("\r\n", messages);
            return messages.Count == 0;
        }

        public bool SaveApp(out string error)
        {
            return XmlConfigStore.Save(AppPath, App, out error);
        }

        public bool SavePlc(out string error)
        {
            return XmlConfigStore.Save(PlcPath, Plc, out error);
        }

        public bool SaveApi(out string error)
        {
            return XmlConfigStore.Save(ApiPath, Api, out error);
        }

        public static AppConfig CreateDefaultApp()
        {
            return new AppConfig();
        }

        public static PlcConfig CreateDefaultPlc()
        {
            return new PlcConfig
            {
                Addresses = new List<AddrOption>
                {
                    NewAddress(AddressKeys.Heartbeat, "RW", "M100.0", 0, "Bool"),
                    NewAddress(AddressKeys.Command, "R", "MW200", 2, "Word"),
                    NewAddress(AddressKeys.ReserveBool, "R", "M110.0", 16, "Bool"),
                    NewAddress(AddressKeys.ReviewResult, "RW", "MW210", 0, "Word"),
                    NewAddress(AddressKeys.MesSerialNumber, "W", "DB1.DBB0", 20, "String"),
                    NewAddress(AddressKeys.PlcSerialNumber, "R", "DB1.DBB100", 20, "String"),
                    NewAddress(AddressKeys.TrayNumber, "R", "MW220", 0, "Word"),
                }
            };
        }

        public static ApiConfig CreateDefaultApi()
        {
            return new ApiConfig
            {
                Headers = new List<HeaderItem>
                {
                    new HeaderItem { Name = "appId", Value = "template" },
                    new HeaderItem { Name = "appKey", Value = "change-me" },
                },
                Endpoints = new List<EndpointItem>
                {
                    new EndpointItem { Key = "ApplySn", Path = "/api/ProcessStation/RequestSnNumber" },
                    new EndpointItem { Key = "PassStation", Path = "/api/ProcessStation/PassStation" },
                    new EndpointItem { Key = "UploadTestData", Path = "/api/TestData/UpLoad" },
                    new EndpointItem { Key = "RegisterNg", Path = "/api/BadProduct/Register" },
                }
            };
        }

        private static AddrOption NewAddress(string name, string rw, string address, int length, string dataType)
        {
            return new AddrOption
            {
                Name = name,
                Rw = rw,
                Address = address,
                Length = length,
                DataType = dataType
            };
        }
    }
}
