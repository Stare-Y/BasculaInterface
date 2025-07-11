using BasculaInterface.Models;
using BasculaInterface.ViewModels.Base;
using Core.Application.DTOs;
using Core.Application.Services;
using System.Collections.ObjectModel;

namespace BasculaInterface.ViewModels
{
    public class DetailedWeightViewModel : ViewModelBase
    {
        public WeightEntryDto? WeightEntry { get; private set; } = null;
        public ClienteProveedorDto? Partner { get; set; } = null;
        public double TotalWeight => WeightEntry?.WeightDetails?.Sum(d => d.Weight) + WeightEntry?.TareWeight ?? 0;
        public ObservableCollection<WeightEntryDetailRow> WeightEntryDetailRows { get; private set; } = new ObservableCollection<WeightEntryDetailRow>();
        
        private readonly IApiService _apiService = null!;

        public DetailedWeightViewModel(IApiService apiService)
        {
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
        }

        public DetailedWeightViewModel() { }

        public async Task LoadProductsAsync(WeightEntryDto weightEntry, ClienteProveedorDto? partner = null)
        {
            WeightEntry = weightEntry ?? throw new ArgumentNullException(nameof(weightEntry));

            Partner = partner ?? new ClienteProveedorDto { RazonSocial = "Socio no identificado"};

            if (WeightEntry == null)
            {
                throw new InvalidOperationException("WeightEntry must be set before loading products.");
            }

            WeightEntryDetailRows.Clear();

            if (WeightEntry.WeightDetails == null || !WeightEntry.WeightDetails.Any())
            {
                return; // No details to load
            }

            foreach (WeightDetailDto detail in WeightEntry.WeightDetails)
            {
                WeightEntryDetailRow row = new WeightEntryDetailRow
                {
                    Id = detail.Id,
                    Weight = detail.Weight,
                };

                if (detail.FK_WeightedProductId is not null)
                {
                    ProductoDto? product = await _apiService.GetAsync<ProductoDto>($"Productos/{detail.FK_WeightedProductId}");
                    row.Description = product?.Nombre ?? $"Unknown Product ({detail.FK_WeightedProductId})";
                }
                else
                {
                    row.Description = "Sin producto asignado";
                }

                WeightEntryDetailRows.Add(row);
            }

            //sort the rows by id, and assign the order index
            WeightEntryDetailRows = new ObservableCollection<WeightEntryDetailRow>(
                WeightEntryDetailRows.OrderBy(row => row.Id).Select((row, index) => 
                {
                    row.OrderIndex = index + 1;
                    return row;
                })
            );

            OnCollectionChanged(nameof(WeightEntryDetailRows));
        }
    }
}
