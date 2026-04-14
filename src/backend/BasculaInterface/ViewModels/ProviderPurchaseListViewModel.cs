using BasculaInterface.Models;
using BasculaInterface.ViewModels.Base;
using Core.Application.DTOs;
using Core.Application.Services;
using System.Collections.ObjectModel;

namespace BasculaInterface.ViewModels
{
    public class ProviderPurchaseListViewModel : ViewModelBase
    {
        private readonly IApiService _apiService;
        private const int PageSize = 30;

        public ObservableCollection<ProviderPurchaseViewRow> Purchases { get; set; } = [];

        private uint _currentPage = 1;
        public uint CurrentPage
        {
            get => _currentPage;
            private set
            {
                _currentPage = value;
                OnPropertyChanged(nameof(CurrentPage));
                OnPropertyChanged(nameof(PageText));
            }
        }

        public string PageText => $"Página {CurrentPage}";
        public bool CanGoBack => CurrentPage > 1;
        public bool CanGoForward { get; private set; }

        public ProviderPurchaseListViewModel(IApiService apiService)
        {
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
        }

        public async Task LoadPurchasesAsync(CancellationToken cancellationToken = default)
        {
            List<ProviderPurchaseDto> purchases = await _apiService.GetAsync<List<ProviderPurchaseDto>>(
                $"api/ProviderPurchase/All?top={PageSize}&page={CurrentPage}", cancellationToken);

            CanGoForward = purchases.Count >= PageSize;
            OnPropertyChanged(nameof(CanGoForward));
            OnPropertyChanged(nameof(CanGoBack));

            int[] providerIds = purchases
                .Select(p => p.ProviderId)
                .Distinct()
                .ToArray();

            int[] productIds = purchases
                .Select(p => p.ProductId)
                .Distinct()
                .ToArray();

            Dictionary<int, string> providerNames = new();
            Dictionary<int, string> productNames = new();

            if (providerIds.Length > 0)
            {
                string idsQuery = string.Join("&ids=", providerIds);
                List<ClienteProveedorDto> partners = await _apiService.GetAsync<List<ClienteProveedorDto>>(
                    $"api/ClienteProveedor/ByMultipleIds?ids={idsQuery}", cancellationToken);

                foreach (var p in partners)
                    providerNames[p.Id] = p.RazonSocial;
            }

            if (productIds.Length > 0)
            {
                string idsQuery = string.Join("&ids=", productIds);
                List<ProductoDto> products = await _apiService.GetAsync<List<ProductoDto>>(
                    $"api/Productos/ByMultipleIds?ids={idsQuery}", cancellationToken);

                foreach (var p in products)
                    productNames[p.Id] = string.IsNullOrEmpty(p.Code) ? p.Nombre : $"{p.Code} - {p.Nombre}";
            }

            Purchases.Clear();

            foreach (var purchase in purchases
                .OrderBy(p => p.Concluded)
                .ThenBy(p => p.CreatedAt))
            {
                string providerName = providerNames.TryGetValue(purchase.ProviderId, out var pn) ? pn : "Desconocido";
                string productName = productNames.TryGetValue(purchase.ProductId, out var prn) ? prn : "Desconocido";

                Purchases.Add(new ProviderPurchaseViewRow(purchase, providerName, productName));
            }

            OnPropertyChanged(nameof(Purchases));
        }

        public async Task GoToNextPageAsync(CancellationToken cancellationToken = default)
        {
            if (!CanGoForward) return;
            CurrentPage++;
            await LoadPurchasesAsync(cancellationToken);
        }

        public async Task GoToPreviousPageAsync(CancellationToken cancellationToken = default)
        {
            if (!CanGoBack) return;
            CurrentPage--;
            await LoadPurchasesAsync(cancellationToken);
        }

        public async Task DeletePurchaseAsync(int id, CancellationToken cancellationToken = default)
        {
            await _apiService.DeleteAsync($"api/ProviderPurchase?id={id}", cancellationToken);
        }
    }
}
