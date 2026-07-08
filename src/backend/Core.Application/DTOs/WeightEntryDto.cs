using Core.Domain.Entities.Weight;

namespace Core.Application.DTOs
{
    public class WeightEntryDto
    {
        public int Id { get; set; }
        public int? PartnerId { get; set; }
        public int? ConptaqiComercialFK { get; set; }
        public string? ContpaqiComercialFolio { get; set; } = null;
        public int? ExternalTargetBehaviorFK { get; set; }
        public double TareWeight { get; set; } = 0; //initial weight
        public double BruteWeight { get; set; } = 0; //final weight
        public DateTime? ConcludeDate { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string VehiclePlate { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public string? RegisteredBy { get; set; }
        public ExternalTargetBehaviorDto? ExternalTargetBehavior { get; set; }
        public ICollection<WeightDetailDto> WeightDetails { get; set; } = [];

        public WeightEntryDto() { }

        public WeightEntryDto(WeightEntry entry)
        {
            ArgumentNullException.ThrowIfNull(entry);
            Id = entry.Id;
            ConptaqiComercialFK = entry.ConptaqiComercialFK;
            ContpaqiComercialFolio = entry.ContpaqiComercialFolio;
            ExternalTargetBehaviorFK = entry.ExternalTargetBehaviorFK;
            ExternalTargetBehavior = entry.ExternalTargetBehavior is not null
                ? new ExternalTargetBehaviorDto(entry.ExternalTargetBehavior)
                : null;
            PartnerId = entry.PartnerId;
            TareWeight = entry.TareWeight;
            BruteWeight = entry.BruteWeight;
            ConcludeDate = entry.ConcludeDate;
            CreatedAt = entry.CreatedAt;
            Notes = entry.Notes;
            VehiclePlate = entry.VehiclePlate;
            RegisteredBy = entry.RegisteredBy;
            WeightDetails = [.. entry.WeightDetails.Select(wd => new WeightDetailDto(wd))];
        }

        public WeightEntry ToEntity()
        {
            return new WeightEntry
            {
                Id = Id,
                ConptaqiComercialFK = ConptaqiComercialFK,
                ContpaqiComercialFolio = ContpaqiComercialFolio,
                ExternalTargetBehaviorFK = ExternalTargetBehaviorFK,
                PartnerId = PartnerId,
                TareWeight = TareWeight,
                BruteWeight = BruteWeight,
                ConcludeDate = ConcludeDate,
                Notes = Notes,
                VehiclePlate = VehiclePlate,
                RegisteredBy = RegisteredBy,
                WeightDetails = [.. WeightDetails.Select(wd => wd.ToEntity())]
            };
        }

        public WeightEntry ApplyTo(WeightEntry entry)
        {
            ArgumentNullException.ThrowIfNull(entry);
            entry.PartnerId = PartnerId;
            entry.TareWeight = TareWeight;
            entry.BruteWeight = BruteWeight;
            entry.ConcludeDate = ConcludeDate;
            entry.Notes = Notes;
            entry.VehiclePlate = VehiclePlate;
            entry.RegisteredBy = RegisteredBy;
            entry.ExternalTargetBehaviorFK = ExternalTargetBehaviorFK;
            entry.ConptaqiComercialFK = ConptaqiComercialFK;
            entry.ContpaqiComercialFolio = ContpaqiComercialFolio;

            var existingDetails = entry.WeightDetails.ToList();
            var updatedDetails = WeightDetails.ToList();

            foreach (WeightDetail existingDetail in existingDetails)
            {
                var updatedDetail = updatedDetails.FirstOrDefault(wd => wd.FK_WeightedProductId == existingDetail.FK_WeightedProductId);
                if (updatedDetail != null)
                {
                    existingDetail.Weight = updatedDetail.Weight;
                    existingDetail.Tare = updatedDetail.Tare;
                    existingDetail.SecondaryTare = updatedDetail.SecondaryTare;
                    existingDetail.WeightedBy = updatedDetail.WeightedBy;
                    existingDetail.RequiredAmount = updatedDetail.RequiredAmount;
                    existingDetail.ProductPrice = updatedDetail.ProductPrice;
                    existingDetail.Costales = updatedDetail.Costales;
                    existingDetail.Notes = updatedDetail.Notes;
                }
            }

            foreach (WeightDetailDto newDetail in updatedDetails)
            {
                if (!existingDetails.Any(ed => ed.FK_WeightedProductId == newDetail.FK_WeightedProductId))
                {
                    entry.WeightDetails.Add(new WeightDetail
                    {
                        FK_WeightedProductId = newDetail.FK_WeightedProductId,
                        Tare = newDetail.Tare,
                        Weight = newDetail.Weight,
                        SecondaryTare = newDetail.SecondaryTare,
                        WeightedBy = newDetail.WeightedBy,
                        RequiredAmount = newDetail.RequiredAmount,
                        ProductPrice = newDetail.ProductPrice,
                        Costales = newDetail.Costales,
                        Notes = newDetail.Notes,
                    });
                }
            }

            foreach (WeightDetail existingDetail in existingDetails.ToList())
            {
                if (!updatedDetails.Any(ud => ud.FK_WeightedProductId == existingDetail.FK_WeightedProductId))
                {
                    entry.WeightDetails.Remove(existingDetail);
                }
            }

            return entry;
        }
    }
}
