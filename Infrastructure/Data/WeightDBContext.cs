using Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data
{
    public class WeightDBContext : DbContext
    {
        public DbSet<WeightEntry> WeightEntries { get; set; } = null!;
        public DbSet<WeightDetail> WeightDetails { get; set; } = null!;
        public WeightDBContext(DbContextOptions<WeightDBContext> options)
            : base(options)
        {
        }
    }
}
