using Core.Domain.Entities.Audit;
using Core.Domain.Entities.Behaviors;
using Core.Domain.Entities.Identity;
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
        public DbSet<Pedido> Pedidos { get; set; } = null!;
        public DbSet<PedidoLine> PedidoLines { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<AuditLogEntry> AuditLogEntries { get; set; } = null!;
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

            modelBuilder.Entity<WeightDetail>(wd =>
            {
                wd.HasOne(d => d.PedidoLine)
                    .WithMany(pl => pl.WeightDetails)
                    .HasForeignKey(d => d.FK_PedidoLineId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<WeightEntry>(we =>
            {
                we.Property<uint>("xmin")
                    .HasColumnType("xid")
                    .ValueGeneratedOnAddOrUpdate()
                    .IsConcurrencyToken();
            });

            // add-user-authentication-and-audit-log: Username/UserCode must be unique so login's
            // UserCode-then-Username resolution (design.md Decision 6) can never be ambiguous.
            modelBuilder.Entity<User>(u =>
            {
                u.HasIndex(x => x.Username).IsUnique();
                u.HasIndex(x => x.UserCode).IsUnique();

                // fix-session-inactivity-timeout: database-level default so a row inserted outside
                // the app (e.g. a manually-seeded Sudo row) never ends up with an unset value.
                u.Property(x => x.InactivityTimeoutMinutes).HasDefaultValue(5);
            });
        }
    }
}
