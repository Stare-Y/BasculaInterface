using Core.Application.DTOs;
using Core.Domain.Entities;
using Core.Domain.Entities.ContpaqiSQL;

namespace Core.Application.Extensions
{
    public static class WeightExtensions
    {
        public static IEnumerable<ClienteProveedorDto> BuildFromBaseEntity(IEnumerable<ClienteProveedor> clienteProveedors)
        {
            return clienteProveedors.Select(cp => new ClienteProveedorDto
            {
                Id = cp.CIDCLIENTEPROVEEDOR,
                RazonSocial = cp.CRAZONSOCIAL,
                RFC = cp.CRFC
            });
        }
        public static IEnumerable<ProductoDto> BuildFromBaseEntity(IEnumerable<Producto> productos)
        {
            return productos.Select(p => new ProductoDto
            {
                Id = p.CIDPRODUCTO,
                Nombre = p.CNOMBREPRODUCTO,
                IdValorClasificacion6 = p.CIDVALORCLASIFICACION6
            });
        }
        public static ProductoDto BuildFromBaseEntity(Producto producto)
        {
            if (producto == null)
            {
                throw new ArgumentNullException(nameof(producto), "Producto cannot be null");
            }
            return new ProductoDto
            {
                Id = producto.CIDPRODUCTO,
                Nombre = producto.CNOMBREPRODUCTO,
                IdValorClasificacion6 = producto.CIDVALORCLASIFICACION6
            };
        }
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
                BruteWeight = weightEntryDto.BruteWeight,
                ConcludeDate = weightEntryDto.ConcludeDate,
                Notes = weightEntryDto.Notes,
                VehiclePlate = weightEntryDto.VehiclePlate,
                RegisteredBy = weightEntryDto.RegisteredBy,
                WeightDetails = weightEntryDto.WeightDetails.Select(wd => new WeightDetail
                {
                    Id = wd.Id,
                    FK_WeightEntryId = wd.FK_WeightEntryId,
                    FK_WeightedProductId = wd.FK_WeightedProductId,
                    Tare = wd.Tare,
                    Weight = wd.Weight,
                    SecondaryTare = wd.SecondaryTare,
                    WeightedBy = wd.WeightedBy
                }).ToList()
            };

