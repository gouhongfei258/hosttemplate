using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HostTemplate.UpdateService
{
    /// <summary>
    /// 定时把 inbox 里新放下的安装包和工站配置收进正式目录。查询接口也会顺手收一次。
    /// </summary>
    public sealed class InboxImportService : BackgroundService
    {
        private readonly FileUpdateStore _store;
        private readonly ILogger<InboxImportService> _logger;

        public InboxImportService(FileUpdateStore store, ILogger<InboxImportService> logger)
        {
            _store = store;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var count = _store.ImportInbox();
                    if (count > 0)
                    {
                        _logger.LogInformation("已从 inbox 导入 {Count} 项。", count);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "扫描 inbox 失败。");
                }

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }
}
