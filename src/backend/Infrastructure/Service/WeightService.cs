using Core.Application.DTOs;
using Core.Application.DTOs.ContpaqiComercial;
using Core.Application.Services;
using Core.Application.Settings;
using Core.Domain.Entities.Base;
using Core.Domain.Entities.Behaviors;
using Core.Domain.Entities.Weight;
using Core.Domain.Interfaces;
using Microsoft.Extensions.Options;

namespace Infrastructure.Service
{
    public class WeightService : IWeightService
    {
        private readonly IWeightRepo _weightRepo;
        private readonly IExternalTargetBehaviorService _targetBehaviorService;
        private readonly IApiService _apiService;
        private readonly IClienteProveedorService _clienteProveedorService;
        private readonly IProductService _productService;
        private readonly ComercialSDKClientSettings _comercialSDKSettings;
        private readonly WeightSettings _weightSettings;
        public WeightService(IWeightRepo weightRepo, IExternalTargetBehaviorService targetBehaviorService, IProductService productService, IClienteProveedorService clienteProveedorService, IApiService apiService, IOptions<ComercialSDKClientSettings> options, IOptions<WeightSettings> weightSettingsOptions)
        {
            _weightRepo = weightRepo;

            _comercialSDKSettings = options.Value;

            _weightSettings = weightSettingsOptions.Value;

            _apiService = apiService;

            _clienteProveedorService = clienteProveedorService;

            _productService = productService;

            _targetBehaviorService = targetBehaviorService;
        }
        public async Task<WeightEntryDto> CreateAsync(WeightEntryDto weightEntry)
        {
            if (weightEntry.Id > 0)
            {
                //this means a currently existing one, is trying to get its initial weight, so, lets update it instead

                await UpdateAsync(weightEntry);

                return weightEntry;
            }
            WeightEntry entity = weightEntry.ToEntity();
            entity.BruteWeight = entity.TareWeight;
            WeightEntry newEntry = await _weightRepo.CreateAsync(entity);

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
        }

        public async Task UpdateAsync(WeightEntry weightEntry, bool force = false)
        {
            await _weightRepo.UpdateAsync(weightEntry, force);
        }

        public async Task<WeightDetailDto> CreateDetailAsync(WeightDetailDto dto)
        {
            WeightEntry entry = await _weightRepo.GetByIdAsync(dto.FK_WeightEntryId);
            if (entry.ConcludeDate != null)
                throw new InvalidOperationException("No se pueden agregar productos a un proceso ya finalizado.");

            WeightDetail detail = new WeightDetail
            {
                FK_WeightEntryId = dto.FK_WeightEntryId,
                FK_WeightedProductId = dto.FK_WeightedProductId,
                RequiredAmount = dto.RequiredAmount,
                Costales = dto.Costales,
                Notes = dto.Notes,
                Weight = dto.Weight,
                Tare = dto.Weight > 0 ? entry.BruteWeight : dto.Tare,
                IsLoaded = true
            };

            WeightDetail created = await _weightRepo.CreateDetailAsync(detail);

            if (detail.IsLoaded && detail.Weight > 0)
                await _weightRepo.RecomputeBruteWeightAsync(dto.FK_WeightEntryId);

            return new WeightDetailDto(created);
        }

        public async Task SetSecondaryTareAsync(int detailId, double tare)
        {
            if (tare <= 0)
                throw new ArgumentOutOfRangeException(nameof(tare), "La tara secundaria debe ser mayor que cero.");

            WeightDetail detail = await _weightRepo.GetDetailByIdAsync(detailId);
            if (detail.WeightEntry.ConcludeDate != null)
                throw new InvalidOperationException("No se puede modificar un proceso ya finalizado.");

            detail.SecondaryTare = tare;
            detail.IsLoaded = false;
            await _weightRepo.UpdateDetailAsync(detail);
        }

        public async Task RecordWeightAsync(int detailId, double weight, string weightedBy)
        {
            if (weight <= 0)
                throw new ArgumentOutOfRangeException(nameof(weight), "El peso debe ser mayor que cero.");

            WeightDetail detail = await _weightRepo.GetDetailByIdAsync(detailId);
            if (detail.WeightEntry.ConcludeDate != null)
                throw new InvalidOperationException("No se puede modificar un proceso ya finalizado.");

            detail.Weight = weight;
            detail.WeightedBy = weightedBy;
            detail.Tare = detail.WeightEntry.BruteWeight;
            await _weightRepo.UpdateDetailAsync(detail);

            if (detail.IsLoaded)
                await _weightRepo.RecomputeBruteWeightAsync(detail.FK_WeightEntryId);
        }

