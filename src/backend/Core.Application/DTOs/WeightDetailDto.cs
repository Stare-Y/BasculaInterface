using Core.Domain.Entities.Weight;

namespace Core.Application.DTOs
{
    public class WeightDetailDto
    {
        public int Id { get; set; }
        public int FK_WeightEntryId { get; set; }
        public double Tare { get; set; } = 0;
        public double Weight { get; set; } = 0;
        public int? Costales { get; set; } = null;
        public int? FK_WeightedProductId { get; set; }
        public string? WeightedBy { get; set; }
        public double? SecondaryTare { get; set; } = null;
        public double? RequiredAmount { get; set; } = null;
        public double? ProductPrice { get; set; } = null;
        public DateTime? LastUpdated { get; set; } = null;
        public string? Notes { get; set; } = null;

        /// <summary>
        /// If false, dont count its weight in the total and next weight entries.
        /// </summary>
        public bool IsLoaded { get; set; } = true;

        public WeightDetailDto() { }

        public WeightDetailDto(WeightDetail wd)
        {
            ArgumentNullException.ThrowIfNull(wd);
            Id = wd.Id;
            FK_WeightEntryId = wd.FK_WeightEntryId;
            FK_WeightedProductId = wd.FK_WeightedProductId;
            Tare = wd.Tare;
            Weight = wd.Weight;
            SecondaryTare = wd.SecondaryTare;
            WeightedBy = wd.WeightedBy;
            RequiredAmount = wd.RequiredAmount;
            ProductPrice = wd.ProductPrice;
            Costales = wd.Costales;
            LastUpdated = wd.LastUpdated;
            Notes = wd.Notes;
            IsLoaded = wd.IsLoaded;
        }

        public WeightDetail ToEntity()
        {
            return new WeightDetail
            {
                Id = Id,
                FK_WeightEntryId = FK_WeightEntryId,
                FK_WeightedProductId = FK_WeightedProductId,
                Tare = Tare,
                Weight = Weight,
                SecondaryTare = SecondaryTare,
                WeightedBy = WeightedBy,
                RequiredAmount = RequiredAmount,
                ProductPrice = ProductPrice,
                Costales = Costales,
                LastUpdated = LastUpdated,
                Notes = Notes,
                IsLoaded = IsLoaded
            };
        }
    }
}
