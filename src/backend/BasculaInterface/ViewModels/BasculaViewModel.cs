using System.Diagnostics;
using System.Text;
using System.Text.Json;
using BasculaInterface.ViewModels.Base;
using Core.Application.DTOs;
using Core.Application.Services;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.IdentityModel.Tokens;

namespace BasculaInterface.ViewModels
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
    public class BasculaViewModel : ViewModelBase
    {
        public string? DetailNotes { get; set; } = null;
        public int? TargetWeightDetail { get; set; } = null;
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
            if (ManualWeighting)
                return;

            UpdateWeight(lecture, true);
        }

        public bool ManualWeighting { get; set; } = false;

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
                    await _apiService.PostAsync<object>("api/Print/Text", content).ConfigureAwait(false);

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
        public bool Providers { get; set; } = false;
        public async Task CaptureNewWeightEntry(bool printTurn = false)
        {
            ValidateBeforePosting();
            await ValidateCreditForCaptureAsync();

            if (_tara == 0)
            {
                // Initial truck tare: set TareWeight only; BruteWeight is server-owned
                WeightEntry!.TareWeight = _pesoTotal;
                WeightEntry.PartnerId = Partner?.Id;

                await PostNewWeightEntry(printTurn);

                return;
            }

            if (WeightEntry!.Id == 0)
            {
                throw new InvalidOperationException("WeightEntry must have a valid Id before capturing a new weight entry.");
            }

            WeightEntry.PartnerId = Partner?.Id;

            if (TargetWeightDetail.HasValue && WeightEntry.WeightDetails.Any(w => w.Id == TargetWeightDetail.Value))
            {
                // Secondary terminal: record measured weight via narrow endpoint
                WeightDetailDto existingDetail = WeightEntry.WeightDetails.First(w => w.Id == TargetWeightDetail.Value);

                if (_diferenciaAbs <= 0)
                {
                    throw new InvalidOperationException($"La diferencia de la tara es 0 \n(Tara inicial: {existingDetail.SecondaryTare} peso final: {_pesoTotal}) \nDESTARA Y CAPTURA DE NUEVO");
                }

                double measuredWeight = _diferenciaAbs;
                string deviceName = DeviceInfo.Name;

                await _apiService.PutWithRetryAsync<object>(
                    $"api/Weight/Detail/{existingDetail.Id}/Weight",
                    buildBody: () => Task.FromResult<object?>(new { Weight = measuredWeight, WeightedBy = deviceName }),
                    refetch: async () =>
                    {
                        WeightEntryDto fresh = await _apiService.GetAsync<WeightEntryDto>($"api/Weight/ById?id={WeightEntry.Id}");
                        WeightEntry = fresh;
                    });

                // Refresh local state from server so BruteWeight reflects server-computed value
                WeightEntryDto updated = await _apiService.GetAsync<WeightEntryDto>($"api/Weight/ById?id={WeightEntry.Id}");
                WeightEntry = updated;

                return;
            }

            // New detail: post to create endpoint, server computes BruteWeight
            WeightDetailDto newDetail = new WeightDetailDto
            {
                FK_WeightEntryId = WeightEntry.Id,
                Weight = _diferenciaAbs,
                FK_WeightedProductId = Product?.Id,
                RequiredAmount = ProductQuantity,
                WeightedBy = DeviceInfo.Name,
                Notes = DetailNotes
            };

            WeightDetailDto created = await _apiService.PostAsync<WeightDetailDto>("api/Weight/Detail", newDetail);
            WeightEntryDto refreshed = await _apiService.GetAsync<WeightEntryDto>($"api/Weight/ById?id={WeightEntry.Id}");
            WeightEntry = refreshed;
        }

        public async Task PutSecondaryTara()
        {
            if (TaraCurrentValue <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(TaraCurrentValue), "Secondary tara cannot be negative or 0.");
            }
            if (WeightEntry == null)
            {
                throw new InvalidOperationException("WeightEntry is not initialized.");
            }
            if (TargetWeightDetail == null)
            {
                throw new InvalidOperationException("TargetWeightDetail must be set to update the secondary tara.");
            }
            WeightDetailDto? detail = WeightEntry.WeightDetails.FirstOrDefault(w => w.Id == TargetWeightDetail.Value);
            if (detail == null)
                throw new InvalidOperationException("No weight detail found for the current product.");

            double tara = TaraCurrentValue;
            await _apiService.PutWithRetryAsync<object>(
                $"api/Weight/Detail/{detail.Id}/SecondaryTare",
                buildBody: () => Task.FromResult<object?>(new { SecondaryTare = tara }),
                refetch: async () =>
                {
                    WeightEntryDto fresh = await _apiService.GetAsync<WeightEntryDto>($"api/Weight/ById?id={WeightEntry.Id}");
                    WeightEntry = fresh;
                });

            // Reflect server state locally (re-find in case WeightEntry was refreshed during retry)
            WeightDetailDto? refreshed = WeightEntry.WeightDetails.FirstOrDefault(w => w.Id == detail.Id);
            if (refreshed != null)
            {
                refreshed.SecondaryTare = tara;
                refreshed.IsLoaded = false;
            }
        }

        private void ValidateBeforePosting()
        {
            if (Preferences.Get("RequirePartner", false) || Providers)
            {
                if (Partner is null || Partner.Id < 1)
                    throw new InvalidOperationException("Es obligatorio especificar al socio.");
            }
            if (Providers && (Product is null || Product.Id <= 0))
            {
                throw new InvalidOperationException("Es obligatorio especificar el producto para las descargas de proveedores.");
            }
            if (_pesoTotal == 0)
            {
                throw new InvalidOperationException("Peso total no puede ser cero.");
            }
            if (WeightEntry == null)
            {
                throw new InvalidOperationException("WeightEntry is not initialized.");
            }
            //if (_tara == 0 && WeightEntry.TareWeight != 0)//IF COMENTED, LOOKS LIKE WE ALLOW WEIGHT PROCESESS WITHOUTT INITIAL WEIGHTS
            //{
            //    throw new InvalidOperationException("Tare weight must be set equal to the received before capturing a new weight entry.");
            //}
            if (string.IsNullOrEmpty(WeightEntry.VehiclePlate) && !Preferences.Get("SecondaryTerminal", false))
            {
                throw new InvalidOperationException("Es obligatorio especificar la placa del vehiculo.");
            }
        }

        /// <summary>
        /// Classic/weightless entry flow (issue #122 follow-up): this ViewModel used to post the
        /// partner + product with no credit check at all — unlike the "Add Product" and
        /// "Change Product" flows in DetailedWeightView, which both go through
        /// `api/Weight/ValidateCredit`. Mirrors their skip conditions (IgnoreCreditLimit or
        /// CreditLimit &lt;= 0 both mean "unlimited") before deferring to the server for the
        /// actual check, so all three entry points enforce the same rule.
        /// </summary>
        private async Task ValidateCreditForCaptureAsync()
        {
            if (Product is null || Product.Id <= 0)
                return; // no product attached to this capture — nothing to validate

            if (Partner is null || Partner.Id <= 0)
                return; // no partner assigned; ValidateBeforePosting already enforces RequirePartner/Providers

            if (Partner.IgnoreCreditLimit || Partner.CreditLimit <= 0)
                return; // unlimited credit

            double requestedAmount = ProductQuantity * Product.Precio;

            CreditValidationResponse result = await _apiService.GetAsync<CreditValidationResponse>(
                $"api/Weight/ValidateCredit?partnerId={Partner.Id}&requestedAmount={requestedAmount}");

            if (!result.IsValid)
            {
                throw new InvalidOperationException(result.Message ?? "Crédito insuficiente para este producto.");
            }
        }

        private async Task PrintTurnAsync(WeightEntryDto newWeightEntry)
        {
            try
            {
                TurnDto turn = await _apiService.GetAsync<TurnDto>($"api/Turn?weightId={newWeightEntry.Id}");

                await _apiService.PostAsync<object>("api/Print/Text", turn.PrintData(Partner?.RazonSocial));
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error printing turn: " + ex.Message);
            }
        }

        private async Task PostNewWeightEntry(bool printTurn = false)
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

            WeightEntry = newEntry;

            if (Product is not null && Product.Id > 0)
            {
                WeightDetailDto detailToAdd = new WeightDetailDto
                {
                    FK_WeightEntryId = WeightEntry.Id,
                    FK_WeightedProductId = Product.Id,
                    RequiredAmount = ProductQuantity,
                    WeightedBy = DeviceInfo.Name,
                    Notes = DetailNotes,
                };
                await _apiService.PostAsync<WeightDetailDto>("api/Weight/Detail", detailToAdd);
                // Refresh to get server-assigned detail ID
                WeightEntry = await _apiService.GetAsync<WeightEntryDto>($"api/Weight/ById?id={WeightEntry.Id}");
            }

            if (printTurn)
                await PrintTurnAsync(newEntry);
        }

    }
}
