﻿using System.Diagnostics;
using System.Text;
using System.Text.Json;
using BasculaInterface.ViewModels.Base;
using Core.Application.DTOs;
using Core.Application.Services;
using Microsoft.AspNetCore.SignalR.Client;

namespace BasculaInterface.ViewModels
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
    public class BasculaViewModel : ViewModelBase
    {
        private WeightEntryDto? _weightEntry;
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
            set => fakePesoWrite = value;
        }
        private string? fakePesoWrite = null;

        private double _tara = 0;
        public string Tara
        {
            get => _tara.ToString("F2");
        }

        public double TaraCurrentValue
        {
            get => _tara;
        }


        private double _diferenciaAbs = 0;
        public string Diferencia
        {
            get => _diferenciaAbs.ToString("F2");
        }

        public double ProductQuantity { get; set; } = 0;

        public string Estado { get; private set; } = "Desconectado";

        private HubConnection? _basculaSocketHub;

        event Action<double>? OnWeightReceived;
        private readonly IApiService _apiService = null!;

        public BasculaViewModel(IApiService apiService)
        {
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
        }

        public async Task<bool> CanWeight()
        {
            try
            {
                return await _apiService.PutAsync<bool>($"api/Weight/CanWeight?deviceId={Preferences.Get("DeviceName", DeviceInfo.Name)}", null);
            }
            catch
            {
                return false;
            }
        }

        public async Task ReleaseWeight()
        {
            try
            {
                await _apiService.PutAsync<bool>($"api/Weight/ReleaseWeight?deviceId={Preferences.Get("DeviceName", DeviceInfo.Name)}", null);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error releasing weight: {ex.Message}");
            }
        }

        public BasculaViewModel() { }

        public async Task ConnectSocket()
        {
            try
            {
                _basculaSocketHub = new HubConnectionBuilder()
                .WithUrl(_apiService?.GetBaseUrl() + "basculaSocket")
                .Build();

                OnWeightReceived += UpdateWeight;

                _basculaSocketHub.On<double>("ReceiveLecture", lecture =>
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        OnWeightReceived?.Invoke(lecture);
                    });
                });

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

        public void UpdateWeight(double lecture)
        {
            UpdateWeight(lecture, true);
        }

        public void UpdateWeight(double lecture, bool notifyWeightChange = true)
        {
            if (_tara != 0)
            {
                _diferenciaAbs = Math.Abs(_tara - lecture);
            }

            _pesoTotal = lecture;

            if (notifyWeightChange)
            {
                OnPropertyChanged(nameof(Peso));
            }
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

            _diferenciaAbs = Math.Abs(_pesoTotal - _tara);

            OnPropertyChanged(nameof(Tara));
            OnPropertyChanged(nameof(Diferencia));
        }

        public void SetTaraFromPesoTotal()
        {
            if (_pesoTotal < 0)
            {
                throw new InvalidOperationException("Peso total cannot be negative.");
            }
            _tara = _pesoTotal;

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
            ValidateBeforePosting();

            if (_tara == 0)
            {
                WeightEntry!.TareWeight = _pesoTotal;
                WeightEntry.BruteWeight = _pesoTotal;
                WeightEntry.PartnerId = Partner?.Id;

                await PostNewWeightEntry();

                return;
            }

            if (WeightEntry!.Id == 0)
            {
                throw new InvalidOperationException("WeightEntry must have a valid Id before capturing a new weight entry.");
            }

            WeightEntry.PartnerId = Partner?.Id;

            if (WeightEntry.WeightDetails.Any(w => w.FK_WeightedProductId == Product?.Id))
            {
                // If the product already exists, update the weight
                WeightDetailDto existingDetail = WeightEntry.WeightDetails.First(w => w.FK_WeightedProductId == Product?.Id);
                existingDetail.Weight = _diferenciaAbs;
                existingDetail.Tare = WeightEntry.BruteWeight;
                existingDetail.WeightedBy = DeviceInfo.Name;

                WeightEntry.BruteWeight += _diferenciaAbs;

                await PutWeightEntry();

                return;
            }
            
            // If the product does not exist, add a new detail
            WeightEntry.WeightDetails.Add(new WeightDetailDto
            {
                FK_WeightEntryId = WeightEntry.Id,
                Tare = WeightEntry.BruteWeight,
                Weight = _diferenciaAbs,
                FK_WeightedProductId = Product?.Id,
                RequiredAmount = ProductQuantity,
                WeightedBy = DeviceInfo.Name
            });

            WeightEntry.BruteWeight = _pesoTotal;

            await PutWeightEntry();
        }

        public async Task PutSecondaryTara()
        {
            if (TaraCurrentValue < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(TaraCurrentValue), "Secondary tara cannot be negative or 0.");
            }
            if (WeightEntry == null)
            {
                throw new InvalidOperationException("WeightEntry is not initialized.");
            }
            WeightDetailDto? detail = WeightEntry.WeightDetails.FirstOrDefault(w => w.FK_WeightedProductId == Product?.Id);
            if (detail != null)
            {
                detail.SecondaryTare = TaraCurrentValue;
                detail.WeightedBy = DeviceInfo.Name;
                await PutWeightEntry();
            }
            else
            {
                throw new InvalidOperationException("No weight detail found for the current product.");
            }
        }

        private void ValidateBeforePosting()
        {
            if (Preferences.Get("RequirePartner", false))
            {
                if(Partner is null || Partner.Id < 1)
                    throw new InvalidOperationException("Es obligatorio especificar al socio.");
            }
            if (_pesoTotal == 0)
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
            if (string.IsNullOrEmpty(WeightEntry.VehiclePlate))
            {
                throw new InvalidOperationException("Es obligatorio especificar la placa del vehiculo.");
            }
        }

        private async Task PrintTurnAsync(WeightEntryDto newWeightEntry)
        {
            try
            {
                TurnDto turn = await _apiService.GetAsync<TurnDto>($"api/Turn?weightId={newWeightEntry.Id}");

                await _apiService.PostAsync<object>("api/Print", turn.PrintData(Partner?.RazonSocial));
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error printing turn: " + ex.Message);
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

            WeightEntry.RegisteredBy = DeviceInfo.Name;

            WeightEntryDto newEntry = await _apiService.PostAsync<WeightEntryDto>("api/Weight", WeightEntry);

            await PrintTurnAsync(newEntry);
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

            WeightEntry.RegisteredBy = DeviceInfo.Name;

            await _apiService.PutAsync<object>("api/Weight", WeightEntry);
        }
    }
}
