using BasculaInterface.Models;
using BasculaInterface.Services;
using BasculaInterface.ViewModels.Base;
using Core.Application.DTOs;
using Core.Application.DTOs.ContpaqiComercial;
using Core.Application.Security;
using Core.Application.Services;
using Core.Domain.Entities.Identity;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;

namespace BasculaInterface.ViewModels
{
    public class DetailedWeightViewModel : ViewModelBase
    {
        // role-driven-terminal-modes: resolved statically, same pattern as MainPage/WeightingScreen —
        // this class has a parameterless constructor too (design-time), so it isn't a plain DI param.
        private readonly ISessionService? _sessionService =
            MauiProgram.ServiceProvider.GetService(typeof(ISessionService)) as ISessionService;

        public bool IsSecondaryTerminal => (_sessionService?.CurrentUser?.TerminalMode ?? TerminalMode.Main) == TerminalMode.Secondary;
        public WeightEntryDto? WeightEntry { get; private set; } = null;
        public ClienteProveedorDto? Partner { get; set; } = null;
        public double TotalWeight
        {
            get
            {
                if (WeightEntry is null)
                    return 0;

                double loadedSum = WeightEntry.WeightDetails?.Where(d => d.IsLoaded).Sum(d => d.Weight) ?? 0;
                return WeightEntry.IsDischarge ? WeightEntry.TareWeight - loadedSum : WeightEntry.TareWeight + loadedSum;
            }
        }
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
                FK_WeightEntryId = WeightEntry.Id,
                FK_WeightedProductId = product.Id,
                RequiredAmount = qty,
                Costales = costales
            };

