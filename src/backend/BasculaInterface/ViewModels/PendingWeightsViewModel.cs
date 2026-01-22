using BasculaInterface.Exceptions;
using BasculaInterface.Models;
using BasculaInterface.ViewModels.Base;
using Core.Application.DTOs;
using Core.Application.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;

namespace BasculaInterface.ViewModels
{
    public class PendingWeightsViewModel : ViewModelBase
    {
        private List<WeightEntryDto> _pendingWeights { get; set; } = [];
        private List<ClienteProveedorDto> _clienteProveedorDtos { get; set; } = [];
        public ObservableCollection<PendingWeightViewRow> PendingWeightsCharge { get; } = [];
        public ObservableCollection<PendingWeightViewRow> PendingWeightsDischarge { get; } = [];

        private readonly IApiService _apiService = null!;

        public PendingWeightsViewModel(IApiService apiService) : this()
        {
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
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

        public ICommand RefreshCommand { get; }
        public PendingWeightsViewModel()
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

        public async Task LoadPendingWeightsAsync()
        {
            _pendingWeights.Clear();

            try
            {
                _pendingWeights = await _apiService.GetAsync<List<WeightEntryDto>>("api/Weight/Pending");

                await LoadClienteProveedorAsync();

                BuildObservableCollection();
            }
            finally
            {
                OnCollectionChanged(nameof(_pendingWeights));
                IsRefreshing = false;
            }
        }

        private void BuildObservableCollection()
        {
            PendingWeightsCharge.Clear();
            PendingWeightsDischarge.Clear();
            foreach (WeightEntryDto weight in _pendingWeights)
            {
                if(!(Preferences.Get("SecondaryTerminal", false) || Preferences.Get("OnlyPedidos", false)) && weight.TareWeight <= 0)
                {
                    //skip weights with no tare weight
                    continue;
                }

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
                    if(partner.IsProvider)
                    {
                        PendingWeightsDischarge.Add(new PendingWeightViewRow(weight, partner, teoricWeightText));
                    }
                    else
                    {
                        PendingWeightsCharge.Add(new PendingWeightViewRow(weight, partner, teoricWeightText));
                    }
                }
                else
                {
                    PendingWeightsCharge.Add(new PendingWeightViewRow(weight, new ClienteProveedorDto { RazonSocial = "No identificado" }, string.Empty));
                }
            }
        }

        private async Task LoadClienteProveedorAsync()
        {
            _clienteProveedorDtos.Clear();

            if (_pendingWeights.Count == 0)
            {
                OnCollectionChanged(nameof(_clienteProveedorDtos));
                return;
            }

            foreach (WeightEntryDto weight in _pendingWeights)
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

        private async Task PrintTurnAsync(WeightEntryDto newWeightEntry, ClienteProveedorDto partner)
        {
            try
            {
                TurnDto turn = await _apiService.GetAsync<TurnDto>($"api/Turn?weightId={newWeightEntry.Id}");

                await _apiService.PostAsync<object>("api/Print/Text", turn.PrintData(partner.RazonSocial));
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error printing turn: " + ex.Message);
            }
        }

        public async Task PostNewWeightEntry(WeightEntryDto weightEntry, ClienteProveedorDto partner)
        {
            if (_apiService == null)
            {
                throw new InvalidOperationException("ApiService is not initialized.");
            }
            if (weightEntry == null)
            {
                throw new InvalidOperationException("WeightEntry is not initialized.");
            }

            weightEntry.RegisteredBy = DeviceInfo.Name;

            WeightEntryDto newEntry = await _apiService.PostAsync<WeightEntryDto>("api/Weight", weightEntry);

            await PrintTurnAsync(newEntry, partner);
        }
    }
}
