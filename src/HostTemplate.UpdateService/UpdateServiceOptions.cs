namespace HostTemplate.UpdateService
{
    public sealed class UpdateServiceOptions
    {
        public string DataRoot { get; set; } = "data";

        public string Token { get; set; } = string.Empty;

        public string PublicBaseUrl { get; set; } = string.Empty;

        public string PackageId { get; set; } = "HostTemplate";
    }
}
