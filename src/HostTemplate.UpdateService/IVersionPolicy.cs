using System;
using System.Collections.Generic;

namespace HostTemplate.UpdateService
{
    /// <summary>
    /// 选出某工站当前应安装的版本。现在始终取已发布版本里最新的一个。
    /// 以后做灰度时换一个实现，按 stationId 返回指定版本，路由和客户端都不用改。
    /// </summary>
    public interface IVersionPolicy
    {
        string Name { get; }

        PackageRecord Select(string stationId, IReadOnlyList<PackageRecord> packages);
    }
}