        public async Task<WeightEntryDto> MarkDetailLoadedAsync(int detailId)
        {
            WeightEntry entry = await _weightRepo.MarkDetailLoadedAsync(detailId);
            return new WeightEntryDto(entry);
        }

        public async Task ConcludeAsync(int weightEntryId)
        {
            await _weightRepo.ConcludeEntryAsync(weightEntryId);

            // Note: PedidoLine.Concluded is computed from received/pending amounts
            // (RequiredAmount - Σ loaded WeightDetail.Weight), not a stored flag tied
            // to WeightEntry conclusion — a truck can leave with a partial delivery,
            // concluding this entry while its pedido line(s) stay open with pending > 0
            // for a future visit. No pedido-side update is needed here.

            WeightEntry entry = await _weightRepo.GetByIdAsync(weightEntryId);
            if (entry.PartnerId > 0 && entry.ExternalTargetBehaviorFK > 0 && (entry.ConptaqiComercialFK == null || entry.ConptaqiComercialFK <= 0))
            {
                try
                {
                    await SendToContpaqiComercial(weightEntryId);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ConcludeAsync] Contpaqi post failed (non-fatal): {ex.Message}");
                }
            }
        }

        public async Task DeleteSafelyAsync(int id, string passwordHash)
        {
            // Same shared password as ChangeDetailProductAsync/ChangePartnerAsync/
            // ChangeDetailAmountAsync/DeleteDetailSafelyAsync (see design.md Decision 3 of
            // extend-delete-password-gate).
            if (string.IsNullOrEmpty(_weightSettings.ChangeProductPasswordHash) ||
                !string.Equals(passwordHash, _weightSettings.ChangeProductPasswordHash, StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException("Contraseña incorrecta.");
            }

            // Propagates KeyNotFoundException if the entry doesn't exist (or is already deleted).
            WeightEntry entry = await _weightRepo.GetByIdAsync(id);

            // Blocked once a Contpaqi document already exists — mirrors the same guard every
            // sibling guarded mutation on WeightEntry/WeightDetail enforces. Deliberately NOT
            // based on ConcludeDate (design.md Decision 2 of extend-delete-password-gate).
            if (entry.ConptaqiComercialFK > 0)
            {
                throw new InvalidOperationException("Este proceso ya cuenta con un documento en Contpaqi; no se puede eliminar.");
            }

            await _weightRepo.DeleteAsync(id);
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
            if (weightEntry.ExternalTargetBehaviorFK is null || weightEntry.ExternalTargetBehavior is null)
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

            await UpdateAsync(weightEntry, force: true);

            return result;
        }

        private async Task<DocumentoDto> BuildContpaqiDocumentDto(WeightEntry weightEntry)
        {
            if (weightEntry.ExternalTargetBehavior is null)
            {
                throw new InvalidOperationException("An External Target Behavior is required to build the document");
            }
            if (string.IsNullOrEmpty(weightEntry.ExternalTargetBehavior.TargetSerie))
            {
                throw new InvalidOperationException("The External Target Behavior needs to have a target serie to build the document");
            }
            if (string.IsNullOrEmpty(weightEntry.ExternalTargetBehavior.TargetAlmacen))
            {
                throw new InvalidOperationException("The External Target Behavior needs to have a target serie to build the document");
            }
            if (string.IsNullOrEmpty(weightEntry.ExternalTargetBehavior.TargetConcept))
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
                CodConcepto = weightEntry.ExternalTargetBehavior.TargetConcept,
                Serie = weightEntry.ExternalTargetBehavior.TargetSerie,
                Fecha = DateTime.Now,
                CodigoCteProv = cteProovedor.Code,
                cObservaciones = "Generado en Bascula CPE",
                Movimientos = weightEntry.WeightDetails.Select(d =>
                    new MovimientoDto
                    {
                        CodigoProducto = products.First(p => p.Id == d.FK_WeightedProductId).Code,
                        // Almacén precedence: the product's own classification-based default,
                        // then the entry-level ExternalTargetBehavior's TargetAlmacen — which
                        // for a pedido conversion is the hidden behavior the operator picked
                        // in the convert-to-weight dialog (design.md Decision 5).
                        CodigoAlmacen = products.First(p => p.Id == d.FK_WeightedProductId).IdAlmacen ?? weightEntry.ExternalTargetBehavior.TargetAlmacen,
                        Unidades = GetUnidadesFromProductAndDetail(d, products.First(p => p.Id == d.FK_WeightedProductId)),
                        Referencia = $"Pesado por: {d.WeightedBy}",
                        // Price precedence: the pedido line's own price (per-product, correct
                        // for multi-product entries) falling back to whatever price was
                        // captured directly on the detail for non-pedido weigh-ins.
                        Precio = (double)(d.PedidoLine?.Price ?? (decimal?)d.ProductPrice ?? 0)
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

        public async Task ChangeTargetDocumentBehavior(int weightId, int targetDocumentBehaviorId)
        {
            WeightEntry existingWeight = await _weightRepo.GetByIdAsync(weightId);

            ExternalTargetBehaviorDto targetBehavior = await _targetBehaviorService.GetByIdAsync(targetDocumentBehaviorId);

            //this seems redundant, but is so taht the target service throws exception if not found
            existingWeight.ExternalTargetBehaviorFK = targetBehavior.Id;

            //force true to not validate if concluded or that
            await _weightRepo.UpdateAsync(existingWeight, force:true);
        }

        public async Task<CreditValidationResponse> ValidatePartnerCreditAsync(int partnerId, double requestedAmount)
        {
            // Fetch the partner to get their credit information
            ClienteProveedorDto partner = await _clienteProveedorService.GetById(partnerId);

            // If partner ignores credit limit, or has no credit limit configured (CreditLimit <= 0
            // means "unlimited" per ERP convention, confirmed with the system's domain expert —
            // NOT "blocked"), always return valid.
            if (partner.IgnoreCreditLimit || partner.CreditLimit <= 0)
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

        public async Task ChangeDetailProductAsync(int detailId, int newProductId, string passwordHash)
        {
            // An empty configured hash means the feature hasn't been set up yet — never allow it to
            // be satisfied by an equally-empty submitted hash.
            if (string.IsNullOrEmpty(_weightSettings.ChangeProductPasswordHash) ||
                !string.Equals(passwordHash, _weightSettings.ChangeProductPasswordHash, StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException("Contraseña incorrecta.");
            }

            WeightDetail detail = await _weightRepo.GetDetailByIdAsync(detailId);

            // Blocked once a Contpaqi document already exists for this entry — mirrors the same
            // guard ChangePartnerAsync already enforces. Deliberately NOT based on ConcludeDate
            // (a concluded-but-not-yet-sent entry may still change product; see design.md
            // Decision 3 of change-weight-detail-amount).
            if (detail.WeightEntry?.ConptaqiComercialFK > 0)
            {
                throw new InvalidOperationException("Este proceso ya cuenta con un documento en Contpaqi; no se puede cambiar el producto.");
            }

            ProductoDto newProduct = await _productService.GetByIdAsync(newProductId);

            // Preserve whatever quantity has already been captured; this operation never touches it.
            double quantity = detail.Weight > 0 ? detail.Weight : (detail.RequiredAmount ?? 0);
            double oldCost = (detail.ProductPrice ?? 0) * quantity;
            double newCost = newProduct.Precio * quantity;

            // Only the incremental exposure needs validating — ValidatePartnerCreditAsync already
            // counts this detail's current cost inside the partner's pending-entries total, so
            // passing the full newCost here would double-count it.
            if (newCost > oldCost)
            {
                int partnerId = detail.WeightEntry?.PartnerId ?? 0;
                CreditValidationResponse creditResult = await ValidatePartnerCreditAsync(partnerId, newCost - oldCost);
                if (!creditResult.IsValid)
                {
                    throw new InvalidOperationException(creditResult.Message ?? "Crédito insuficiente para el nuevo producto.");
                }
            }

            detail.FK_WeightedProductId = newProductId;
            detail.ProductPrice = newProduct.Precio;

            // Deliberately not checking WeightEntry.ConcludeDate here — the password is the
            // intended override to correct a product after conclusion (see design.md Decision 3).
            await _weightRepo.UpdateDetailAsync(detail);
        }

        public async Task ChangePartnerAsync(int weightId, int newPartnerId, string passwordHash)
        {
            // Same shared password as ChangeDetailProductAsync — a single "manager override"
            // secret gates both actions (see design.md Decision 2).
            if (string.IsNullOrEmpty(_weightSettings.ChangeProductPasswordHash) ||
                !string.Equals(passwordHash, _weightSettings.ChangeProductPasswordHash, StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException("Contraseña incorrecta.");
            }

            WeightEntry entry = await _weightRepo.GetByIdAsync(weightId);

            // Blocked once a Contpaqi document already exists for this entry — deliberately NOT
            // based on ConcludeDate (a concluded-but-not-yet-sent entry may still change partner;
            // see design.md Decision 4).
            if (entry.ConptaqiComercialFK > 0)
            {
                throw new InvalidOperationException("Este proceso ya cuenta con un documento en Contpaqi; no se puede cambiar el socio.");
            }

            // Picking the same partner that's already assigned is a no-op for credit purposes —
            // this entry is already counted in ValidatePartnerCreditAsync's pendingEntriesCost for
            // that partner (it's still tracked under them and not concluded), so re-validating
            // would double-count its own cost and could spuriously reject a change that changes
            // nothing. Still persist below (harmless idempotent write) so the password gate stays
            // consistent regardless of which partner was picked.
            if (newPartnerId != entry.PartnerId)
            {
                // The entry's entire current cost is new exposure for the incoming partner — none
                // of it is counted in their pending-entries total yet, since the entry is still
                // attributed to the old partner until this change is persisted (see design.md
                // Decision 5). (ValidatePartnerCreditAsync fetches the new partner itself and
                // surfaces a not-found error for a bad partner id — no separate lookup needed here.)
                double totalCost = 0;
                foreach (WeightDetail detail in entry.WeightDetails)
                {
                    if (detail.FK_WeightedProductId.HasValue && detail.ProductPrice.HasValue)
                    {
                        double quantity = detail.Weight > 0 ? detail.Weight : (detail.RequiredAmount ?? 0);
                        totalCost += detail.ProductPrice.Value * quantity;
                    }
                }

                CreditValidationResponse creditResult = await ValidatePartnerCreditAsync(newPartnerId, totalCost);
                if (!creditResult.IsValid)
                {
                    throw new InvalidOperationException(creditResult.Message ?? "Crédito insuficiente para el nuevo socio.");
                }
            }

            entry.PartnerId = newPartnerId;

            // force:true bypasses the concluded-entry lock, same as ChangeTargetDocumentBehavior —
            // the ConptaqiComercialFK check above is the real gate for this action.
            await _weightRepo.UpdateAsync(entry, force: true);
        }

        public async Task ChangeDetailAmountAsync(int detailId, double? newWeight, double? newRequiredAmount, string passwordHash)
        {
            // Same shared password as ChangeDetailProductAsync/ChangePartnerAsync — a single
            // "manager override" secret gates all three actions (see design.md Decision 2 of
            // change-weight-detail-product, reused as-is here).
            if (string.IsNullOrEmpty(_weightSettings.ChangeProductPasswordHash) ||
                !string.Equals(passwordHash, _weightSettings.ChangeProductPasswordHash, StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException("Contraseña incorrecta.");
            }

            // Exactly one of the two fields must be supplied — an ambiguous request (both or
            // neither) is rejected rather than silently resolved by priority (see design.md
            // Decision 1).
            if (newWeight.HasValue == newRequiredAmount.HasValue)
            {
                throw new ArgumentException("Debe especificarse exactamente uno: NewWeight o NewRequiredAmount.");
            }

            if (newWeight.HasValue && newWeight.Value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(newWeight), "El peso debe ser mayor que cero.");
            }

            if (newRequiredAmount.HasValue && newRequiredAmount.Value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(newRequiredAmount), "La cantidad requerida debe ser mayor que cero.");
            }

            WeightDetail detail = await _weightRepo.GetDetailByIdAsync(detailId);

            // Blocked once a Contpaqi document already exists for this entry — same rule as
            // ChangePartnerAsync/ChangeDetailProductAsync. Deliberately NOT based on ConcludeDate
            // (see design.md Decision 3).
            if (detail.WeightEntry?.ConptaqiComercialFK > 0)
            {
                throw new InvalidOperationException("Este proceso ya cuenta con un documento en Contpaqi; no se puede cambiar el peso/cantidad.");
            }

            // quantity before the edit: existing convention (Weight wins if captured).
            double oldQuantity = detail.Weight > 0 ? detail.Weight : (detail.RequiredAmount ?? 0);

            // quantity after the edit: if Weight is being changed, the new Weight always wins
            // (same convention). If only RequiredAmount is being changed and the detail already
            // has a captured Weight, the quantity used for cost stays keyed off Weight — this
            // RequiredAmount edit has no effect on cost/credit (documented edge case, see
            // design.md Decision 4).
            double newQuantity = newWeight ?? (detail.Weight > 0 ? detail.Weight : (newRequiredAmount ?? 0));

            double oldCost = (detail.ProductPrice ?? 0) * oldQuantity;
            double newCost = (detail.ProductPrice ?? 0) * newQuantity;

            // Only the incremental exposure needs validating — ValidatePartnerCreditAsync already
            // counts this detail's current cost inside the partner's pending-entries total, so
            // passing the full newCost here would double-count it.
            if (newCost > oldCost)
            {
                int partnerId = detail.WeightEntry?.PartnerId ?? 0;
                CreditValidationResponse creditResult = await ValidatePartnerCreditAsync(partnerId, newCost - oldCost);
                if (!creditResult.IsValid)
                {
                    throw new InvalidOperationException(creditResult.Message ?? "Crédito insuficiente para el nuevo peso/cantidad.");
                }
            }

            if (newWeight.HasValue)
            {
                detail.Weight = newWeight.Value;
            }
            else
            {
                detail.RequiredAmount = newRequiredAmount;
            }

            // Deliberately not checking WeightEntry.ConcludeDate here — the password is the
            // intended override to correct a captured amount after conclusion (see design.md
            // Decision 3).
            await _weightRepo.UpdateDetailAsync(detail);

            // BruteWeight only ever sums Weight, never RequiredAmount, and only for loaded
            // details — mirrors RecordWeightAsync's own recompute guard (see design.md Decision 5).
            if (newWeight.HasValue && detail.IsLoaded)
            {
                await _weightRepo.RecomputeBruteWeightAsync(detail.FK_WeightEntryId);
            }
        }

        public async Task DeleteDetailSafelyAsync(int detailId, string passwordHash)
        {
            // Same shared password as ChangeDetailProductAsync/ChangePartnerAsync/
            // ChangeDetailAmountAsync — a single "manager override" secret gates all four actions
            // (see design.md Decision 2 of change-weight-detail-product, reused as-is here).
            if (string.IsNullOrEmpty(_weightSettings.ChangeProductPasswordHash) ||
                !string.Equals(passwordHash, _weightSettings.ChangeProductPasswordHash, StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException("Contraseña incorrecta.");
            }

            WeightDetail detail = await _weightRepo.GetDetailByIdAsync(detailId);

            // Blocked once a Contpaqi document already exists for this entry — same rule as
            // ChangePartnerAsync/ChangeDetailProductAsync/ChangeDetailAmountAsync. Deliberately
            // NOT based on ConcludeDate (see design.md Decision 2 of delete-weight-detail).
            if (detail.WeightEntry?.ConptaqiComercialFK > 0)
            {
                throw new InvalidOperationException("Este proceso ya cuenta con un documento en Contpaqi; no se puede eliminar el detalle.");
            }

            // Capture before deleting — once IsDeleted=true the detail is no longer a reliable
            // source for these (see design.md Decision 5 of delete-weight-detail).
            bool wasLoaded = detail.IsLoaded;
            int entryId = detail.FK_WeightEntryId;

            // No credit re-validation here: removing a detail only ever decreases the parent
            // entry's cost exposure (see design.md Decision 4 of delete-weight-detail).
            await _weightRepo.DeleteDetailAsync(detailId);

            // BruteWeight only ever sums Weight for IsLoaded details — a never-loaded detail was
            // never counted, so skip the recompute entirely for it. Must run after the delete
            // above so the recompute's own query excludes the just-deleted detail.
            if (wasLoaded)
            {
                await _weightRepo.RecomputeBruteWeightAsync(entryId);
            }
        }
    }
}
