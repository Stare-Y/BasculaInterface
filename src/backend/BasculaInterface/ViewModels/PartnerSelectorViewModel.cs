using BasculaInterface.ViewModels.Base;
using Core.Application.DTOs;
using Core.Application.Services;
using System.Collections.ObjectModel;

namespace BasculaInterface.ViewModels
{
    public class PartnerSelectorViewModel : ViewModelBase
    {
        public ObservableCollection<ClienteProveedorDto> Partners { get; set; } = new ObservableCollection<ClienteProveedorDto>();

        private readonly IApiService? _apiService;

        public PartnerSelectorViewModel(IApiService apiService)
        {
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
        }

        public PartnerSelectorViewModel() { }

        public async Task SearchPartners(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                throw new ArgumentException("Search term cannot be null or empty.", nameof(searchTerm));
            }
            if (_apiService is null)
            {
                throw new InvalidOperationException("API service is not initialized.");
            }
            IEnumerable<ClienteProveedorDto> partners = await _apiService.GetAsync<IEnumerable<ClienteProveedorDto>>($"api/ClienteProveedor/ByName?name={searchTerm}");
            Partners.Clear();
            foreach (var partner in partners)
            {
                Partners.Add(partner);
            }
        }
    }
}
