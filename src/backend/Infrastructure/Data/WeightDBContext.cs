using Core.Domain.Entities.Behaviors;
using Core.Domain.Entities.ProviderOrders;
using Core.Domain.Entities.Turns;
using Core.Domain.Entities.Weight;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;

namespace Infrastructure.Data
{
    public class WeightDBContext : DbContext
    {
        public DbSet<WeightEntry> WeightEntries { get; set; } = null!;
        public DbSet<WeightDetail> WeightDetails { get; set; } = null!;
        public DbSet<ExternalTargetBehavior> ExternalTargetBehaviors { get; set; } = null!;
        public DbSet<Turn> Turns { get; set; } = null!;
        public DbSet<ProviderPurchase> ProviderPurchases { get; set; } = null!;
        public WeightDBContext(DbContextOptions<WeightDBContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<WeightDetail>()
                .Property(wd => wd.IsLoaded)
                .HasDefaultValue(true);
        }
    }
}
