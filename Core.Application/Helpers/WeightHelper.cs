using Core.Application.DTOs;
using Core.Domain.Entities;

namespace Core.Application.Helpers
{
    public static class WeightHelper
    {
        public static WeightEntry BuildFromDto(this WeightEntry weightEntry, WeightEntryDto weightEntryDto)
        {
            if (weightEntryDto == null)
            {
                throw new ArgumentNullException(nameof(weightEntryDto), "WeightEntryDto cannot be null");
            }

            weightEntry = new WeightEntry
            {
                PartnerId = weightEntryDto.PartnerId,
                TareWeight = weightEntryDto.TareWeight,
                NetWeight = weightEntryDto.NetWeight,
                ConcludeDate = weightEntryDto.ConcludeDate,
                Notes = weightEntryDto.Notes,
                WeightDetails = weightEntryDto.WeightDetails.Select(wd => new WeightDetail
                {
                    FK_WeightedProductId = wd.FK_WeightedProductId,
                    Weight = wd.Weight
                }).ToList()
            };

            return weightEntry;
        }
        public static WeightEntry UpdateFromDto(this WeightEntry weightEntry, WeightEntryDto weightEntryDto)
        {
            if (weightEntry == null)
            {
                throw new ArgumentNullException(nameof(weightEntry), "WeightEntry cannot be null");
            }
            if (weightEntryDto == null)
            {
                throw new ArgumentNullException(nameof(weightEntryDto), "WeightEntryDto cannot be null");
            }
            weightEntry.PartnerId = weightEntryDto.PartnerId;
            weightEntry.TareWeight = weightEntryDto.TareWeight;
            weightEntry.NetWeight = weightEntryDto.NetWeight;
            weightEntry.ConcludeDate = weightEntryDto.ConcludeDate;
            weightEntry.Notes = weightEntryDto.Notes;

            // Update WeightDetails
            var existingDetails = weightEntry.WeightDetails.ToList(); // Get existing details
            var updatedDetails = weightEntryDto.WeightDetails.ToList(); // Get updated details from DTO

            // Update existing WeightDetails
            foreach (var existingDetail in existingDetails)
            {
                var updatedDetail = updatedDetails.FirstOrDefault(wd => wd.FK_WeightedProductId == existingDetail.FK_WeightedProductId);
                if (updatedDetail != null)
                {
                    // Update properties of existing detail
                    existingDetail.Weight = updatedDetail.Weight;
                }
            }

            // Add new WeightDetails
            foreach (var newDetail in updatedDetails)
            {
                if (!existingDetails.Any(ed => ed.FK_WeightedProductId == newDetail.FK_WeightedProductId))
                {
                    weightEntry.WeightDetails.Add(new WeightDetail
                    {
                        FK_WeightedProductId = newDetail.FK_WeightedProductId,
                        Weight = newDetail.Weight
                    });
                }
            }

            // Remove WeightDetails that are no longer present in the DTO
            foreach (var existingDetail in existingDetails.ToList())
            {
                if (!updatedDetails.Any(ud => ud.FK_WeightedProductId == existingDetail.FK_WeightedProductId))
                {
                    weightEntry.WeightDetails.Remove(existingDetail);
                }
            }

            return weightEntry;
        }
    }
}
