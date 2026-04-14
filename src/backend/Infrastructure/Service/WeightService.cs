using Core.Application.DTOs;
using Core.Application.DTOs.ContpaqiComercial;
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
        private readonly IProviderPurchaseService _providerPurchaseService;
        private readonly ComercialSDKClientSettings _comercialSDKSettings;
        public WeightService(IWeightRepo weightRepo, IProductService productService, IClienteProveedorService clienteProveedorService, IApiService apiService, IOptions<ComercialSDKClientSettings> options, IProviderPurchaseService providerPurchaseService)
        {
            _weightRepo = weightRepo;

            _comercialSDKSettings = options.Value;

            _apiService = apiService;

            _clienteProveedorService = clienteProveedorService;

            _productService = productService;

            _providerPurchaseService = providerPurchaseService;
        }
        public async Task<WeightEntryDto> CreateAsync(WeightEntryDto weightEntry)
        {
            if(weightEntry.Id > 0)
            {
                //this means a currently existing one, is trying to get its initial weight, so, lets update it instead

                await UpdateAsync(weightEntry);

                return weightEntry;
            }
            WeightEntry newEntry = await _weightRepo.CreateAsync(weightEntry.ToEntity());

            return new WeightEntryDto(newEntry);
        }

        public async Task<WeightEntryDto> GetByIdAsync(int id)
        {
            WeightEntry entry = await _weightRepo.GetByIdAsync(id);

            return new WeightEntryDto(entry);
        }

        public async Task<IEnumerable<WeightEntryDto>> GetAllAsync(int top = 30, uint page = 1)
        {
            return (await _weightRepo.GetAllAsync(top, page)).Select(we => new WeightEntryDto(we));
        }

        public async Task<IEnumerable<WeightEntryDto>> GetAllComplete(int top = 30, uint page = 1)
        {
            return (await _weightRepo.GetAllComplete(top, page)).Select(we => new WeightEntryDto(we));
        }
        public async Task<IEnumerable<WeightEntryDto>> GetByDateRange(DateOnly startDate, DateOnly endDate, int top = 30, uint page = 1)
        {
            return (await _weightRepo.GetByDateRange(startDate, endDate, top: top, page: page)).Select(we => new WeightEntryDto(we));
        }

        public async Task<IEnumerable<WeightEntryDto>> GetAllByPartnerAsync(int partnerId, int top = 30, uint page = 1)
        {
            return (await _weightRepo.GetAllByPartnerAsync(partnerId, top, page)).Select(we => new WeightEntryDto(we));
        }

        public async Task<IEnumerable<WeightEntryDto>> GetPendingWeights(int top = 30, uint page = 1)
        {
            return (await _weightRepo.GetPendingWeights(top, page)).Select(we => new WeightEntryDto(we));
        }

        public async Task UpdateAsync(WeightEntryDto weightEntry)
        {
            await _weightRepo.UpdateAsync(weightEntry.ToEntity());

            if (weightEntry.ConcludeDate.HasValue)
                await _providerPurchaseService.ConcludeByWeightEntryAsync(weightEntry.Id);
        }

        public async Task UpdateAsync(WeightEntry weightEntry)
        {
            await _weightRepo.UpdateAsync(weightEntry);

            if (weightEntry.ConcludeDate.HasValue)
                await _providerPurchaseService.ConcludeByWeightEntryAsync(weightEntry.Id);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            return await _weightRepo.DeleteAsync(id);
        }

        public Task<bool> DeleteDetailAsync(int id)
        {
            return _weightRepo.DeleteDetailAsync(id);
        }

        public async Task<GenericResponse<ContpaqiComercialResult>> SendToContpaqiComercial(int id)
        {
            WeightEntry weightEntry = await _weightRepo.GetByIdAsync(id);

            Console.WriteLine($"Sending WeightEntry to contpaq: {weightEntry}");

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

            GenericResponse<ContpaqiComercialResult> result = await _apiService.PostAsync<GenericResponse<ContpaqiComercialResult>>(_comercialSDKSettings.ApiUrl + "/ComercialSDK/Document", new { Document = payload, Empresa = _comercialSDKSettings.TargetEmpresa });

            if (result.Data is null || result.Data.ResultingId <= 0)
            {
                return new GenericResponse<ContpaqiComercialResult> { Message = $"Error posting to SDK: {result.Message}" };
            }

            weightEntry.ConptaqiComercialFK = result.Data.ResultingId;
            weightEntry.ContpaqiComercialFolio = result.Data.ResultingFolio;
            Console.WriteLine($"Received Notes: {result.Message}");
            weightEntry.Notes += " " + result.Message;

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
            ProviderPurchaseDto? purchase = await _providerPurchaseService.GetByWeightEntryIdAsync(weightEntry.Id);
            double purchasePrice = (double)(purchase?.Price ?? 0);

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
                CodConcepto = weightEntry.ExternalTargetBehavior.TargetConcept,
                Serie = weightEntry.ExternalTargetBehavior.TargetSerie,
                Fecha = DateTime.Now,
                CodigoCteProv = cteProovedor.Code,
                cObservaciones = "Generado en Bascula CPE",
                Movimientos = products.Select(p =>
                    new MovimientoDto
                    {
                        CodigoProducto = p.Code,
                        CodigoAlmacen = p.IdAlmacen ?? weightEntry.ExternalTargetBehavior.TargetAlmacen,
                        Unidades = GetUnidadesFromProductAndDetail( weightEntry.WeightDetails.First(wd => wd.FK_WeightedProductId == p.Id),p),
                        Referencia = $"Pesado por: {weightEntry.WeightDetails.First(wd => wd.FK_WeightedProductId == p.Id).WeightedBy}",
                        Precio = purchasePrice
                    }).ToArray()
            };
        }

        private double GetUnidadesFromProductAndDetail(WeightDetail weightDetail, ProductoDto product)
        {
            if (product.IsGranel)
                return weightDetail.Weight;
            else
                return weightDetail.RequiredAmount ?? 0;
        }

        public async Task<CreditValidationResponse> ValidatePartnerCreditAsync(int partnerId, double requestedAmount)
        {
            // Fetch the partner to get their credit information
            ClienteProveedorDto partner = await _clienteProveedorService.GetById(partnerId);

            // If partner ignores credit limit, always return valid
            if (partner.IgnoreCreditLimit)
            {
                return new CreditValidationResponse
                {
                    IsValid = true,
                    AvailableCredit = partner.AvailableCredit,
                    RequestedAmount = requestedAmount,
                    PendingEntriesCost = 0,
                    RemainingCredit = partner.AvailableCredit - requestedAmount,
                    Message = "Crédito válido."
                };
            }

            // If partner has no credit limit set, they can't make purchases on credit
            if (partner.CreditLimit <= 0)
            {
                return new CreditValidationResponse
                {
                    IsValid = false,
                    AvailableCredit = 0,
                    RequestedAmount = requestedAmount,
                    PendingEntriesCost = 0,
                    RemainingCredit = 0,
                    Message = "El socio no tiene límite de crédito configurado."
                };
            }

            // Get all pending (not finished) weight entries for this partner
            IEnumerable<WeightEntry> pendingEntries = await _weightRepo.GetPendingWeightsByPartnerAsync(partnerId);

            // Calculate the total cost of pending entries
            // If weight is already measured (Weight > 0), use Weight * Price
            // Otherwise, use RequiredAmount * Price
            double pendingEntriesCost = 0;
            foreach (WeightEntry entry in pendingEntries)
            {
                foreach (WeightDetail detail in entry.WeightDetails)
                {
                    if (detail.FK_WeightedProductId.HasValue && detail.ProductPrice.HasValue)
                    {
                        // Use actual weight if measured, otherwise use required amount
                        double quantity = detail.Weight > 0 
                            ? detail.Weight 
                            : (detail.RequiredAmount ?? 0);

                        pendingEntriesCost += detail.ProductPrice.Value * quantity;
                    }
                }
            }

            // Calculate remaining credit after considering pending entries and requested amount
            double remainingCredit = partner.AvailableCredit - requestedAmount - pendingEntriesCost;
            bool isValid = remainingCredit >= 0;

            return new CreditValidationResponse
            {
                IsValid = isValid,
                AvailableCredit = partner.AvailableCredit,
                RequestedAmount = requestedAmount,
                PendingEntriesCost = pendingEntriesCost,
                RemainingCredit = remainingCredit,
                Message = isValid
                    ? "Crédito válido."
                    : "Crédito insuficiente."
            };
        }
    }
}
