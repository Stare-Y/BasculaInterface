using System.Text;
using System.Text.Json;
using BasculaInterface.ViewModels.Base;
using Core.Application.DTOs;
using Core.Application.Services;
using Microsoft.AspNetCore.SignalR.Client;

namespace BasculaInterface.ViewModels
{
    public class BasculaViewModel : ViewModelBase
    {
        private WeightEntryDto?_weightEntry;
        public WeightEntryDto? WeightEntry 
        {
            get => _weightEntry;
            set
            {
                _weightEntry = value;
                OnPropertyChanged(nameof(WeightEntry));
            }
        }
        private ClienteProveedorDto? _partner;
        public ClienteProveedorDto? Partner
        {
            get => _partner;
            set
            {
                _partner = value;
                OnPropertyChanged(nameof(Partner));
            }
        }

        private ProductoDto? _product;
        public ProductoDto? Product 
        {
            get => _product;
            set 
            {
                _product = value;
                OnPropertyChanged(nameof(Product));
            }
        }

        private double _pesoTotal = 0;
        public string Peso
        {
            get => _pesoTotal.ToString("F2");
        }

        private double _tara = 0;
        public string Tara
        {
            get => _tara.ToString("F2");
        }


        private double _diferenciaAbs = 0;
        public string Diferencia
        {
            get => _diferenciaAbs.ToString("F2");
        }

        public string Estado { get; private set; } = "Desconectado";

        private HubConnection? _basculaSocketHub;

        event Action<double>? OnWeightReceived;
        private readonly IApiService? _apiService;

        public BasculaViewModel(IApiService apiService)
        {
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
        }

        public BasculaViewModel() { }

        public async Task ConnectSocket()
        {
            try
            {
                _basculaSocketHub = new HubConnectionBuilder()
                .WithUrl(_apiService?.GetBaseUrl() + "basculaSocket")
                .Build();

                _basculaSocketHub.On<double>("ReceiveNumber", data =>
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        OnWeightReceived?.Invoke(data);
                    });
                });

                OnWeightReceived += UpdateWeight;

                await _basculaSocketHub.StartAsync();
                Estado = "Conectado";
            }
            catch (Exception ex)
            {
                Estado = $"Error conectando socket: {_apiService?.GetBaseUrl()}: " + ex.Message;
            }
            finally
            {
                OnPropertyChanged(nameof(Estado));
            }
        }

        private void UpdateWeight(double data)
        {
            if (_tara != 0)
            {
                _diferenciaAbs = Math.Abs(_tara - data);
            }

            _pesoTotal = data;

            OnPropertyChanged(nameof(Peso));
            OnPropertyChanged(nameof(Tara));
            OnPropertyChanged(nameof(Diferencia));
        }

        public void SetTara(double tara)
        {
            if (tara < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(tara), "Tara cannot be negative.");
            }
            _tara = tara;
            OnPropertyChanged(nameof(Tara));
        }
        public async Task PrintTicketAsync(string text)
        {
            try
            {
                string jsonString = JsonSerializer.Serialize(text);
                StringContent content = new StringContent(jsonString, Encoding.UTF8, "application/json");
                if (_apiService is not null)
                {
                    await _apiService.PostAsync<object>("api/print", content).ConfigureAwait(false);

                    Estado = "Impresion enviada";
                }
                else
                {
                    Estado = "Error: porfavor, presiona Reconectar";
                }
            }
            catch (Exception ex)
            {
                Estado = "Error: " + ex.Message;
            }
            finally
            {
                OnPropertyChanged(nameof(Estado));
            }
        }

        public async Task ReleaseSocket()
        {
            try
            {
                if (_basculaSocketHub != null)
                {
                    await _basculaSocketHub.StopAsync();
                    await _basculaSocketHub.DisposeAsync();
                    _basculaSocketHub = null;
                }

                Estado = "Desconectado";
            }
            catch (Exception ex)
            {
                Estado = "Error: " + ex.Message;
            }
            finally
            {
                OnPropertyChanged(nameof(Estado));
            }
        }

        public async Task CaptureNewWeightEntry()
        {
            if(_pesoTotal == 0)
            {
                throw new InvalidOperationException("Peso total no puede ser cero.");
            }
            if (WeightEntry == null)
            {
                throw new InvalidOperationException("WeightEntry is not initialized.");
            }
            if (_tara == 0 && WeightEntry.TareWeight != 0)
            {
                throw new InvalidOperationException("Tare weight must be set equal to the received before capturing a new weight entry.");
            }
            if (_tara == 0)
            {
                WeightEntry.TareWeight = _pesoTotal;
                WeightEntry.BruteWeight = _pesoTotal;
                WeightEntry.PartnerId = Partner?.Id;

                await PostNewWeightEntry();
            }
            else
            {
                if (WeightEntry.Id == 0)
                {
                    throw new InvalidOperationException("WeightEntry must have a valid Id before capturing a new weight entry.");
                }
                WeightEntry.PartnerId = Partner?.Id;
                WeightEntry.BruteWeight = _pesoTotal;
                WeightEntry.WeightDetails.Add(new WeightDetailDto
                {
                    FK_WeightEntryId = WeightEntry.Id,
                    Tare = _tara,
                    Weight = _diferenciaAbs,
                    FK_WeightedProductId = Product?.Id,
                });

                await PutWeightEntry();
            }
        }

        private async Task PostNewWeightEntry()
        {
            if (_apiService == null)
            {
                throw new InvalidOperationException("ApiService is not initialized.");
            }
            if (WeightEntry == null)
            {
                throw new InvalidOperationException("WeightEntry is not initialized.");
            }

            await _apiService.PostAsync<WeightEntryDto>("api/Weight", WeightEntry);
        }

        private async Task PutWeightEntry()
        {
            if (_apiService == null)
            {
                throw new InvalidOperationException("ApiService is not initialized.");
            }
            if (WeightEntry == null)
            {
                throw new InvalidOperationException("WeightEntry is not initialized.");
            }
            await _apiService.PutAsync<object>("api/Weight/", WeightEntry);
        }
    }
}
