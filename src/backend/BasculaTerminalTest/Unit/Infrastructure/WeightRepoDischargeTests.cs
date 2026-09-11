using Core.Domain.Entities.Weight;
using Infrastructure.Data;
using Infrastructure.Repos;
using Microsoft.EntityFrameworkCore;

namespace BasculaTerminalTest.Unit.Infrastructure
{
    /// <summary>
    /// <see cref="WeightRepo"/>'s running <c>BruteWeight</c> is computed differently depending
    /// on <see cref="WeightEntry.IsDischarge"/> (design.md Decision 7): the normal case adds
    /// each loaded detail's weight to the tare, a discharge entry subtracts it — with a guard
    /// against discharging more than the vehicle brought in. Each test gets its own EF Core
    /// InMemory database (no Docker/Postgres needed) so these run anywhere the Unit suite does.
    ///
    /// Seeding and exercising each use a fresh <see cref="WeightDBContext"/> instance against the
    /// same database name — mirroring the real per-request scoped context and avoiding
    /// change-tracker fixup silently bypassing WeightRepo's filtered `!IsDeleted` includes.
    /// </summary>
    public class WeightRepoDischargeTests
    {
        private static WeightDBContext CreateContext(string dbName) => new(
            new DbContextOptionsBuilder<WeightDBContext>().UseInMemoryDatabase(dbName).Options);

        private static async Task<(string DbName, int EntryId)> SeedEntryAsync(
            bool isDischarge, double tareWeight, params WeightDetail[] details)
        {
            string dbName = Guid.NewGuid().ToString();
            using WeightDBContext db = CreateContext(dbName);
            var entry = new WeightEntry { TareWeight = tareWeight, IsDischarge = isDischarge };
            db.WeightEntries.Add(entry);
            await db.SaveChangesAsync();

            foreach (WeightDetail detail in details)
            {
                detail.FK_WeightEntryId = entry.Id;
                db.WeightDetails.Add(detail);
            }
            await db.SaveChangesAsync();

            return (dbName, entry.Id);
        }

        // --- RecomputeBruteWeightAsync -----------------------------------------------

        [Fact]
        public async Task Normal_entry_adds_loaded_weight_to_tare()
        {
            (string dbName, int entryId) = await SeedEntryAsync(isDischarge: false, tareWeight: 1000,
                new WeightDetail { Weight = 200, IsLoaded = true });

            using WeightDBContext db = CreateContext(dbName);
            await new WeightRepo(db).RecomputeBruteWeightAsync(entryId);

            Assert.Equal(1200, (await db.WeightEntries.FindAsync(entryId))!.BruteWeight);
        }

        [Fact]
        public async Task Discharge_entry_subtracts_loaded_weight_from_tare()
        {
            (string dbName, int entryId) = await SeedEntryAsync(isDischarge: true, tareWeight: 1000,
                new WeightDetail { Weight = 200, IsLoaded = true });

            using WeightDBContext db = CreateContext(dbName);
            await new WeightRepo(db).RecomputeBruteWeightAsync(entryId);

            Assert.Equal(800, (await db.WeightEntries.FindAsync(entryId))!.BruteWeight);
        }

        [Fact]
        public async Task Discharge_entry_never_shows_the_vehicle_gaining_weight_as_details_accumulate()
        {
            (string dbName, int entryId) = await SeedEntryAsync(isDischarge: true, tareWeight: 1000,
                new WeightDetail { Weight = 300, IsLoaded = true });

            using (WeightDBContext firstDb = CreateContext(dbName))
            {
                await new WeightRepo(firstDb).RecomputeBruteWeightAsync(entryId);
            }
            double afterFirst;
            using (WeightDBContext readDb = CreateContext(dbName))
            {
                afterFirst = (await readDb.WeightEntries.FindAsync(entryId))!.BruteWeight;
            }

            using (WeightDBContext seedDb = CreateContext(dbName))
            {
                seedDb.WeightDetails.Add(new WeightDetail { FK_WeightEntryId = entryId, Weight = 200, IsLoaded = true });
                await seedDb.SaveChangesAsync();
            }
            using (WeightDBContext secondDb = CreateContext(dbName))
            {
                await new WeightRepo(secondDb).RecomputeBruteWeightAsync(entryId);
            }
            double afterSecond;
            using (WeightDBContext readDb = CreateContext(dbName))
            {
                afterSecond = (await readDb.WeightEntries.FindAsync(entryId))!.BruteWeight;
            }

            Assert.Equal(700, afterFirst);
            Assert.Equal(500, afterSecond);
            Assert.True(afterSecond < afterFirst, "The running total must fall as a discharge is captured, never climb.");
        }

