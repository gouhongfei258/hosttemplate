using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HostTemplate.Backend.Models;

namespace HostTemplate.Backend.Infrastructure
{
    public static class PackageHash
    {
        public static string Sha1(byte[] data)
        {
            return Hex(SHA1.HashData(data));
        }

        public static string Sha256(byte[] data)
        {
            return Hex(SHA256.HashData(data));
        }

        public static string Sha1File(string path)
        {
            using (var stream = File.OpenRead(path))
            {
                return Hex(SHA1.HashData(stream));
            }
        }

        public static string Sha256File(string path)
        {
            using (var stream = File.OpenRead(path))
            {
                return Hex(SHA256.HashData(stream));
            }
        }

        private static string Hex(byte[] hash)
        {
            return BitConverter.ToString(hash).Replace("-", string.Empty);
        }
    }

    public static class VelopackFeed
    {
        public const string IndexFileName = "releases.win.json";

        public static string ToJson(string packageId, PackageRecord record)
        {
            var payload = new
            {
                Assets = new[]
                {
                    new
                    {
                        PackageId = string.IsNullOrWhiteSpace(packageId) ? "HostTemplate" : packageId,
                        Version = record.Version,
                        Type = "Full",
                        FileName = record.FileName,
                        SHA1 = record.Sha1,
                        SHA256 = record.Sha256,
                        Size = record.Size,
                    }
                }
            };

            return JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
        }
    }

    internal sealed class PackageMeta
    {
        public string Version { get; set; } = string.Empty;

        public string FileName { get; set; } = string.Empty;

        public long Size { get; set; }

        public string Sha1 { get; set; } = string.Empty;

        public string Sha256 { get; set; } = string.Empty;

        public DateTime PublishedAtUtc { get; set; }

        public static PackageMeta FromRecord(PackageRecord record)
        {
            return new PackageMeta
            {
                Version = record.Version,
                FileName = record.FileName,
                Size = record.Size,
                Sha1 = record.Sha1,
                Sha256 = record.Sha256,
                PublishedAtUtc = record.PublishedAtUtc,
            };
        }

        public PackageRecord ToRecord()
        {
            return new PackageRecord
            {
                Version = Version,
                FileName = FileName,
                Size = Size,
                Sha1 = Sha1,
                Sha256 = Sha256,
                PublishedAtUtc = PublishedAtUtc,
            };
        }
    }
}
