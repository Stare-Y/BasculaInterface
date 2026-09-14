using Core.Application.DTOs;
using Core.Application.DTOs.ContpaqiComercial;
using Core.Application.Services;
using Core.Domain.Entities.Weight;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BasculaTerminalApi.Controllers
{
    public record SetSecondaryTareRequest(double SecondaryTare);
    public record RecordWeightRequest(double Weight, string WeightedBy);
    public record ChangeDetailProductRequest(int NewProductId, string GateIdentifier, string GatePassword)
    {
        public GateCredential ToGateCredential() => new(GateIdentifier, GatePassword);
    }
    public record ChangePartnerRequest(int NewPartnerId, string GateIdentifier, string GatePassword)
    {
        public GateCredential ToGateCredential() => new(GateIdentifier, GatePassword);
    }
    public record ChangeDetailAmountRequest(double? NewWeight, double? NewRequiredAmount, string GateIdentifier, string GatePassword)
    {
        public GateCredential ToGateCredential() => new(GateIdentifier, GatePassword);
    }
    public record DeleteDetailRequest(string GateIdentifier, string GatePassword)
    {
        public GateCredential ToGateCredential() => new(GateIdentifier, GatePassword);
    }
    public record DeleteWeightEntryRequest(string GateIdentifier, string GatePassword)
    {
        public GateCredential ToGateCredential() => new(GateIdentifier, GatePassword);
    }
    public record AuthorizeTurnBypassRequest(string GateIdentifier, string GatePassword);

    [ApiController]
    [Route("api/[Controller]")]
    public class WeightController : ControllerBase
    {
        private readonly IWeightService _weightService = null!;
        private readonly IWeightLogisticService _weightLogisticService = null!;
        private readonly IGateAuthorizationService _gateAuthorizationService;
        private readonly ILogger<WeightController> _logger;
        public WeightController(
            IWeightService weightService,
            IWeightLogisticService weightLogisticService,
            IGateAuthorizationService gateAuthorizationService,
            ILogger<WeightController> logger)
        {
            _weightService = weightService;
            _weightLogisticService = weightLogisticService;
            _gateAuthorizationService = gateAuthorizationService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<ActionResult<WeightEntryDto>> Create([FromBody] WeightEntryDto weightEntryDto)
        {
            return Ok(await _weightService.CreateAsync(weightEntryDto));
        }

        [HttpGet("ById")]
        [AllowAnonymous]
        public async Task<ActionResult<WeightEntryDto>> GetById([FromQuery] int id)
        {
            return Ok(await _weightService.GetByIdAsync(id));
        }

        [HttpGet("Pending")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<WeightEntryDto>>> GetPendingWeights([FromQuery] int top = 30, [FromQuery] uint page = 1)
        {
            return Ok(await _weightService.GetPendingWeights(top, page));
        }

        [HttpGet("All")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<WeightEntryDto>>> GetAll([FromQuery] int top = 30, [FromQuery] uint page = 1)
        {
            return Ok(await _weightService.GetAllAsync(top, page));
        }

        [HttpGet("All/Completed")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<WeightEntryDto>>> GetAllComplete([FromQuery] int top = 30, [FromQuery] uint page = 1)
        {
            return Ok(await _weightService.GetAllComplete(top, page));
        }

        [HttpGet("All/ByPartner")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<WeightEntryDto>>> GetAllByPartner([FromQuery] int partnerId, [FromQuery] int top = 30, [FromQuery] uint page = 1)
        {
            return Ok(await _weightService.GetAllByPartnerAsync(partnerId, top, page));
        }

        [HttpGet("All/ByDateRange")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<WeightEntryDto>>> GetAllByDateRange([FromBody] GetByDateRangeCommand command, [FromQuery] int top = 30, [FromQuery] uint page = 1)
        {
            return Ok(await _weightService.GetByDateRange(command.startDate, command.endDate, top, page));
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] WeightEntryDto weightEntryDto)
        {
            try
            {
                await _weightService.UpdateAsync(weightEntryDto);

                return Ok(new GenericResponse<string> { Data = "Updated", Message = "Success"});
            }
            catch (Exception ex)
            {
                return BadRequest(new GenericResponse<string> { Message = $"Error updating entry: {ex.Message}" });
            }

        }

        [HttpPost("Detail")]
        public async Task<ActionResult<WeightDetailDto>> CreateDetail([FromBody] WeightDetailDto detailDto)
        {
            try
            {
                WeightDetailDto created = await _weightService.CreateDetailAsync(detailDto);
                return CreatedAtAction(nameof(GetById), new { id = created.FK_WeightEntryId }, created);
            }
            catch (Exception ex)
            {
                return BadRequest(new GenericResponse<string> { Message = $"Error creating detail: {ex.Message}" });
            }
        }

        [HttpPut("Detail/{id}/SecondaryTare")]
        public async Task<IActionResult> SetSecondaryTare(int id, [FromBody] SetSecondaryTareRequest request)
        {
            try
            {
                await _weightService.SetSecondaryTareAsync(id, request.SecondaryTare);
                return Ok(new GenericResponse<string> { Data = "Updated", Message = "Success" });
            }
            catch (WeightConcurrencyException)
            {
                return Conflict(new GenericResponse<string> { Message = "El registro fue modificado por otro terminal. Intente de nuevo." });
            }
            catch (Exception ex)
            {
                return BadRequest(new GenericResponse<string> { Message = ex.Message });
            }
        }

        [HttpPut("Detail/{id}/Weight")]
        public async Task<IActionResult> RecordWeight(int id, [FromBody] RecordWeightRequest request)
        {
            try
            {
                await _weightService.RecordWeightAsync(id, request.Weight, request.WeightedBy);
                return Ok(new GenericResponse<string> { Data = "Updated", Message = "Success" });
            }
            catch (WeightConcurrencyException)
            {
                return Conflict(new GenericResponse<string> { Message = "El registro fue modificado por otro terminal. Intente de nuevo." });
            }
            catch (Exception ex)
            {
                return BadRequest(new GenericResponse<string> { Message = ex.Message });
            }
        }

        [HttpPut("Detail/{id}/MarkLoaded")]
        public async Task<ActionResult<WeightEntryDto>> MarkDetailLoaded(int id)
        {
            try
            {
                WeightEntryDto updated = await _weightService.MarkDetailLoadedAsync(id);
                return Ok(updated);
            }
            catch (WeightConcurrencyException)
            {
                return Conflict(new GenericResponse<string> { Message = "El registro fue modificado por otro terminal. Intente de nuevo." });
            }
            catch (Exception ex)
            {
                return BadRequest(new GenericResponse<string> { Message = ex.Message });
            }
        }

        [HttpPut("{id}/Conclude")]
        public async Task<IActionResult> ConcludeWeightEntry(int id)
        {
            try
            {
                await _weightService.ConcludeAsync(id);
                return Ok(new GenericResponse<string> { Data = "Concluded", Message = "Success" });
            }
            catch (WeightConcurrencyException)
            {
                return Conflict(new GenericResponse<string> { Message = "El registro fue modificado por otro terminal. Intente de nuevo." });
            }
            catch (Exception ex)
            {
                return BadRequest(new GenericResponse<string> { Message = ex.Message });
            }
        }

        [HttpPatch("{id}/Delete")]
        public async Task<IActionResult> DeleteSafely(int id, [FromBody] DeleteWeightEntryRequest request)
        {
            try
            {
                await _weightService.DeleteSafelyAsync(id, request.ToGateCredential());
                return Ok(new GenericResponse<string> { Data = "Deleted", Message = "Success" });
            }
            catch (KeyNotFoundException)
            {
                return NotFound($"Weight entry with ID {id} not found.");
            }
            catch (UnauthorizedAccessException)
            {
                return BadRequest(new GenericResponse<string> { Message = "Credenciales inválidas o sin autorización." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting weight entry with ID {Id}", id);
                return BadRequest($"Error deleting weight entry: {ex.Message}");
            }
        }

        [HttpDelete("Detail")]
        public async Task<IActionResult> DeleteDetail([FromQuery] int id)
        {
            try
            {

                bool deleted = await _weightService.DeleteDetailAsync(id);
                if (!deleted)
                {
                    return NotFound($"Weight detail with ID {id} not found.");
                }
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting detail entry with ID {Id}", id);
                return BadRequest($"Error deleting weight detail: {ex.Message}");
            }
        }

        [HttpPut("CanWeight")]
        public async Task<ActionResult<bool>> RequestWeight([FromQuery] string deviceId)
        {
            try
            {
                return Ok(await _weightLogisticService.RequestWeight(deviceId));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting weight with ID {Id}", deviceId);
                return BadRequest($"Error requesting weight: {ex.Message}");
            }
        }

        [HttpPut("ReleaseWeight")]
        public async Task<ActionResult<bool>> ReleaseWeight([FromQuery] string deviceId)
        {
            try
            {
                return Ok(await _weightLogisticService.ReleaseWeight(deviceId));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error releasing weight with ID {Id}", deviceId);
                return BadRequest($"Error releasing weight: {ex.Message}");
            }
        }

        /// <summary>Verify-only endpoint backing the device-local BypasTurn setting's per-use gate
        /// (role-driven-terminal-modes design.md Decision 3) — reuses the same
        /// IGateAuthorizationService resolution/verification/permission check as every other
        /// self-authorize-gated action, but performs no mutation itself regardless of the
        /// result.</summary>
        [HttpPost("AuthorizeTurnBypass")]
        public async Task<ActionResult<bool>> AuthorizeTurnBypass([FromBody] AuthorizeTurnBypassRequest request)
        {
            return Ok(await _gateAuthorizationService.TryAuthorizeAsync(request.GateIdentifier, request.GatePassword));
        }

        [HttpPost("ContpaqiComercial")]
        public async Task<IActionResult> SendToContpaqiComercial([FromQuery] int weightId)
        {
            try
            {
                if(weightId <= 0)
                {
                    return BadRequest("Invalid weight ID");
                }

                GenericResponse<ContpaqiComercialResult> fKResult = await _weightService.SendToContpaqiComercial(weightId);

                return Ok(fKResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending document to contpaqi comercial");
                return BadRequest("Error sending document to contpaqi comercial" + ex.Message);
            }
        }

        [HttpPatch("{weightId}/ChangeTargetDocumentBehavior")]
        public async Task<IActionResult> ChangeTargetDocumentBehavior(int weightId, [FromQuery] int newTargetId)
        {
            try
            {
                if (weightId == 0)
                    return BadRequest("Invalid weight ID");

                if (newTargetId == 0)
                    return BadRequest("invalid TargetBehavior ID");

                await _weightService.ChangeTargetDocumentBehavior(weightId, newTargetId);

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating target behavior");
                return BadRequest("Error updating target behavior" + ex.Message);
            }
        }

        [HttpGet("ValidateCredit")]
        [AllowAnonymous]
        public async Task<ActionResult<CreditValidationResponse>> ValidatePartnerCredit(
            [FromQuery] int partnerId,
            [FromQuery] double requestedAmount)
        {
            try
            {
                if (partnerId <= 0)
                {
                    return BadRequest(new CreditValidationResponse
                    {
                        IsValid = false,
                        Message = "ID de socio inválido."
                    });
                }

                if (requestedAmount < 0)
                {
                    return BadRequest(new CreditValidationResponse
                    {
                        IsValid = false,
                        Message = "El monto solicitado no puede ser negativo."
                    });
                }

                CreditValidationResponse result = await _weightService.ValidatePartnerCreditAsync(partnerId, requestedAmount);

                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Partner with ID {PartnerId} not found", partnerId);
                return NotFound(new CreditValidationResponse
                {
                    IsValid = false,
                    Message = $"Socio con ID {partnerId} no encontrado."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating credit for partner {PartnerId}", partnerId);
                return BadRequest(new CreditValidationResponse
                {
                    IsValid = false,
                    Message = $"Error validando crédito: {ex.Message}"
                });
            }
        }

        [HttpPatch("Detail/{id}/Product")]
        public async Task<IActionResult> ChangeDetailProduct(int id, [FromBody] ChangeDetailProductRequest request)
        {
            try
            {
                await _weightService.ChangeDetailProductAsync(id, request.NewProductId, request.ToGateCredential());
                return Ok(new GenericResponse<string> { Data = "Updated", Message = "Success" });
            }
            catch (WeightConcurrencyException)
            {
                return Conflict(new GenericResponse<string> { Message = "El registro fue modificado por otro terminal. Intente de nuevo." });
            }
            catch (UnauthorizedAccessException)
            {
                return BadRequest(new GenericResponse<string> { Message = "Credenciales inválidas o sin autorización." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing product for weight detail {Id}", id);
                return BadRequest(new GenericResponse<string> { Message = ex.Message });
            }
        }

        [HttpPatch("{id}/Partner")]
        public async Task<IActionResult> ChangePartner(int id, [FromBody] ChangePartnerRequest request)
        {
            try
            {
                await _weightService.ChangePartnerAsync(id, request.NewPartnerId, request.ToGateCredential());
                return Ok(new GenericResponse<string> { Data = "Updated", Message = "Success" });
            }
            catch (WeightConcurrencyException)
            {
                return Conflict(new GenericResponse<string> { Message = "El registro fue modificado por otro terminal. Intente de nuevo." });
            }
            catch (UnauthorizedAccessException)
            {
                return BadRequest(new GenericResponse<string> { Message = "Credenciales inválidas o sin autorización." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing partner for weight entry {Id}", id);
                return BadRequest(new GenericResponse<string> { Message = ex.Message });
            }
        }

        [HttpPatch("Detail/{id}/Amount")]
        public async Task<IActionResult> ChangeDetailAmount(int id, [FromBody] ChangeDetailAmountRequest request)
        {
            try
            {
                await _weightService.ChangeDetailAmountAsync(id, request.NewWeight, request.NewRequiredAmount, request.ToGateCredential());
                return Ok(new GenericResponse<string> { Data = "Updated", Message = "Success" });
            }
            catch (WeightConcurrencyException)
            {
                return Conflict(new GenericResponse<string> { Message = "El registro fue modificado por otro terminal. Intente de nuevo." });
            }
            catch (UnauthorizedAccessException)
            {
                return BadRequest(new GenericResponse<string> { Message = "Credenciales inválidas o sin autorización." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing amount for weight detail {Id}", id);
                return BadRequest(new GenericResponse<string> { Message = ex.Message });
            }
        }

        [HttpPatch("Detail/{id}/Delete")]
        public async Task<IActionResult> DeleteDetailSafely(int id, [FromBody] DeleteDetailRequest request)
        {
            try
            {
                await _weightService.DeleteDetailSafelyAsync(id, request.ToGateCredential());
                return Ok(new GenericResponse<string> { Data = "Deleted", Message = "Success" });
            }
            catch (WeightConcurrencyException)
            {
                return Conflict(new GenericResponse<string> { Message = "El registro fue modificado por otro terminal. Intente de nuevo." });
            }
            catch (UnauthorizedAccessException)
            {
                return BadRequest(new GenericResponse<string> { Message = "Credenciales inválidas o sin autorización." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting detail entry with ID {Id}", id);
                return BadRequest(new GenericResponse<string> { Message = ex.Message });
            }
        }

        [HttpGet("{id}/Radiography")]
        public async Task<ActionResult<WeightEntryRadiographyDto>> GetRadiography(int id, [FromServices] IAuditLogService auditLogService)
        {
            try
            {
                return Ok(await auditLogService.GetWeightEntryRadiographyAsync(id));
            }
            catch (KeyNotFoundException)
            {
                return NotFound($"Weight entry with ID {id} not found.");
            }
        }
    }
}
