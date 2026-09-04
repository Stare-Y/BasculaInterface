using BasculaInterface.Models;
using BasculaInterface.ViewModels.Base;
using Core.Application.DTOs;
using Core.Application.Services;
using System.Collections.ObjectModel;

namespace BasculaInterface.ViewModels
{
    /// <summary>Replaces ProviderPurchaseListViewModel — lists Pedido headers instead of flat purchase rows.</summary>
    public class PedidoListViewModel : ViewModelBase
    {
        private readonly IApiService _apiService;
        private const int PageSize = 30;

        public ObservableCollection<PedidoViewRow> Pedidos { get; set; } = [];

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

        public PedidoListViewModel(IApiService apiService)
        {
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
        }

        public async Task LoadPedidosAsync(CancellationToken cancellationToken = default)
        {
            List<PedidoDto> pedidos = await _apiService.GetAsync<List<PedidoDto>>(
                $"api/Pedido/All?top={PageSize}&page={CurrentPage}", cancellationToken);

            CanGoForward = pedidos.Count >= PageSize;
            OnPropertyChanged(nameof(CanGoForward));
            OnPropertyChanged(nameof(CanGoBack));

            int[] providerIds = pedidos
                .Select(p => p.ProviderId)
                .Distinct()
                .ToArray();

            Dictionary<int, string> providerNames = new();

            if (providerIds.Length > 0)
            {
                string idsQuery = string.Join("&ids=", providerIds);
                List<ClienteProveedorDto> partners = await _apiService.GetAsync<List<ClienteProveedorDto>>(
                    $"api/ClienteProveedor/ByMultipleIds?ids={idsQuery}", cancellationToken);

                foreach (var p in partners)
                    providerNames[p.Id] = p.RazonSocial;
            }

            Pedidos.Clear();

            foreach (var pedido in pedidos
                .OrderBy(p => p.Concluded)
                .ThenBy(p => p.ExpectedArrival))
            {
                string providerName = providerNames.TryGetValue(pedido.ProviderId, out var pn) ? pn : "Desconocido";
                Pedidos.Add(new PedidoViewRow(pedido, providerName));
            }

            OnPropertyChanged(nameof(Pedidos));
        }

        public async Task GoToNextPageAsync(CancellationToken cancellationToken = default)
        {
            if (!CanGoForward) return;
            CurrentPage++;
            await LoadPedidosAsync(cancellationToken);
        }

        public async Task GoToPreviousPageAsync(CancellationToken cancellationToken = default)
        {
            if (!CanGoBack) return;
            CurrentPage--;
            await LoadPedidosAsync(cancellationToken);
        }

        public async Task DeletePedidoAsync(int id, CancellationToken cancellationToken = default)
        {
            await _apiService.DeleteAsync($"api/Pedido?id={id}", cancellationToken);
        }
    }
}
