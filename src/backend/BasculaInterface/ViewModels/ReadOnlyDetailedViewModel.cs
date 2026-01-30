using BasculaInterface.Models;
using BasculaInterface.ViewModels.Base;
using Core.Application.DTOs;
using Core.Application.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;

namespace BasculaInterface.ViewModels
{
    public class ReadOnlyDetailedViewModel : ViewModelBase
    {
        public WeightEntryDto? WeightEntry { get; private set; } = null;
        public ClienteProveedorDto? Partner { get; set; } = null;
        public double TotalWeight => WeightEntry?.WeightDetails?.Sum(d => d.Weight) + WeightEntry?.TareWeight ?? 0;
        public ObservableCollection<WeightEntryDetailRow> WeightEntryDetailRows { get; private set; } = new ObservableCollection<WeightEntryDetailRow>();
        private readonly IApiService _apiService = null!;
        public ReadOnlyDetailedViewModel(IApiService apiService)
        {
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
            RefreshCommand = new Command(async () =>
            {
                try
                {
                    await FetchNewWeightDetails();
                }
                catch (Exception ex)
                {
                    // Handle exceptions, e.g., show an alert or log the error
                    throw new Exception("Error refreshing weight details: " + ex.Message);
                }
                finally
                {
                    IsRefreshing = false;
                }
            });
        }
        public double TotalCost => WeightEntryDetailRows.Sum(row =>
        {
            if (row.FK_WeightedProductId.HasValue && row.RequiredAmount.HasValue && row.ProductPrice.HasValue)
            {
                return row.ProductPrice.Value * row.RequiredAmount.Value;
            }
            return 0;
        });

        public async Task PrintTicketAsync()
        {
            if (WeightEntry == null)
            {
                throw new InvalidOperationException("WeightEntry must be set before printing.");
            }
            try
            {
                await _apiService.PostAsync<object>("api/Print/WeightEntry", WeightEntry);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error printing ticket: " + ex.Message);
            }
        }

        public async Task<string> SendToContpaqiComercial()
        {
            if (WeightEntry == null)
            {
                throw new InvalidOperationException("WeightEntry must be set before sending to Contpaqi Comercial.");
            }

            GenericResponse<int?>  response = await _apiService.PostAsync<GenericResponse<int?>>($"api/Weight/ContpaqiComercial?weightId={WeightEntry.Id}", WeightEntry);

            WeightEntry.ConptaqiComercialFK = response.Data;

            OnPropertyChanged(nameof(WeightEntry));

            return response.Message ?? "Enviado a Contpaqi Comercial correctamente.";
        }
        public async Task LoadProductsAsync(WeightEntryDto weightEntry, ClienteProveedorDto? partner = null)
        {
            WeightEntry = weightEntry;

            Partner = partner ?? new ClienteProveedorDto { RazonSocial = "No identificado" };

            if (WeightEntry == null)
            {
                throw new InvalidOperationException("WeightEntry must be set before loading products.");
            }

            WeightEntryDetailRows.Clear();

            if (WeightEntry.WeightDetails == null || WeightEntry.WeightDetails.Count == 0)
            {
                return; // No details to load
            }

            foreach (WeightDetailDto detail in WeightEntry.WeightDetails)
            {
                WeightEntryDetailRow row = new WeightEntryDetailRow
                {
                    Id = detail.Id,
                    Tare = detail.Tare,
                    Weight = detail.Weight,
                    FK_WeightedProductId = detail.FK_WeightedProductId,
                    ProductPrice = detail.ProductPrice,
                    WeightedBy = detail.WeightedBy == null
                                    ? null
                                    : detail.WeightedBy,
                    SecondaryTare = detail.SecondaryTare,
                    RequiredAmount = detail.RequiredAmount
                };

                if (detail.FK_WeightedProductId is not null)
                {
                    ProductoDto? product = await _apiService.GetAsync<ProductoDto>($"api/Productos/ById?id={detail.FK_WeightedProductId}");
                    row.Description = product?.Nombre ?? $"Unknown Product ({detail.FK_WeightedProductId})";
                    row.IsGranel = product?.IsGranel ?? true;
                }
                else
                {
                    row.Description = "Peso Libre";
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

        public async Task FetchNewWeightDetails()
        {
            if (WeightEntry == null)
            {
                throw new InvalidOperationException("WeightEntry must be set before fetching new details.");
            }
            if (WeightEntry.Id <= 0)
            {
                throw new InvalidOperationException("WeightEntry.Id must be a valid positive integer.");
            }
            // Fetch the latest weight entry details from the API
            WeightEntryDto? updatedEntry = await _apiService.GetAsync<WeightEntryDto>($"api/Weight/ById?id={WeightEntry.Id}");
            if (updatedEntry == null)
            {
                throw new InvalidOperationException("Failed to fetch updated weight entry details.");
            }
            // Update the WeightEntry property and reload products
            WeightEntry = updatedEntry;
            await LoadProductsAsync(WeightEntry, Partner);

            //ifweightentry has partnerid, fetch partner details
            if (WeightEntry.PartnerId.HasValue && WeightEntry.PartnerId.Value > 0)
            {
                Partner = await _apiService.GetAsync<ClienteProveedorDto>($"api/ClienteProveedor/ById?id={WeightEntry.PartnerId.Value}");
            }

            OnPropertyChanged(nameof(WeightEntry));
            OnPropertyChanged(nameof(Partner));
            OnPropertyChanged(nameof(TotalWeight));
        }

        private bool isRefreshing;
        public bool IsRefreshing
        {
            get => isRefreshing;
            set
            {
                if (isRefreshing == value) return;
                isRefreshing = value;
                OnPropertyChanged(nameof(IsRefreshing));
            }
        }

        public ICommand? RefreshCommand { get; }

    }
}
