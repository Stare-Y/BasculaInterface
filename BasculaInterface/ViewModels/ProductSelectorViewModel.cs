using BasculaInterface.ViewModels.Base;
using Core.Application.DTOs;
using Core.Application.Services;
using System.Collections.ObjectModel;

namespace BasculaInterface.ViewModels
{
    public class ProductSelectorViewModel : ViewModelBase
    {
        private readonly IApiService? _apiService;
        public ProductSelectorViewModel(IApiService apiService)
        {
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
        }

        public ProductSelectorViewModel() { }

        public ObservableCollection<ProductoDto> Products { get; } = new ObservableCollection<ProductoDto>();

        public async Task SearchProducts(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                throw new ArgumentException("Search term cannot be null or empty.", nameof(searchTerm));
            }
            
            if (_apiService is null)
            {
                throw new InvalidOperationException("API service is not initialized.");
            }

            List<ProductoDto> products = await _apiService.GetAsync<List<ProductoDto>>($"api/Productos/ByName/{searchTerm}");

            Products.Clear();

            foreach (var product in products)
            {
                //if (product.IdValorClasificacion6 == 0)
                //    continue;

                Products.Add(product);
            }
        }
    }
}
