using BasculaInterface.Exceptions;
using BasculaInterface.Models;
using BasculaInterface.ViewModels.Base;
using Core.Application.DTOs;
using Core.Application.Services;
using System.Collections.ObjectModel;

namespace BasculaInterface.ViewModels
{
    public class PendingWeightsViewModel : ViewModelBase
    {
        private List<WeightEntryDto> _pendingWeights { get; set; } = new List<WeightEntryDto>();
        private List<ClienteProveedorDto> _clienteProveedorDtos { get; set; } = new List<ClienteProveedorDto>();
        public ObservableCollection<PendingWeightViewRow> PendingWeights { get; } = new ObservableCollection<PendingWeightViewRow>();

        private readonly IApiService _apiService = null!;

        public PendingWeightsViewModel(IApiService apiService)
        {
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
        }

        public PendingWeightsViewModel() { }

        public async Task LoadPendingWeightsAsync()
        {
            _pendingWeights.Clear();

            try
            {
                _pendingWeights = await _apiService.GetAsync<List<WeightEntryDto>>("api/Weight/Pending");

                await LoadClienteProveedorAsync();

                BuildObservableCollection();
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
            finally
            {
                await LoadClienteProveedorAsync();

                BuildObservableCollection();

                OnCollectionChanged(nameof(_pendingWeights));
            }
        }

        private void BuildObservableCollection()
        {
            PendingWeights.Clear();
            foreach (WeightEntryDto weight in _pendingWeights)
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

                    PendingWeights.Add(new PendingWeightViewRow(weight, partner, teoricWeightText));
                }
                else
                {
                    PendingWeights.Add(new PendingWeightViewRow(weight, new ClienteProveedorDto { RazonSocial = "Socio no identificado" }, string.Empty));
                }
            }
        }

        private async Task LoadClienteProveedorAsync()
        {
            _clienteProveedorDtos.Clear();

            foreach (WeightEntryDto weight in _pendingWeights)
            {
                //get the partner id
                if (weight.PartnerId.HasValue && weight.PartnerId.Value > 0)
                {
                    ClienteProveedorDto? partner = await _apiService.GetAsync<ClienteProveedorDto>($"api/ClienteProveedor/{weight.PartnerId.Value}");
                    if (partner != null)
                    {
                        _clienteProveedorDtos.Add(partner);
                    }
                }
            }

            OnCollectionChanged(nameof(_clienteProveedorDtos));
        }
    }
}
