using System.Threading;
using System.Threading.Tasks;
using HostTemplate.Contracts;

namespace HostTemplate.Core.Updates
{
    public interface IStationServiceClient
    {
        Task<ConfigPackage> GetConfigAsync(string stationId, CancellationToken cancellationToken);

        Task<VersionListResponse> GetVersionsAsync(string stationId, CancellationToken cancellationToken);

        Task<TargetVersion> GetLatestAsync(string stationId, CancellationToken cancellationToken);
    }
}
