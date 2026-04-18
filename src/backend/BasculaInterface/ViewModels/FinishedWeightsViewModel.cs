using BasculaInterface.Exceptions;
using BasculaInterface.Models;
using BasculaInterface.ViewModels.Base;
using Core.Application.DTOs;
using Core.Application.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace BasculaInterface.ViewModels
{
    public class FinishedWeightsViewModel : ViewModelBase
    {
        private List<WeightEntryDto> _finishedWeights { get; set; } = [];
        private Dictionary<int, ClienteProveedorDto> _partnerMap { get; set; } = [];
        private readonly IApiService _apiService = null!;
        private const int PageSize = 30;

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

        private bool isRefreshing;

        public FinishedWeightsViewModel(IApiService apiService) : this()
        {
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
        }
        public FinishedWeightsViewModel()
        {
            RefreshCommand = new Command(async () =>
            {
                IsRefreshing = true;
                try
                {
                    await LoadPendingWeightsAsync();

                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("NotFound"))
                    {
                        throw new OriginEmptyException("No hay pesos pendientes, puedes crear uno nuevo :D");
                    }
                    else
                    {
                        throw new Exception(ex.Message);
                    }
                }
            });
        }
        public ICommand RefreshCommand { get; }

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
        public ObservableCollection<PendingWeightViewRow> FinishedWeights { get; set; } = [];

        private async Task LoadClienteProveedorAsync(CancellationToken cancellationToken = default)
        {
            _partnerMap.Clear();

            if (_finishedWeights.Count == 0)
                return;

            int[] partnerIds = _finishedWeights
                .Where(w => w.PartnerId.HasValue && w.PartnerId.Value > 0)
                .Select(w => w.PartnerId!.Value)
                .Distinct()
                .ToArray();

            if (partnerIds.Length == 0)
                return;

            string idsQuery = string.Join("&ids=", partnerIds);
            List<ClienteProveedorDto> partners = await _apiService.GetAsync<List<ClienteProveedorDto>>(
                $"api/ClienteProveedor/ByMultipleIds?ids={idsQuery}", cancellationToken);

            foreach (ClienteProveedorDto partner in partners)
                _partnerMap[partner.Id] = partner;
        }

        public async Task LoadPendingWeightsAsync(CancellationToken cancellationToken = default)
        {
            _finishedWeights.Clear();

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                _finishedWeights = await _apiService.GetAsync<List<WeightEntryDto>>(
                    $"api/Weight/All/Completed?top={PageSize}&page={CurrentPage}", cancellationToken);

                CanGoForward = _finishedWeights.Count >= PageSize;
                OnPropertyChanged(nameof(CanGoForward));
                OnPropertyChanged(nameof(CanGoBack));

                cancellationToken.ThrowIfCancellationRequested();

                await LoadClienteProveedorAsync(cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();

                BuildObservableCollection();
            }
            finally
            {
                OnCollectionChanged(nameof(_finishedWeights));
                IsRefreshing = false;
            }
        }

        private void BuildObservableCollection()
        {
            var rows = new List<PendingWeightViewRow>();

            foreach (WeightEntryDto weight in _finishedWeights)
            {
                _partnerMap.TryGetValue(weight.PartnerId ?? 0, out ClienteProveedorDto? partner);

                string teoricWeightText = string.Empty;

                if (partner != null && weight.WeightDetails.Count > 0)
                {
                    teoricWeightText =
                        "Total (teorico): "
                        + (weight.WeightDetails.Sum(d => d.Weight)
                        + weight.TareWeight).ToString()
                        + " kg.";
                }

                rows.Add(new PendingWeightViewRow(
                    weight,
                    partner ?? new ClienteProveedorDto { RazonSocial = "No identificado" },
                    teoricWeightText));
            }

            FinishedWeights = new ObservableCollection<PendingWeightViewRow>(rows);
            OnPropertyChanged(nameof(FinishedWeights));
        }

        public async Task GoToNextPageAsync(CancellationToken cancellationToken = default)
        {
            if (!CanGoForward) return;
            CurrentPage++;
            await LoadPendingWeightsAsync(cancellationToken);
        }

        public async Task GoToPreviousPageAsync(CancellationToken cancellationToken = default)
        {
            if (!CanGoBack) return;
            CurrentPage--;
            await LoadPendingWeightsAsync(cancellationToken);
        }
    }
}
