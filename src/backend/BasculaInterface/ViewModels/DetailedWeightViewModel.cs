using BasculaInterface.Models;
using BasculaInterface.ViewModels.Base;
using Core.Application.DTOs;
using Core.Application.DTOs.ContpaqiComercial;
using Core.Application.Services;
using iText.Pdfua.Checkers.Utils.Ua2;
using Microsoft.IdentityModel.Tokens;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;

namespace BasculaInterface.ViewModels
{
    public class DetailedWeightViewModel : ViewModelBase
    {
        public bool IsSecondaryTerminal => Preferences.Get("SecondaryTerminal", false);
        public WeightEntryDto? WeightEntry { get; private set; } = null;
        public ClienteProveedorDto? Partner { get; set; } = null;
        public double TotalWeight => WeightEntry?.WeightDetails?.Sum(d => d.Weight) + WeightEntry?.TareWeight ?? 0;
        public ObservableCollection<WeightEntryDetailRow> WeightEntryDetailRows { get; private set; } = [];

        public ObservableCollection<ExternalTargetBehaviorDto> ExternalTargetBehaviors { get; set; } = [];
        private readonly IApiService _apiService = null!;

        public DetailedWeightViewModel(IApiService apiService)
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

        public async Task AddProductToWeightEntry(ProductoDto product, double qty = 0, int? costales = null)
        {
            if (WeightEntry == null)
            {
                throw new InvalidOperationException("WeightEntry must be set before adding products.");
            }
            if (product == null)
            {
                throw new ArgumentNullException(nameof(product), "Product cannot be null.");
            }
            WeightDetailDto newDetail = new WeightDetailDto
            {
                FK_WeightedProductId = product.Id,
                Tare = 0, // Default tare value, can be adjusted later
                Weight = 0, // Default weight value, can be adjusted later
                RequiredAmount = qty,
                Costales = costales
            };

            // Add the new detail to the WeightEntry before updating
            WeightEntry.WeightDetails.Add(newDetail);

            await UpdateWeightEntry();
        }

        public DetailedWeightViewModel() { }

        public async Task DeleteWeightEntry()
        {
            if (WeightEntry == null)
            {
                throw new InvalidOperationException("WeightEntry must be set before deletion.");
            }
            if (WeightEntry.Id <= 0)
            {
                throw new InvalidOperationException("WeightEntry.Id must be a valid positive integer.");
            }
            // Send delete request to the API
            await _apiService.DeleteAsync($"api/Weight?id={WeightEntry.Id}");
            WeightEntry = null;
            Partner = null;
            WeightEntryDetailRows.Clear();
            OnPropertyChanged(nameof(WeightEntry));
            OnPropertyChanged(nameof(Partner));
            OnCollectionChanged(nameof(WeightEntryDetailRows));
        }

        public async Task RemoveWeightEntryDetail(WeightEntryDetailRow selectedRow)
        {
            if (WeightEntry == null)
            {
                throw new InvalidOperationException("Cannot remove an item that doesn't exist");
            }
            if (selectedRow == null)
            {
                throw new ArgumentNullException(nameof(selectedRow), "Selected row cannot be null.");
            }
            WeightDetailDto? detailToRemove = WeightEntry.WeightDetails.FirstOrDefault(d => d.Id == selectedRow.Id);
            if (detailToRemove != null)
            {


                await DeleteWeightDetail(selectedRow.Id);

                WeightEntry.WeightDetails.Remove(detailToRemove);
                WeightEntryDetailRows.Remove(selectedRow);

                OnCollectionChanged(nameof(WeightEntryDetailRows));
                OnPropertyChanged(nameof(TotalWeight));
            }
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

            // Compare against UI rows count, not the DTO count (which may have been modified locally)
            int previousDetailCount = WeightEntryDetailRows.Count;

            // Fetch the latest weight entry details from the API
            WeightEntryDto? updatedEntry = await _apiService.GetAsync<WeightEntryDto>($"api/Weight/ById?id={WeightEntry.Id}", cancellationToken);
            if (updatedEntry == null)
            {
                throw new InvalidOperationException("Failed to fetch updated weight entry details.");
            }

            // Update the WeightEntry property
            WeightEntry = updatedEntry;

            // Reload products if the detail count changed (new products added/removed)
            int newDetailCount = WeightEntry.WeightDetails?.Count ?? 0;
            if (newDetailCount != previousDetailCount)
            {
                await LoadProductsAsync(WeightEntry, Partner, cancellationToken);
            }
            else
            {
                // Update existing rows with new weight/tare/weightedBy values without refetching products
                UpdateExistingDetailRows();
            }

            // Only fetch partner if PartnerId changed or Partner is not set
            if (WeightEntry.PartnerId.HasValue && WeightEntry.PartnerId.Value > 0)
            {
                // Skip API call if Partner already matches the current PartnerId
                if (Partner == null || Partner.Id != WeightEntry.PartnerId.Value)
                {
                    Partner = await _apiService.GetAsync<ClienteProveedorDto>($"api/ClienteProveedor/ById?id={WeightEntry.PartnerId.Value}", cancellationToken);
                    Partner.RazonSocial = Partner.Code.IsNullOrEmpty() ? Partner.RazonSocial : $"{Partner.Code} - {Partner.RazonSocial}";
                }
            }

            OnPropertyChanged(nameof(WeightEntry));
            OnPropertyChanged(nameof(Partner));
            OnPropertyChanged(nameof(TotalWeight));
        }