            WeightDetailDto created = await _apiService.PostAsync<WeightDetailDto>("api/Weight/Detail", newDetail);
            WeightEntry.WeightDetails.Add(created);
            await FetchNewWeightDetails();
        }

        public DetailedWeightViewModel() { }

        /// <summary>
        /// Deletes the whole WeightEntry via the self-authorize gate (issue #134, superseding the
        /// shared password from issue #133 / extend-delete-password-gate). The authorizer's
        /// identifier (UserCode or Username) and plaintext password travel to the server, which
        /// verifies the password against that user's salted hash and checks their
        /// CanSelfAuthorizeGate permission. Throws on an unresolved/unauthorized credential, an
        /// entry that already has a Contpaqi document, or a missing entry; the caller (View
        /// code-behind) is responsible for surfacing that to the user.
        /// </summary>
        public async Task DeleteWeightEntry(string gateIdentifier, string gatePassword)
        {
            if (WeightEntry == null)
            {
                throw new InvalidOperationException("WeightEntry must be set before deletion.");
            }
            if (WeightEntry.Id <= 0)
            {
                throw new InvalidOperationException("WeightEntry.Id must be a valid positive integer.");
            }

            await _apiService.PatchAsync<GenericResponse<string>>(
                $"api/Weight/{WeightEntry.Id}/Delete",
                new { GateIdentifier = gateIdentifier, GatePassword = gatePassword });

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

            // Reload products if the detail count changed (new products added/removed), or if an
            // existing detail's product identity changed (e.g. "Change Product", issue #122) —
            // UpdateExistingDetailRows only refreshes weight/tare/notes fields, never the product
            // name/price, so a same-count product swap needs the full reload too.
            int newDetailCount = WeightEntry.WeightDetails?.Count ?? 0;
            bool productIdentityChanged = WeightEntry.WeightDetails?.Any(detail =>
            {
                WeightEntryDetailRow? existingRow = WeightEntryDetailRows.FirstOrDefault(r => r.Id == detail.Id);
                return existingRow != null && existingRow.FK_WeightedProductId != detail.FK_WeightedProductId;
            }) ?? false;

            if (newDetailCount != previousDetailCount || productIdentityChanged)
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
                    Partner.RazonSocial = string.IsNullOrEmpty(Partner.Code) ? Partner.RazonSocial : $"{Partner.Code} - {Partner.RazonSocial}";
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
                    existingRow.IsLoaded = detail.IsLoaded;
                }
            }

            // Re-sort so incomplete (red) rows stay at the top
            var sorted = WeightEntryDetailRows.OrderBy(r => r.IsRowComplete).ThenBy(r => r.Id).ToList();
            for (int i = 0; i < sorted.Count; i++)
            {
                int currentIndex = WeightEntryDetailRows.IndexOf(sorted[i]);
                if (currentIndex != i)
                    WeightEntryDetailRows.Move(currentIndex, i);
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

        /// <summary>
        /// Attempts to retrieve an external target behavior by its ID from the API.
        /// If found, adds it to the local list and returns its index.
        /// If not found or an error occurs, returns -1.
        /// </summary>
        public async Task<int> ResolveExternalTargetBehaviorByIdAsync(int targetId, CancellationToken cancellationToken = default)
        {
            var behavior = await _apiService.GetAsync<ExternalTargetBehaviorDto>(
                $"api/ExternalTargetBehavior/ById?id={targetId}", cancellationToken);

            if (behavior is null || behavior.Id <= 0)
                return -1;

            ExternalTargetBehaviors.Add(behavior);
            OnCollectionChanged(nameof(ExternalTargetBehaviors));

            return ExternalTargetBehaviors.Count - 1;
        }

        public async Task ChangeTargetDocumentBehavior(ExternalTargetBehaviorDto newTargetBehavior, CancellationToken cancellationToken = default)
        {
            if (WeightEntry == null)
            {
                throw new InvalidOperationException("WeightEntry must be set before updating.");
            }

            await _apiService.PatchAsync($"api/Weight/{WeightEntry.Id}/ChangeTargetDocumentBehavior?newTargetId={newTargetBehavior.Id}", cancellationToken);

            await FetchNewWeightDetails();
        }

        /// <summary>
        /// Changes the product on an existing WeightDetail via the self-authorize gate (issue
        /// #134). Throws on an unresolved/unauthorized credential, insufficient credit, or a
        /// concurrency conflict; the caller (View code-behind) is responsible for surfacing that
        /// to the user.
        /// </summary>
        public async Task ChangeDetailProductAsync(int detailId, int newProductId, string gateIdentifier, string gatePassword)
        {
            await _apiService.PatchAsync<GenericResponse<string>>(
                $"api/Weight/Detail/{detailId}/Product",
                new { NewProductId = newProductId, GateIdentifier = gateIdentifier, GatePassword = gatePassword });

            await FetchNewWeightDetails();
        }

        /// <summary>
        /// Changes the partner on the current WeightEntry via the self-authorize gate (issue
        /// #134, superseding the shared password from issue #122's stopgap). Throws on an
        /// unresolved/unauthorized credential, an entry that already has a Contpaqi document,
        /// insufficient credit, or a concurrency conflict; the caller (View code-behind) is
        /// responsible for surfacing that to the user.
        /// </summary>
        public async Task ChangePartnerAsync(int newPartnerId, string gateIdentifier, string gatePassword)
        {
            if (WeightEntry == null)
            {
                throw new InvalidOperationException("WeightEntry must be set before changing its partner.");
            }

            await _apiService.PatchAsync<GenericResponse<string>>(
                $"api/Weight/{WeightEntry.Id}/Partner",
                new { NewPartnerId = newPartnerId, GateIdentifier = gateIdentifier, GatePassword = gatePassword });

            // Also refreshes Partner, since WeightEntry.PartnerId will now differ from the
            // previously cached Partner.Id (see FetchNewWeightDetails).
            await FetchNewWeightDetails();
        }

        /// <summary>
        /// Overrides the captured Weight (granel products) or RequiredAmount (non-granel
        /// products) on an existing WeightDetail via the self-authorize gate (issue #134,
        /// superseding the shared password from issue #122's stopgap). Throws on an
        /// unresolved/unauthorized credential, an ERP document already existing on the entry,
        /// insufficient credit, or a concurrency conflict; the caller (View code-behind) is
        /// responsible for surfacing that to the user.
        /// </summary>
        public async Task ChangeDetailAmountAsync(int detailId, bool isGranel, double newValue, string gateIdentifier, string gatePassword)
        {
            await _apiService.PatchAsync<GenericResponse<string>>(
                $"api/Weight/Detail/{detailId}/Amount",
                isGranel
                    ? new { NewWeight = newValue, NewRequiredAmount = (double?)null, GateIdentifier = gateIdentifier, GatePassword = gatePassword }
                    : new { NewWeight = (double?)null, NewRequiredAmount = newValue, GateIdentifier = gateIdentifier, GatePassword = gatePassword });

            // Also refreshes TotalWeight/BruteWeight, since a Weight override may have changed
            // the server-recomputed total.
            await FetchNewWeightDetails();
        }

        /// <summary>
        /// Soft-deletes an existing WeightDetail via the self-authorize gate (issue #134,
        /// superseding the shared password from issue #125's stopgap). Throws on an
        /// unresolved/unauthorized credential, an entry that already has a Contpaqi document, or a
        /// concurrency conflict; the caller (View code-behind) is responsible for surfacing that
        /// to the user. Distinct from the existing unguarded DeleteWeightDetail, which stays
        /// reserved for the empty-row "✕" button.
        /// </summary>
        public async Task DeleteWeightDetailSafelyAsync(int detailId, string gateIdentifier, string gatePassword)
        {
            await _apiService.PatchAsync<GenericResponse<string>>(
                $"api/Weight/Detail/{detailId}/Delete",
                new { GateIdentifier = gateIdentifier, GatePassword = gatePassword });

            // Also refreshes TotalWeight/BruteWeight, since deleting a loaded detail changes the
            // server-recomputed total.
            await FetchNewWeightDetails();
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

        public async Task SetWeightDetailLoaded(WeightEntryDetailRow row)
        {
            if (WeightEntry == null)
                throw new InvalidOperationException("WeightEntry must be set before updating a detail.");

            WeightDetailDto? detail = WeightEntry.WeightDetails.FirstOrDefault(d => d.Id == row.Id);
            if (detail == null)
                throw new InvalidOperationException("No weight detail found for the given row.");

            int detailId = row.Id;
            WeightEntryDto updatedEntry = await _apiService.PutWithRetryAsync<WeightEntryDto>(
                $"api/Weight/Detail/{detailId}/MarkLoaded",
                buildBody: () => Task.FromResult<object?>(null),
                refetch: async () =>
                {
                    WeightEntryDto fresh = await _apiService.GetAsync<WeightEntryDto>($"api/Weight/ById?id={WeightEntry.Id}");
                    WeightEntry = fresh;
                });

            WeightEntry = updatedEntry;
            row.IsLoaded = true;
            OnPropertyChanged(nameof(WeightEntry));
            OnPropertyChanged(nameof(TotalWeight));
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

            // Server validates all preconditions; call the dedicated conclude endpoint
            await _apiService.PutWithRetryAsync<GenericResponse<string>>(
                $"api/Weight/{WeightEntry.Id}/Conclude",
                buildBody: () => Task.FromResult<object?>(null),
                refetch: async () =>
                {
                    WeightEntryDto fresh = await _apiService.GetAsync<WeightEntryDto>($"api/Weight/ById?id={WeightEntry.Id}");
                    WeightEntry = fresh;
                });

            // Contpaqi is now triggered server-side by ConcludeAsync — nothing to do here.

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

        /// <summary>
        /// Validates if the partner has sufficient credit for the requested amount,
        /// considering all pending weight entries.
        /// </summary>
        /// <param name="requestedAmount">The amount to validate (e.g., qty * product.Precio)</param>
        /// <returns>CreditValidationResponse with validation results</returns>
        /// <exception cref="InvalidOperationException">Thrown when Partner is not set</exception>
        public async Task<CreditValidationResponse> ValidatePartnerCreditAsync(double requestedAmount)
        {
            if (Partner is null || Partner.Id <= 0)
            {
                throw new InvalidOperationException("No se ha seleccionado un socio para validar el crédito.");
            }

            CreditValidationResponse response = await _apiService.GetAsync<CreditValidationResponse>(
                $"api/Weight/ValidateCredit?partnerId={Partner.Id}&requestedAmount={requestedAmount}");

            return response;
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
                .Where(d => d.FK_WeightedProductId.HasValue && d.FK_WeightedProductId > 0)
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
                    Costales = detail.Costales,
                    IsLoaded = detail.IsLoaded
                };

                if (detail.FK_WeightedProductId > 0 && productsById.TryGetValue(detail.FK_WeightedProductId.Value, out ProductoDto? product))
                {
                    if (product is null || string.IsNullOrEmpty(product.Nombre))
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
                else
                {
                    row.Description = string.IsNullOrEmpty(detail.Notes) ? "Peso Libre" : detail.Notes;
                }

                WeightEntryDetailRows.Add(row);
            }

            //sort the rows: incomplete (red) first, then complete (green), within each group order by id
            WeightEntryDetailRows = new ObservableCollection<WeightEntryDetailRow>(
                WeightEntryDetailRows.OrderBy(row => row.IsRowComplete).ThenBy(row => row.Id).Select((row, index) =>
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
