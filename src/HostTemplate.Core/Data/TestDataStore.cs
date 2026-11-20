using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace HostTemplate.Core.Data
{
    /// <summary>
    /// 测试数据与报警的落库入口。实时界面不经过这里。
    /// </summary>
    public sealed class TestDataStore
    {
        private readonly string _databasePath;

        public TestDataStore(string databasePath)
        {
            if (string.IsNullOrWhiteSpace(databasePath))
            {
                throw new ArgumentException("数据库路径不能为空。", nameof(databasePath));
            }

            _databasePath = databasePath;
        }

        public string DatabasePath => _databasePath;

        /// <summary>库文件不存在时创建，已存在则不动表。</summary>
        public void EnsureCreated()
        {
            var directory = Path.GetDirectoryName(_databasePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (var db = HostDbContext.Create(_databasePath))
            {
                db.Database.EnsureCreated();
            }
        }

        /// <summary>
        /// 保存一条测试记录（含测试项）。
        /// </summary>
        /// <param name="record">主记录。</param>
        /// <param name="attachment">可选附件说明，例如曲线文件名。没有就传 null。</param>
        public async Task<long> SaveAsync(
            TestDataRecord record,
            string attachment,
            CancellationToken cancellationToken = default)
        {
            if (record == null)
            {
                throw new ArgumentNullException(nameof(record));
            }

            if (attachment != null)
            {
                record.Attachment = attachment;
            }

            using (var db = HostDbContext.Create(_databasePath))
            {
                db.TestDataSet.Add(record);
                await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return record.Id;
            }
        }

        public async Task SaveAlarmAsync(AlarmRecord alarm, CancellationToken cancellationToken = default)
        {
            if (alarm == null)
            {
                throw new ArgumentNullException(nameof(alarm));
            }

            using (var db = HostDbContext.Create(_databasePath))
            {
                db.AlarmSet.Add(alarm);
                await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>最近若干条，新的在前。只给调用方用，主界面不做历史查询。</summary>
        public async Task<IReadOnlyList<TestDataRecord>> QueryRecentAsync(
            int count,
            CancellationToken cancellationToken = default)
        {
            var take = count <= 0 ? 1 : count;

            using (var db = HostDbContext.Create(_databasePath))
            {
                return await db.TestDataSet
                    .AsNoTracking()
                    .Include(x => x.Items)
                    .OrderByDescending(x => x.Id)
                    .Take(take)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
        }
    }
}
