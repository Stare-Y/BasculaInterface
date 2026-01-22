using Core.Application.DTOs;
using Core.Application.DTOs.ContpaqiComercial;
using Core.Application.Extensions;
using Core.Application.Services;
using Core.Domain.Entities.Base;
using Core.Domain.Entities.Weight;
using Core.Domain.Interfaces;
using Microsoft.Extensions.Options;

namespace Infrastructure.Service
{
    public class WeightService : IWeightService
    {
        private readonly IWeightRepo _weightRepo;
        private readonly IApiService _apiService;
        private readonly IClienteProveedorService _clienteProveedorService;
        private readonly IProductService _productService;
        private readonly ComercialSDKClientSettings _comercialSDKSettings;
        public WeightService(IWeightRepo weightRepo, IProductService productService, IClienteProveedorService clienteProveedorService, IApiService apiService, IOptions<ComercialSDKClientSettings> options)
        {
            _weightRepo = weightRepo;

            _comercialSDKSettings = options.Value;

            _apiService = apiService;

            _clienteProveedorService = clienteProveedorService;

            _productService = productService;
        }
        public async Task<WeightEntryDto> CreateAsync(WeightEntryDto weightEntry)
        {
            WeightEntry newEntry = await _weightRepo.CreateAsync(weightEntry.ConvertToBaseEntry());

            return newEntry.ConvertToDto();
        }

        public async Task<WeightEntryDto> GetByIdAsync(int id)
        {
            WeightEntry entry = await _weightRepo.GetByIdAsync(id);

            return entry.ConvertToDto();
        }

        public async Task<IEnumerable<WeightEntryDto>> GetAllAsync(int top = 30, uint page = 1)
        {
            return WeightExtensions.ConvertRangeToDto(await _weightRepo.GetAllAsync(top, page));
        }

        public async Task<IEnumerable<WeightEntryDto>> GetAllComplete(int top = 30, uint page = 1)
        {
            return WeightExtensions.ConvertRangeToDto(await _weightRepo.GetAllComplete(top, page));
        }
        public async Task<IEnumerable<WeightEntryDto>> GetByDateRange(DateOnly startDate, DateOnly endDate, int top = 30, uint page = 1)
        {
            return WeightExtensions.ConvertRangeToDto(await _weightRepo.GetByDateRange(startDate, endDate, top: top, page: page));
        }

        public async Task<IEnumerable<WeightEntryDto>> GetAllByPartnerAsync(int partnerId, int top = 30, uint page = 1)
        {
            return WeightExtensions.ConvertRangeToDto(await _weightRepo.GetAllByPartnerAsync(partnerId, top, page));
        }

        public async Task<IEnumerable<WeightEntryDto>> GetPendingWeights(int top = 30, uint page = 1)
        {
            return WeightExtensions.ConvertRangeToDto(await _weightRepo.GetPendingWeights(top, page));
        }

        public async Task UpdateAsync(WeightEntryDto weightEntry)
        {
            await _weightRepo.UpdateAsync(weightEntry.ConvertToBaseEntry());
        }

        public async Task UpdateAsync(WeightEntry weightEntry)
        {
            await _weightRepo.UpdateAsync(weightEntry);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            return await _weightRepo.DeleteAsync(id);
        }

        public Task<bool> DeleteDetailAsync(int id)
        {
            return _weightRepo.DeleteDetailAsync(id);
        }

        public async Task<GenericResponse<int?>> SendToContpaqiComercial(int id)
        {
            WeightEntry weightEntry = await _weightRepo.GetByIdAsync(id);

            if (weightEntry.PartnerId <= 0)
            {
                throw new InvalidOperationException("Need a ClienteProveedor related to the weight before posting to SDK");
            }
            if (!weightEntry.WeightDetails.Any(wd => wd.FK_WeightedProductId != null))
            {
                throw new InvalidOperationException("At least 1 product needs to be related to be able to make the pedido");
            }
            if(weightEntry.ExternalTargetBehaviorFK is null || weightEntry.ExternalTargetBehavior is null)
            {
                throw new InvalidOperationException("An External Target Behavior is required to post to SDK");
            }
            if (weightEntry.ConptaqiComercialFK > 0)
            {
                throw new InvalidOperationException("This entry already has a record with contpaq");
            }
            DocumentoDto payload = await BuildContpaqiDocumentDto(weightEntry);

            GenericResponse<int?> result = await _apiService.PostAsync<GenericResponse<int?>>(_comercialSDKSettings.ApiUrl + "/ComercialSDK/Document", new { Document = payload, Empresa = _comercialSDKSettings.TargetEmpresa });

            if (result.Data <= 0)
            {
                throw new Exception($"Error posting to SDK: {result.Message}");
            }

            weightEntry.ConptaqiComercialFK = result.Data;
            Console.WriteLine($"Received Notes: {result.Message}");
            weightEntry.Notes += result.Message;

            await UpdateAsync(weightEntry);

            return result;
        }

        private async Task<DocumentoDto> BuildContpaqiDocumentDto(WeightEntry weightEntry)
        {
            if(weightEntry.ExternalTargetBehavior is null)
            {
                throw new InvalidOperationException("An External Target Behavior is required to build the document");
            }
            if(string.IsNullOrEmpty(weightEntry.ExternalTargetBehavior.TargetSerie))
            {
                throw new InvalidOperationException("The External Target Behavior needs to have a target serie to build the document");
            }
            if(string.IsNullOrEmpty(weightEntry.ExternalTargetBehavior.TargetAlmacen))
            {
                throw new InvalidOperationException("The External Target Behavior needs to have a target serie to build the document");
            }
            if(string.IsNullOrEmpty(weightEntry.ExternalTargetBehavior.TargetConcept))
            {
                throw new InvalidOperationException("The External Target Behavior needs to have a target serie to build the document");
            }
            ClienteProveedorDto cteProovedor = await _clienteProveedorService.GetById(weightEntry.PartnerId!.Value);
            List<ProductoDto> products = [];
            foreach (WeightDetail weightDetail in weightEntry.WeightDetails)
            {
                if (weightDetail.FK_WeightedProductId == null)
                    continue;

                products.Add(await _productService.GetByIdAsync(weightDetail.FK_WeightedProductId!.Value));
            }

            bool isProvider = cteProovedor.IsProvider;

            return new DocumentoDto
            {
                CodConcepto = !isProvider ? weightEntry.ExternalTargetBehavior.TargetConcept 
                    : throw new NotImplementedException("Can't create documents from providers, just clients for now"),
                Serie = weightEntry.ExternalTargetBehavior.TargetSerie,
                Fecha = DateTime.Now,
                CodigoCteProv = cteProovedor.Code,
                cObservaciones = "Generado en Bascula CPE",
                Movimientos = products.Select(p =>
                    new MovimientoDto
                    {
                        CodigoProducto = p.Code,
                        CodigoAlmacen = weightEntry.ExternalTargetBehavior.TargetAlmacen,
                        Unidades = weightEntry.WeightDetails.First(wd => wd.FK_WeightedProductId == p.Id).Weight,
                        Referencia = $"Pesado por: {weightEntry.WeightDetails.First(wd => wd.FK_WeightedProductId == p.Id).WeightedBy}"
                    }).ToArray()
            };
        }
    }
}
