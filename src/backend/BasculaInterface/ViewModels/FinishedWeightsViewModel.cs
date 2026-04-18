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
        public ObservableCollection<PendingWeightViewRow> FinishedWeights { get; } = [];

        private async Task LoadClienteProveedorAsync()
        {
            _clienteProveedorDtos.Clear();

            if (_finishedWeights.Count == 0)
            {
                OnCollectionChanged(nameof(_clienteProveedorDtos));
                return;
            }

            foreach (WeightEntryDto weight in _finishedWeights)
            {
                //get the partner id
                if (weight.PartnerId.HasValue && weight.PartnerId.Value > 0)
                {
                    ClienteProveedorDto? partner = await _apiService.GetAsync<ClienteProveedorDto>($"api/ClienteProveedor/ById?id={weight.PartnerId.Value}");
                    if (partner != null)
                    {
                        _clienteProveedorDtos.Add(partner);
                    }
                }
            }

            OnCollectionChanged(nameof(_clienteProveedorDtos));
        }

        public async Task LoadPendingWeightsAsync()
        {
            _finishedWeights.Clear();

            try
            {
                _finishedWeights = await _apiService.GetAsync<List<WeightEntryDto>>($"api/Weight/All/Completed?top={PageSize}&page={CurrentPage}");

                CanGoForward = _finishedWeights.Count >= PageSize;
                OnPropertyChanged(nameof(CanGoForward));
                OnPropertyChanged(nameof(CanGoBack));

                await LoadClienteProveedorAsync();

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
            FinishedWeights.Clear();
            foreach (WeightEntryDto weight in _finishedWeights)
            {
                ClienteProveedorDto? partner = _clienteProveedorDtos.FirstOrDefault(p => p.Id == weight.PartnerId);
                if (partner != null)
                {
                    string teoricWeightText = string.Empty;

                    if (weight.WeightDetails.Count > 0)
                    {
                        teoricWeightText +=
                            "Total (teorico): "
                            + (weight.WeightDetails.Sum(d => d.Weight)
                            + weight.TareWeight).ToString()
                            + " kg.";
                    }

                    FinishedWeights.Add(new PendingWeightViewRow(weight, partner, teoricWeightText));
                }
                else
                {
                    FinishedWeights.Add(new PendingWeightViewRow(weight, new ClienteProveedorDto { RazonSocial = "No identificado" }, string.Empty));
                }
            }
        }

        public async Task GoToNextPageAsync()
        {
            if (!CanGoForward) return;
            CurrentPage++;
            await LoadPendingWeightsAsync();
        }

        public async Task GoToPreviousPageAsync()
        {
            if (!CanGoBack) return;
            CurrentPage--;
            await LoadPendingWeightsAsync();
        }
    }
}
