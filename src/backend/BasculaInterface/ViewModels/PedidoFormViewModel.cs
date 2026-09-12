using BasculaInterface.Models;
using BasculaInterface.ViewModels.Base;
using Core.Application.DTOs;
using Core.Application.Security;
using Core.Application.Services;
using System.Collections.ObjectModel;

namespace BasculaInterface.ViewModels
{
    /// <summary>
    /// Replaces ProviderPurchaseFormViewModel — a Pedido header with N product lines,
    /// each independently convertible to weight (design.md Decisions 1-4). All amount/
    /// pending/almacén/dis-tare logic is computed backend-side; this ViewModel only
    /// orchestrates API calls and shapes DTOs for display, per the change's backend-only
    /// business-logic constraint.
    /// </summary>
    public class PedidoFormViewModel : ViewModelBase
    {
        private readonly IApiService _apiService;

        public PedidoDto Pedido { get; set; } = new();
        public bool IsEditing => Pedido.Id > 0;

        public ObservableCollection<ClienteProveedorDto> Providers { get; set; } = [];
        public ObservableCollection<ProductoDto> Products { get; set; } = [];

        /// <summary>
        /// Hidden <c>ExternalTargetBehavior</c> rows offered as almacén-target options in
        /// the convert-to-weight dialog (design.md Decision 5). The picked one becomes the
        /// new weight entry's <c>ExternalTargetBehaviorFK</c>.
        /// </summary>
        public ObservableCollection<ExternalTargetBehaviorDto> AlmacenTargets { get; set; } = [];
        public ObservableCollection<PedidoLineViewRow> LineRows { get; set; } = [];

        private ClienteProveedorDto? _selectedProvider;
        public ClienteProveedorDto? SelectedProvider
        {
            get => _selectedProvider;
            set
            {
                _selectedProvider = value;
                if (value != null)
                    Pedido.ProviderId = value.Id;
                OnPropertyChanged(nameof(SelectedProvider));
            }
        }

        // Pending "new line" selection, used by the add-line panel before it's posted.
        public ProductoDto? NewLineProduct { get; set; }

        public PedidoFormViewModel(IApiService apiService)
        {
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
        }

        public void LoadExisting(PedidoDto existing)
        {
            Pedido = existing;
            RebuildLineRows(new Dictionary<int, string>(), new Dictionary<int, string?>());
            OnPropertyChanged(nameof(Pedido));
            OnPropertyChanged(nameof(IsEditing));
        }

        private void RebuildLineRows(Dictionary<int, string> productNames, Dictionary<int, string?> productAlmacenCodes)
        {
            LineRows.Clear();
            foreach (var line in Pedido.Lines)
            {
                string name = productNames.TryGetValue(line.ProductId, out var n) ? n : "Producto";
                productAlmacenCodes.TryGetValue(line.ProductId, out var almacenCode);
                LineRows.Add(new PedidoLineViewRow(line, name, almacenCode));
            }
            OnPropertyChanged(nameof(LineRows));
        }

        /// <summary>Re-fetches the pedido and resolves product names for its lines — used after any line mutation.</summary>
        public async Task ReloadAsync(CancellationToken cancellationToken = default)
        {
            Pedido = await _apiService.GetAsync<PedidoDto>($"api/Pedido/{Pedido.Id}", cancellationToken);

            int[] productIds = [.. Pedido.Lines.Select(l => l.ProductId).Distinct()];
            Dictionary<int, string> productNames = new();
            Dictionary<int, string?> productAlmacenCodes = new();

            if (productIds.Length > 0)
            {
                string idsQuery = string.Join("&ids=", productIds);
                List<ProductoDto> products = await _apiService.GetAsync<List<ProductoDto>>(
                    $"api/Productos/ByMultipleIds?ids={idsQuery}", cancellationToken);

                foreach (var p in products)
                {
                    productNames[p.Id] = string.IsNullOrEmpty(p.Code) ? p.Nombre : $"{p.Code} - {p.Nombre}";
                    productAlmacenCodes[p.Id] = p.IdAlmacen;
                }
            }

            RebuildLineRows(productNames, productAlmacenCodes);
            OnPropertyChanged(nameof(Pedido));
        }

