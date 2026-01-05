using BasculaInterface.Models;
using BasculaInterface.ViewModels.Base;
using Core.Application.DTOs;
using Core.Application.Services;
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
        public ObservableCollection<WeightEntryDetailRow> WeightEntryDetailRows { get; private set; } = new ObservableCollection<WeightEntryDetailRow>();

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

        public async Task AddProductToWeightEntry(ProductoDto product, double qty = 0)
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
                RequiredAmount = qty
            };
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

        public async Task UpdateWeightEntry()
        {
            if (WeightEntry == null)
            {
                throw new InvalidOperationException("WeightEntry must be set before updating.");
            }
            // Validate the weight entry before updating
            if (WeightEntry.WeightDetails == null || !WeightEntry.WeightDetails.Any())
            {
                throw new InvalidOperationException("WeightEntry must have at least one weight detail.");
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

            WeightEntry.ConcludeDate = DateTime.Now;

            //TODO: Validate tare + weights equal brute weight, maybe validate this from the API side

            // Send the updated weight entry to the API
            await _apiService.PutAsync<object>("api/Weight", WeightEntry);
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
    }
}