        /// <summary>
        /// Updates existing detail rows with new weight/tare values without refetching product data.
        /// </summary>
        private void UpdateExistingDetailRows()
        {
            if (WeightEntry?.WeightDetails == null) return;

            foreach (var detail in WeightEntry.WeightDetails)
            {
                var existingRow = WeightEntryDetailRows.FirstOrDefault(r => r.Id == detail.Id);
                if (existingRow != null)
                {
                    existingRow.Tare = detail.Tare;
                    existingRow.Weight = detail.Weight;
                    existingRow.SecondaryTare = detail.SecondaryTare;
                    existingRow.WeightedByDecorated = detail.WeightedBy;
                    existingRow.RequiredAmount = detail.RequiredAmount;
                    existingRow.Costales = detail.Costales;
                }
            }

            OnPropertyChanged(nameof(TotalWeight));
            OnCollectionChanged(nameof(WeightEntryDetailRows));
        }

        public async Task LoadExternalTargetBehaviors(CancellationToken cancellationToken = default)
        {
            // Skip API call if behaviors are already loaded (cache)
            if (ExternalTargetBehaviors.Count > 0)
            {
                return;
            }

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

        public async Task DeleteWeightDetail(int detailId)
        {
            if (WeightEntry == null)
            {
                throw new InvalidOperationException("WeightEntry must be set before deleting a detail.");
            }
            if (detailId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(detailId), "Detail ID must be a positive integer.");
            }
            // Send delete request to the API
            await _apiService.DeleteAsync($"api/Weight/Detail?id={detailId}");
        }

        public async Task ConcludeWeightProcess()
        {
            if (WeightEntry == null)
            {
                throw new InvalidOperationException("WeightEntry must be set before concluding the weight process.");
            }

            if (WeightEntry.WeightDetails.Count > 1 && (Partner is null || Partner.Id <= 0))
            {
                throw new InvalidOperationException("A partner must be selected before concluding the weight process with multiple products.");
            }

            WeightEntry.ConcludeDate = DateTime.UtcNow;

            //TODO: Validate tare + weights equal brute weight, maybe validate this from the API side

            // Send the updated weight entry to the API
            await _apiService.PutAsync<GenericResponse<string>>("api/Weight", WeightEntry);

            if (Partner is not null && !Partner.IsProvider)
            {
                try
                {
                    await SendToContpaqiComercial();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Error sending to contpaq: " + ex.Message);
                }
                //TODO: send 2 contpaq and then print with generated folio.
            }

            await Task.Delay(500); // Small delay to ensure the weight entry is updated before fetching new details

            await PrintTicketAsync();
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
            WeightEntry.ContpaqiComercialFolio = response.Data.ResultingFolio;

            OnPropertyChanged(nameof(WeightEntry));

            return response.Message ?? "Enviado a Contpaqi Comercial correctamente.";
        }

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

        public async Task PrintTurnAsync()
        {
            try
            {
                TurnDto turn = await _apiService.GetAsync<TurnDto>($"api/Turn?weightId={WeightEntry?.Id}");

                await _apiService.PostAsync<object>("api/Print/Text", turn.PrintData(Partner?.RazonSocial));
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error printing turn: " + ex.Message);
            }
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
                    RequiredAmount = detail.RequiredAmount,
                    Costales = detail.Costales
                };

                if (detail.FK_WeightedProductId is not null && productsById.TryGetValue(detail.FK_WeightedProductId.Value, out ProductoDto? product))
                {
                    if (product is null || product.Nombre.IsNullOrEmpty())
                    {
                        row.Description = $"Unknown Product ({detail.FK_WeightedProductId})";
                        row.IsGranel = false;
                    }
                    else
                    {
                        row.Description = $"{product.Code} - {product.Nombre}";
                        row.IsGranel = product!.IsGranel;
                    }
                }
                else if (detail.FK_WeightedProductId is not null)
                {
                    row.Description = $"Unknown Product ({detail.FK_WeightedProductId})";
                    row.IsGranel = false;
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

            // Notify UI that the collection reference changed
            OnPropertyChanged(nameof(WeightEntryDetailRows));
            OnCollectionChanged(nameof(WeightEntryDetailRows));
        }
    }
}
