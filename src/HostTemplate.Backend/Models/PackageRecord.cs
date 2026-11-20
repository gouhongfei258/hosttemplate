using System;

namespace HostTemplate.Backend.Models
{
    public sealed class PackageRecord
    {
        public string Version { get; set; } = string.Empty;

        public string FileName { get; set; } = string.Empty;

        public long Size { get; set; }

        public string Sha1 { get; set; } = string.Empty;

        public string Sha256 { get; set; } = string.Empty;

        public DateTime PublishedAtUtc { get; set; }
    }
}
