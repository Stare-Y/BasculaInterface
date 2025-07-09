using BasculaInterface.ViewModels.Base;
using Core.Application.DTOs;
using Core.Application.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasculaInterface.ViewModels
{
    public class PendingWeightsViewModel : ViewModelBase
    {
        public List<WeightEntryDto> PendingWeights { get; set; } = new List<WeightEntryDto>();

        private readonly IApiService _apiService;

        public PendingWeightsViewModel(IApiService apiService)
        {
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
        }

        public async Task LoadPendingWeightsAsync()
        {
            PendingWeights.Clear();

            PendingWeights = await _apiService.GetAsync<List<WeightEntryDto>>("Weight");

            OnCollectionChanged(nameof(PendingWeights));
        }
    }
}
