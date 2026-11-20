using System.Collections.Generic;
using System.ComponentModel;

namespace HostTemplate.Core.Configuration
{
    /// <summary>
    /// 接口请求头。
    /// </summary>
    public sealed class HeaderItem
    {
        [DisplayName("1.参数名")]
        public string Name { get; set; } = string.Empty;

        [DisplayName("2.参数值")]
        public string Value { get; set; } = string.Empty;
    }

    /// <summary>
    /// 接口端点。业务代码按 Key 取路径，不写死 URL。
    /// </summary>
    public sealed class EndpointItem
    {
        [DisplayName("1.键名"), Description("如 ApplySn / PassStation")]
        public string Key { get; set; } = string.Empty;

        [DisplayName("2.相对路径"), Description("如 /api/ProcessStation/RequestSnNumber")]
        public string Path { get; set; } = string.Empty;

        [DisplayName("3.方法")]
        public string Method { get; set; } = "POST";
    }

    /// <summary>
    /// MES / 上位系统接口配置。
    /// </summary>
    [DefaultProperty(nameof(BaseUrl))]
    public sealed class ApiConfig
    {
        [Category("[服务]"), DisplayName("01.基地址"), Description("如 http://192.168.1.10:8080")]
        public string BaseUrl { get; set; } = "http://127.0.0.1:8080";

        [Category("[服务]"), DisplayName("02.超时(ms)")]
        public int TimeoutMs { get; set; } = 5000;

        [Category("[服务]"), DisplayName("03.失败重试次数")]
        public int RetryCount { get; set; } = 3;

        [Category("[服务]"), DisplayName("04.重试退避基数(秒)")]
        public int RetryBackoffSeconds { get; set; } = 1;

        [Category("[服务]"), DisplayName("05.启用接口调用"), Description("关闭后所有过站调用直接放行，便于离线调试")]
        public bool Enabled { get; set; } = true;

        [Browsable(false)]
        [DisplayName("01.请求头")]
        public List<HeaderItem> Headers { get; set; } = new List<HeaderItem>();

        [Browsable(false)]
        [DisplayName("01.端点表")]
        public List<EndpointItem> Endpoints { get; set; } = new List<EndpointItem>();

        public string GetPath(string key, string fallback = "")
        {
            if (Endpoints == null)
            {
                return fallback;
            }

            foreach (var item in Endpoints)
            {
                if (item != null && string.Equals(item.Key, key, System.StringComparison.OrdinalIgnoreCase))
                {
                    return item.Path;
                }
            }

            return fallback;
        }
    }
}
