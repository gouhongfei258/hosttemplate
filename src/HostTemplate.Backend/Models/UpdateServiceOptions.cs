namespace HostTemplate.Backend.Models
{
    /// <summary>
    /// 绑定 appsettings 的 UpdateService 节。节名保持不变，已经部署的配置不用改。
    /// </summary>
    public sealed class UpdateServiceOptions
    {
        public string DataRoot { get; set; } = "data";

        public string Token { get; set; } = string.Empty;

        public string PublicBaseUrl { get; set; } = string.Empty;

        public string PackageId { get; set; } = "HostTemplate";
    }
}
