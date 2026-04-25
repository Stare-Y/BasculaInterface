using BasculaInterface.Exceptions;
using BasculaInterface.Models;
using BasculaInterface.ViewModels.Base;
using Core.Application.DTOs;
using Core.Application.Services;
using Core.Domain.Entities.Weight;
using Microsoft.IdentityModel.Tokens;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;

namespace BasculaInterface.ViewModels
{
    public class PendingWeightsViewModel : ViewModelBase
    {
        private List<WeightEntryDto> _pendingWeights { get; set; } = [];
        private List<ClienteProveedorDto> _clienteProveedorDtos { get; set; } = [];
        public ObservableCollection<PendingWeightViewRow> PendingWeightsCharge { get; set; } = [];
        public ObservableCollection<PendingWeightViewRow> PendingWeightsDischarge { get; set; } = [];
        public ObservableCollection<ExternalTargetBehaviorDto> AvailableDocumentTypes { get; set; } = [];

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

                    // Apply the same filtering rules as OnAppearing and Reconect
                    if (Preferences.Get("ShowDocumentTypeFilter", false))
                    {
                        await LoadExternalTargetBehaviors();
                    }

                    if (Preferences.Get("PreferedDocumentType", null) is string preferedDocumentType)
                    {
                        int preferedId = int.TryParse(preferedDocumentType, out int result) ? result : 0;
                        ShowDocumentsWithId(preferedId);
                    }
                }
                catch (Exception)
                {
                    // Exceptions are handled by the view or LoadPendingWeightsAsync itself
                    // Just ensure IsRefreshing is reset
                }
                finally
                {
                    IsRefreshing = false;
                }
            });
        }

        public async Task LoadPendingWeightsAsync(CancellationToken cancellationToken = default)
        {
            _pendingWeights.Clear();

            try
            {
                _pendingWeights = await _apiService.GetAsync<List<WeightEntryDto>>("api/Weight/Pending", cancellationToken);

                await LoadClienteProveedorAsync(cancellationToken);

                BuildObservableCollection();
            }
            finally
            {
                OnCollectionChanged(nameof(_pendingWeights));
                IsRefreshing = false;
            }
        }

        public void ShowDocumentsWithId(int desiredId = 0)
        {
            BuildObservableCollection(desiredId);
        }

        public async Task LoadExternalTargetBehaviors(CancellationToken cancellationToken = default)
        {
            AvailableDocumentTypes.Clear();

            List<ExternalTargetBehaviorDto> behaviors = await _apiService.GetAsync<List<ExternalTargetBehaviorDto>>($"api/ExternalTargetBehavior/Available", cancellationToken);

            AvailableDocumentTypes.Add(new ExternalTargetBehaviorDto { Id = 0, TargetName = "Mostrar Todos" });

            foreach (var behavior in behaviors)
            {
                AvailableDocumentTypes.Add(behavior);
            }

            OnCollectionChanged(nameof(AvailableDocumentTypes));
        }

        private void BuildObservableCollection(int externalPreferedId = 0)
        {
            PendingWeightsCharge.Clear();
            PendingWeightsDischarge.Clear();

            // Sort pending weights first, then add to collections (avoids reassigning ObservableCollection)
            var sortedWeights = _pendingWeights.OrderByDescending(w => w.CreatedAt);

            foreach (WeightEntryDto weight in sortedWeights)
            {
                //if (Preferences.Get("SecondaryTerminal", false))
                //{
                //    //skip weights with no tare weight
                //    continue;
                //}

                ClienteProveedorDto? partner = _clienteProveedorDtos.FirstOrDefault(p => p.Id == weight.PartnerId);
                if (partner != null)
                {
                    string teoricWeightText = string.Empty;

                    if (weight.WeightDetails.Count > 0)
                    {
                        teoricWeightText +=
                            "Total (teorico): "
                            + (weight.WeightDetails.Where(d => d.IsLoaded).Sum(d => d.Weight)
                            + weight.TareWeight).ToString()
                            + " kg.";
                    }

                    if (!(externalPreferedId == 0 || ((weight.ExternalTargetBehaviorFK is null && !Preferences.Get("FilterNull", true)) || weight.ExternalTargetBehaviorFK == externalPreferedId)))
                        continue;

                    if (partner.IsProvider)
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
                    if (!(externalPreferedId == 0 || ((weight.ExternalTargetBehaviorFK is null && !Preferences.Get("FilterNull", true)) || weight.ExternalTargetBehaviorFK == externalPreferedId)))
                        continue;

                    PendingWeightsCharge.Add(new PendingWeightViewRow(weight, new ClienteProveedorDto { RazonSocial = "No identificado" }, string.Empty));
                }
            }

            OnPropertyChanged(nameof(PendingWeightsCharge));
            OnPropertyChanged(nameof(PendingWeightsDischarge));
        }

        private async Task LoadClienteProveedorAsync(CancellationToken cancellationToken = default)
        {
            _clienteProveedorDtos.Clear();

            if (_pendingWeights.Count == 0)
            {
                OnCollectionChanged(nameof(_clienteProveedorDtos));
                return;
            }

            // Batch fetch all partners to avoid N+1 queries
            int[] partnerIds = _pendingWeights
                .Where(w => w.PartnerId.HasValue && w.PartnerId.Value > 0)
                .Select(w => w.PartnerId!.Value)
                .Distinct()
                .ToArray();

            if (partnerIds.Length > 0)
            {
                string idsQuery = string.Join("&ids=", partnerIds);
                List<ClienteProveedorDto>? partners = await _apiService.GetAsync<List<ClienteProveedorDto>>($"api/ClienteProveedor/ByMultipleIds?ids={idsQuery}", cancellationToken);

                if (partners != null)
                {
                    foreach (ClienteProveedorDto partner in partners)
                    {
                        partner.RazonSocial = partner.Code.IsNullOrEmpty() ? partner.RazonSocial : $"{partner.Code} - {partner.RazonSocial}";
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

            // await PrintTurnAsync(newEntry, partner);
        }
    }
}
