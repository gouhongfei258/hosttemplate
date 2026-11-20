using Microsoft.EntityFrameworkCore;

namespace HostTemplate.Core.Data
{
    /// <summary>
    /// 本地 SQLite。表结构用 EnsureCreated 建，模板不走迁移。
    /// </summary>
    public sealed class HostDbContext : DbContext
    {
        public HostDbContext(DbContextOptions<HostDbContext> options)
            : base(options)
        {
        }

        public DbSet<TestDataRecord> TestDataSet { get; set; }

        public DbSet<TestDataItemRecord> TestDataItemSet { get; set; }

        public DbSet<AlarmRecord> AlarmSet { get; set; }

        public static HostDbContext Create(string databasePath)
        {
            var options = new DbContextOptionsBuilder<HostDbContext>()
                .UseSqlite("Data Source=" + databasePath)
                .Options;

            return new HostDbContext(options);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TestDataRecord>(entity =>
            {
                entity.ToTable("TestData");
                entity.HasKey(x => x.Id);
                entity.HasMany(x => x.Items)
                    .WithOne(x => x.Record)
                    .HasForeignKey(x => x.RecordId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<TestDataItemRecord>(entity =>
            {
                entity.ToTable("TestDataItem");
                entity.HasKey(x => x.Id);
            });

            modelBuilder.Entity<AlarmRecord>(entity =>
            {
                entity.ToTable("Alarm");
                entity.HasKey(x => x.Id);
            });
        }
    }
}
