using BasculaInterface.ViewModels.Base;
using Core.Application.DTOs;
using Core.Application.Services;
using System.Collections.ObjectModel;

namespace BasculaInterface.ViewModels
{
    public class ProviderPurchaseFormViewModel : ViewModelBase
    {
        private readonly IApiService _apiService;

        public ProviderPurchaseDto Purchase { get; set; } = new();
        public bool IsEditing => Purchase.Id > 0;

        public ObservableCollection<ClienteProveedorDto> Providers { get; set; } = [];
        public ObservableCollection<ProductoDto> Products { get; set; } = [];

        private ClienteProveedorDto? _selectedProvider;
        public ClienteProveedorDto? SelectedProvider
        {
            get => _selectedProvider;
            set
            {
                _selectedProvider = value;
                if (value != null)
                    Purchase.ProviderId = value.Id;
                OnPropertyChanged(nameof(SelectedProvider));
            }
        }

        private ProductoDto? _selectedProduct;
        public ProductoDto? SelectedProduct
        {
            get => _selectedProduct;
            set
            {
                _selectedProduct = value;
                if (value != null)
                    Purchase.ProductId = value.Id;
                OnPropertyChanged(nameof(SelectedProduct));
            }
        }

        public ProviderPurchaseFormViewModel(IApiService apiService)
        {
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
        }

        public void LoadExisting(ProviderPurchaseDto existing)
        {
            Purchase = existing;
            OnPropertyChanged(nameof(Purchase));
            OnPropertyChanged(nameof(IsEditing));
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

        public async Task<ProviderPurchaseDto> SaveAsync(CancellationToken cancellationToken = default)
        {
            if (IsEditing)
            {
                await _apiService.PutAsync<object>("api/ProviderPurchase", Purchase, cancellationToken);
                return Purchase;
            }
            else
            {
                return await _apiService.PostAsync<ProviderPurchaseDto>("api/ProviderPurchase", Purchase, cancellationToken);
            }
        }

        public async Task LoadProviderByIdAsync(int providerId, CancellationToken cancellationToken = default)
        {
            ClienteProveedorDto provider = await _apiService.GetAsync<ClienteProveedorDto>(
                $"api/ClienteProveedor/ById?id={providerId}", cancellationToken);

            Providers.Clear();
            Providers.Add(provider);
            SelectedProvider = provider;
        }

        public async Task LoadProductByIdAsync(int productId, CancellationToken cancellationToken = default)
        {
            ProductoDto product = await _apiService.GetAsync<ProductoDto>(
                $"api/Productos/ById?id={productId}", cancellationToken);

            product.Nombre = string.IsNullOrEmpty(product.Code) ? product.Nombre : $"{product.Code} - {product.Nombre}";
            Products.Clear();
            Products.Add(product);
            SelectedProduct = product;
        }

        public async Task<WeightEntryDto> CreateWeightEntryAsync(CancellationToken cancellationToken = default)
        {
            return await _apiService.PostAsync<WeightEntryDto>(
                $"api/ProviderPurchase/CreateWeightEntry?purchaseId={Purchase.Id}", new { }, cancellationToken);
        }
    }
}
