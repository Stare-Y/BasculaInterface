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

            _pendingWeights = await _apiService.GetAsync<List<WeightEntryDto>>("Weight/Pending");

            await LoadClienteProveedorAsync();

            BuildObservableCollection();

            OnCollectionChanged(nameof(_pendingWeights));
        }

        private void BuildObservableCollection()
        {
            PendingWeights.Clear();
            foreach (WeightEntryDto weight in _pendingWeights)
            {
                ClienteProveedorDto? partner = _clienteProveedorDtos.FirstOrDefault(p => p.Id == weight.PartnerId);
                if (partner != null)
                {
                    string historyText = string.Empty;

                    if(weight.WeightDetails.Count > 0)
                    {
                        foreach (WeightDetailDto detail in weight.WeightDetails)
                        {
                            historyText += $"+|{detail.Weight}|";
                        }
                    }

                    PendingWeights.Add(new PendingWeightViewRow(weight, partner, historyText));
                }
            }
        }

        private async Task LoadClienteProveedorAsync()
        {
            _clienteProveedorDtos.Clear();
            
            foreach(WeightEntryDto weight in _pendingWeights)
            {
                //get the partner id
                if (weight.PartnerId.HasValue && weight.PartnerId.Value > 0)
                {
                    ClienteProveedorDto? partner = await _apiService.GetAsync<ClienteProveedorDto>($"ClienteProveedor/{weight.PartnerId.Value}");
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