        public async Task SearchProvidersAsync(string searchTerm, CancellationToken cancellationToken = default)
        {
            List<ClienteProveedorDto> results = await _apiService.GetAsync<List<ClienteProveedorDto>>(
                $"api/ClienteProveedor/ByName?name={searchTerm}", cancellationToken);

            Providers.Clear();
            foreach (var p in results.Where(p => p.IsProvider))
                Providers.Add(p);

            OnPropertyChanged(nameof(Providers));
        }

        public async Task SearchProductsAsync(string searchTerm, CancellationToken cancellationToken = default)
        {
            List<ProductoDto> results = await _apiService.GetAsync<List<ProductoDto>>(
                $"api/Productos/ByName?name={searchTerm}", cancellationToken);

            Products.Clear();
            foreach (var p in results)
            {
                p.Nombre = string.IsNullOrEmpty(p.Code) ? p.Nombre : $"{p.Code} - {p.Nombre}";
                Products.Add(p);
            }

            OnPropertyChanged(nameof(Products));
        }

        public async Task LoadProviderByIdAsync(int providerId, CancellationToken cancellationToken = default)
        {
            ClienteProveedorDto provider = await _apiService.GetAsync<ClienteProveedorDto>(
                $"api/ClienteProveedor/ById?id={providerId}", cancellationToken);

            Providers.Clear();
            Providers.Add(provider);
            SelectedProvider = provider;
        }

        public async Task LoadAlmacenTargetsAsync(CancellationToken cancellationToken = default)
        {
            List<ExternalTargetBehaviorDto> results = await _apiService.GetAsync<List<ExternalTargetBehaviorDto>>(
                "api/ExternalTargetBehavior/AlmacenTargets", cancellationToken);
            AlmacenTargets.Clear();
            foreach (var t in results)
                AlmacenTargets.Add(t);
            OnPropertyChanged(nameof(AlmacenTargets));
        }

        /// <summary>Creates the Pedido header (with no lines yet — lines are added afterward via AddLineAsync).</summary>
        public async Task<PedidoDto> SaveHeaderAsync(CancellationToken cancellationToken = default)
        {
            if (IsEditing)
            {
                await _apiService.PutAsync<object>("api/Pedido", Pedido, cancellationToken);
                return Pedido;
            }

            Pedido = await _apiService.PostAsync<PedidoDto>("api/Pedido", Pedido, cancellationToken);
            return Pedido;
        }

        /// <summary>
        /// Deletes the whole Pedido via the password-gated endpoint (issue #133 /
        /// extend-delete-password-gate). Requires the manager password (plaintext here — hashed
        /// before it ever reaches the API, see PasswordHasher). Reuses the same shared password as
        /// every WeightEntry/WeightDetail guarded mutation.
        /// </summary>
        public async Task DeletePedidoAsync(string passwordPlaintext, CancellationToken cancellationToken = default)
        {
            string passwordHash = PasswordHasher.HashSha256Hex(passwordPlaintext);

            await _apiService.PatchAsync<GenericResponse<string>>(
                $"api/Pedido/{Pedido.Id}/Delete",
                new { PasswordHash = passwordHash },
                cancellationToken);
        }

        public async Task AddLineAsync(int productId, decimal requiredAmount, decimal? price, string? notes, bool requiresDisTaring, CancellationToken cancellationToken = default)
        {
            PedidoLineDto dto = new()
            {
                PedidoId = Pedido.Id,
                ProductId = productId,
                RequiredAmount = requiredAmount,
                Price = price,
                Notes = notes,
                RequiresDisTaring = requiresDisTaring
            };

            await _apiService.PostAsync<PedidoLineDto>("api/Pedido/Line", dto, cancellationToken);
            await ReloadAsync(cancellationToken);
        }

        public async Task CloseLineAsync(int lineId, CancellationToken cancellationToken = default)
        {
            await _apiService.PatchAsync($"api/Pedido/Line/{lineId}/Close", cancellationToken);
            await ReloadAsync(cancellationToken);
        }

        public async Task<WeightEntryDto> ConvertLineToWeightAsync(int lineId, decimal targetAmount, int almacenTargetId, int? weightEntryId = null, CancellationToken cancellationToken = default)
        {
            var body = new
            {
                WeightEntryId = weightEntryId,
                TargetAmount = targetAmount,
                ExternalTarget = almacenTargetId.ToString()
            };

            WeightEntryDto created = await _apiService.PostAsync<WeightEntryDto>(
                $"api/Pedido/Line/{lineId}/ConvertToWeight", body, cancellationToken);

            await ReloadAsync(cancellationToken);
            return created;
        }
    }
}