        [Fact]
        public async Task Discharging_more_than_the_tare_throws()
        {
            (string dbName, int entryId) = await SeedEntryAsync(isDischarge: true, tareWeight: 100,
                new WeightDetail { Weight = 150, IsLoaded = true });

            using WeightDBContext db = CreateContext(dbName);
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => new WeightRepo(db).RecomputeBruteWeightAsync(entryId));
        }

        [Fact]
        public async Task Unloaded_and_deleted_details_are_excluded_from_the_running_total()
        {
            (string dbName, int entryId) = await SeedEntryAsync(isDischarge: true, tareWeight: 1000,
                new WeightDetail { Weight = 200, IsLoaded = true },
                new WeightDetail { Weight = 999, IsLoaded = false },
                new WeightDetail { Weight = 999, IsLoaded = true, IsDeleted = true });

            using WeightDBContext db = CreateContext(dbName);
            await new WeightRepo(db).RecomputeBruteWeightAsync(entryId);

            Assert.Equal(800, (await db.WeightEntries.FindAsync(entryId))!.BruteWeight);
        }

        // --- MarkDetailLoadedAsync mirrors the same arithmetic ------------------------

        [Fact]
        public async Task Marking_a_detail_loaded_on_a_discharge_entry_subtracts_its_weight()
        {
            (string dbName, int entryId) = await SeedEntryAsync(isDischarge: true, tareWeight: 1000,
                new WeightDetail { Weight = 250, IsLoaded = false });

            int detailId;
            using (WeightDBContext readDb = CreateContext(dbName))
            {
                detailId = (await readDb.WeightDetails.SingleAsync(d => d.FK_WeightEntryId == entryId)).Id;
            }

            using WeightDBContext db = CreateContext(dbName);
            WeightEntry updated = await new WeightRepo(db).MarkDetailLoadedAsync(detailId);

            Assert.Equal(750, updated.BruteWeight);
        }

        // --- UpdateAsync copies IsDischarge and recomputes ----------------------------

        [Fact]
        public async Task UpdateAsync_copies_IsDischarge_from_the_incoming_entry_and_recomputes()
        {
            (string dbName, int entryId) = await SeedEntryAsync(isDischarge: false, tareWeight: 1000,
                new WeightDetail { Weight = 200, IsLoaded = true });

            using (WeightDBContext db = CreateContext(dbName))
            {
                await new WeightRepo(db).UpdateAsync(
                    new WeightEntry { Id = entryId, TareWeight = 1000, IsDischarge = true, VehiclePlate = "TEST-1" });
            }

            using WeightDBContext readDb = CreateContext(dbName);
            WeightEntry reloaded = (await readDb.WeightEntries.FindAsync(entryId))!;
            Assert.True(reloaded.IsDischarge);
            Assert.Equal(800, reloaded.BruteWeight);
        }

        [Fact]
        public async Task UpdateAsync_on_a_normal_entry_is_unaffected_by_the_discharge_branch()
        {
            (string dbName, int entryId) = await SeedEntryAsync(isDischarge: false, tareWeight: 1000,
                new WeightDetail { Weight = 200, IsLoaded = true });

            using (WeightDBContext db = CreateContext(dbName))
            {
                await new WeightRepo(db).UpdateAsync(
                    new WeightEntry { Id = entryId, TareWeight = 1000, IsDischarge = false, VehiclePlate = "TEST-2" });
            }

            using WeightDBContext readDb = CreateContext(dbName);
            WeightEntry reloaded = (await readDb.WeightEntries.FindAsync(entryId))!;
            Assert.False(reloaded.IsDischarge);
            Assert.Equal(1200, reloaded.BruteWeight);
        }
    }
}
