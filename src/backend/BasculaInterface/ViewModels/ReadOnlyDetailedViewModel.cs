using BasculaInterface.Models;
using BasculaInterface.ViewModels.Base;
using Core.Application.DTOs;
using Core.Application.DTOs.ContpaqiComercial;
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
        public ObservableCollection<ExternalTargetBehaviorDto> ExternalTargetBehaviors { get; set; } = new ObservableCollection<ExternalTargetBehaviorDto>();
        public ReadOnlyDetailedViewModel() { }
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

        public async Task UpdateWeightEntry()
        {
            if (WeightEntry == null)
            {
                throw new InvalidOperationException("WeightEntry must be set before updating.");
            }

            // Send the updated weight entry to the API
            await _apiService.PutAsync<object>("api/Weight", WeightEntry);

            await FetchNewWeightDetails();
        }

        public async Task LoadExternalTargetBehaviors(CancellationToken cancellationToken = default)
        {
            ExternalTargetBehaviors.Clear();
            if (WeightEntry == null)
            {
                throw new InvalidOperationException("WeightEntry must be set before loading external target behaviors.");
            }
            var behaviors = await _apiService.GetAsync<List<ExternalTargetBehaviorDto>>($"api/ExternalTargetBehavior/Available", cancellationToken);
            foreach (var behavior in behaviors)
            {
                ExternalTargetBehaviors.Add(behavior);
            }
            OnCollectionChanged(nameof(ExternalTargetBehaviors));
        }

        public async Task<string> SendToContpaqiComercial()
        {
            if (WeightEntry == null)
            {
                throw new InvalidOperationException("WeightEntry must be set before sending to Contpaqi Comercial.");
            }

            GenericResponse<ContpaqiComercialResult> response = await _apiService.PostAsync<GenericResponse<ContpaqiComercialResult>>($"api/Weight/ContpaqiComercial?weightId={WeightEntry.Id}", WeightEntry);

            if (response.Data is null)
                throw new InvalidOperationException($"Resultado nulo ({response.Message}).");

            WeightEntry.ConptaqiComercialFK = response.Data.ResultingId;

            OnPropertyChanged(nameof(WeightEntry));

            return response.Message ?? "Enviado a Contpaqi Comercial correctamente.";
        }
        public async Task LoadProductsAsync(WeightEntryDto weightEntry, ClienteProveedorDto? partner = null, CancellationToken cancellationToken = default)
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

            // Batch fetch all products to avoid N+1 queries
            int[] productIds = WeightEntry.WeightDetails
                .Where(d => d.FK_WeightedProductId.HasValue)
                .Select(d => d.FK_WeightedProductId!.Value)
                .Distinct()
                .ToArray();

            Dictionary<int, ProductoDto> productsById = [];
            if (productIds.Length > 0)
            {
                string idsQuery = string.Join("&ids=", productIds);
                List<ProductoDto>? products = await _apiService.GetAsync<List<ProductoDto>>($"api/Productos/ByMultipleIds?ids={idsQuery}", cancellationToken);
                if (products != null)
                {
                    productsById = products.ToDictionary(p => p.Id);
                }
            }

            foreach (WeightDetailDto detail in WeightEntry.WeightDetails)
            {
                cancellationToken.ThrowIfCancellationRequested();

                WeightEntryDetailRow row = new WeightEntryDetailRow
                {
                    Id = detail.Id,
                    Tare = detail.Tare,
                    Weight = detail.Weight,
                    FK_WeightedProductId = detail.FK_WeightedProductId,
                    ProductPrice = detail.ProductPrice,
                    WeightedByDecorated = detail.WeightedBy == null
                                    ? null
                                    : detail.WeightedBy,
                    SecondaryTare = detail.SecondaryTare,
                    RequiredAmount = detail.RequiredAmount
                };

                if (detail.FK_WeightedProductId is not null && productsById.TryGetValue(detail.FK_WeightedProductId.Value, out ProductoDto? product))
                {
                    row.Description = product?.Nombre ?? $"Unknown Product ({detail.FK_WeightedProductId})";
                    row.IsGranel = product?.IsGranel ?? true;
                }
                else if (detail.FK_WeightedProductId is not null)
                {
                    row.Description = $"Unknown Product ({detail.FK_WeightedProductId})";
                    row.IsGranel = true;
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

        public async Task FetchNewWeightDetails(CancellationToken cancellationToken = default)
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
            WeightEntryDto? updatedEntry = await _apiService.GetAsync<WeightEntryDto>($"api/Weight/ById?id={WeightEntry.Id}", cancellationToken);
            if (updatedEntry == null)
            {
                throw new InvalidOperationException("Failed to fetch updated weight entry details.");
            }
            // Update the WeightEntry property and reload products
            WeightEntry = updatedEntry;
            await LoadProductsAsync(WeightEntry, Partner, cancellationToken);

            //ifweightentry has partnerid, fetch partner details
            if (WeightEntry.PartnerId.HasValue && WeightEntry.PartnerId.Value > 0)
            {
                Partner = await _apiService.GetAsync<ClienteProveedorDto>($"api/ClienteProveedor/ById?id={WeightEntry.PartnerId.Value}", cancellationToken);
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
