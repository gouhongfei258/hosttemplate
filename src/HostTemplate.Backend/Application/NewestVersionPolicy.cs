using System;
using System.Collections.Generic;
using HostTemplate.Backend.Models;
using HostTemplate.Contracts;

namespace HostTemplate.Backend.Application
{
    public sealed class NewestVersionPolicy : IVersionPolicy
    {
        public string Name => "newest";

        public PackageRecord Select(string stationId, IReadOnlyList<PackageRecord> packages)
        {
            PackageRecord best = null;

            if (packages == null)
            {
                return null;
            }

            foreach (var package in packages)
            {
                if (package == null || string.IsNullOrWhiteSpace(package.Version))
                {
                    continue;
                }

                if (best == null || AppVersion.Compare(package.Version, best.Version) > 0)
                {
                    best = package;
                }
            }

            return best;
        }
    }
}