            return weightEntry;
        }

        public static WeightEntryDto ConvertToDto(this WeightEntry weightEntry)
        {
            if (weightEntry == null)
            {
                throw new ArgumentNullException(nameof(weightEntry), "WeightEntry cannot be null");
            }
            return new WeightEntryDto
            {
                Id = weightEntry.Id,
                PartnerId = weightEntry.PartnerId,
                TareWeight = weightEntry.TareWeight,
                BruteWeight = weightEntry.BruteWeight,
                ConcludeDate = weightEntry.ConcludeDate,
                Notes = weightEntry.Notes,
                VehiclePlate = weightEntry.VehiclePlate,
                RegisteredBy = weightEntry.RegisteredBy,
                WeightDetails = weightEntry.WeightDetails.Select(wd => new WeightDetailDto
                {
                    Id = wd.Id,
                    FK_WeightEntryId = wd.FK_WeightEntryId,
                    FK_WeightedProductId = wd.FK_WeightedProductId,
                    Tare = wd.Tare,
                    Weight = wd.Weight,
                    SecondaryTare = wd.SecondaryTare,
                    WeightedBy = wd.WeightedBy
                }).ToList()
            };
        }

        public static WeightEntry ConvertToBaseEntry(this WeightEntryDto weightEntryDto)
        {
            if (weightEntryDto == null)
            {
                throw new ArgumentNullException(nameof(weightEntryDto), "WeightEntryDto cannot be null");
            }
            return new WeightEntry
            {
                Id = weightEntryDto.Id,
                PartnerId = weightEntryDto.PartnerId,
                TareWeight = weightEntryDto.TareWeight,
                BruteWeight = weightEntryDto.BruteWeight,
                ConcludeDate = weightEntryDto.ConcludeDate,
                Notes = weightEntryDto.Notes,
                VehiclePlate = weightEntryDto.VehiclePlate,
                RegisteredBy = weightEntryDto.RegisteredBy,
                WeightDetails = weightEntryDto.WeightDetails.Select(wd => new WeightDetail
                {
                    Id = wd.Id,
                    FK_WeightEntryId = wd.FK_WeightEntryId,
                    FK_WeightedProductId = wd.FK_WeightedProductId,
                    Tare = wd.Tare,
                    Weight = wd.Weight,
                    WeightedBy = wd.WeightedBy,
                    SecondaryTare = wd.SecondaryTare,
                    RequiredAmount = wd.RequiredAmount
                }).ToList()
            };
        }

        public static IEnumerable<WeightEntryDto> ConvertRangeToDto(this IEnumerable<WeightEntry> weightEntries)
        {
            if (weightEntries == null)
            {
                throw new ArgumentNullException(nameof(weightEntries), "WeightEntries cannot be null");
            }
            return weightEntries.Select(we => new WeightEntryDto
            {
                Id = we.Id,
                PartnerId = we.PartnerId,
                TareWeight = we.TareWeight,
                BruteWeight = we.BruteWeight,
                ConcludeDate = we.ConcludeDate,
                Notes = we.Notes,
                VehiclePlate = we.VehiclePlate,
                RegisteredBy = we.RegisteredBy,
                WeightDetails = we.WeightDetails.Select(wd => new WeightDetailDto
                {
                    Id = wd.Id,
                    FK_WeightEntryId = wd.FK_WeightEntryId,
                    FK_WeightedProductId = wd.FK_WeightedProductId,
                    Tare = wd.Tare,
                    Weight = wd.Weight,
                    SecondaryTare = wd.SecondaryTare,
                    WeightedBy = wd.WeightedBy
                }).ToList()
            });
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
            weightEntry.BruteWeight = weightEntryDto.BruteWeight;
            weightEntry.ConcludeDate = weightEntryDto.ConcludeDate;
            weightEntry.Notes = weightEntryDto.Notes;
            weightEntry.VehiclePlate = weightEntryDto.VehiclePlate;
            weightEntry.RegisteredBy = weightEntryDto.RegisteredBy;

            // Update WeightDetails
            var existingDetails = weightEntry.WeightDetails.ToList(); // Get existing details
            var updatedDetails = weightEntryDto.WeightDetails.ToList(); // Get updated details from DTO

            // Update existing WeightDetails
            foreach (WeightDetail existingDetail in existingDetails)
            {
                var updatedDetail = updatedDetails.FirstOrDefault(wd => wd.FK_WeightedProductId == existingDetail.FK_WeightedProductId);
                if (updatedDetail != null)
                {
                    // Update properties of existing detail
                    existingDetail.Weight = updatedDetail.Weight;
                    existingDetail.Tare = updatedDetail.Tare;
                    existingDetail.SecondaryTare = updatedDetail.SecondaryTare;
                    existingDetail.WeightedBy = updatedDetail.WeightedBy;
                }
            }

            // Add new WeightDetails
            foreach (WeightDetailDto newDetail in updatedDetails)
            {
                if (!existingDetails.Any(ed => ed.FK_WeightedProductId == newDetail.FK_WeightedProductId))
                {
                    weightEntry.WeightDetails.Add(new WeightDetail
                    {
                        FK_WeightedProductId = newDetail.FK_WeightedProductId,
                        Tare = newDetail.Tare,
                        Weight = newDetail.Weight,
                        SecondaryTare = newDetail.SecondaryTare,
                        WeightedBy = newDetail.WeightedBy
                    });
                }
            }

            // Remove WeightDetails that are no longer present in the DTO
            foreach (WeightDetail existingDetail in existingDetails.ToList())
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
