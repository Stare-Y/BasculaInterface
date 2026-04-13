using Core.Application.DTOs;
using Core.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace BasculaTerminalApi.Controllers
{
    [ApiController]
    [Route("api/[Controller]")]
    public class ProviderPurchaseController : ControllerBase
    {
        private readonly IProviderPurchaseService _providerPurchaseService;
        private readonly ILogger<ProviderPurchaseController> _logger;

        public ProviderPurchaseController(IProviderPurchaseService providerPurchaseService, ILogger<ProviderPurchaseController> logger)
        {
            _providerPurchaseService = providerPurchaseService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<ActionResult<ProviderPurchaseDto>> Create([FromBody] ProviderPurchaseDto dto)
        {
            try
            {
                return Ok(await _providerPurchaseService.CreateAsync(dto));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating provider purchase");
                return BadRequest(new GenericResponse<string> { Message = $"Error creating entry: {ex.Message}" });
            }
        }

        [HttpGet("ById")]
        public async Task<ActionResult<ProviderPurchaseDto>> GetById([FromQuery] int id)
        {
            try
            {
                return Ok(await _providerPurchaseService.GetByIdAsync(id));
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Provider purchase with ID {Id} not found", id);
                return NotFound(new GenericResponse<string> { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving provider purchase with ID {Id}", id);
                return BadRequest(new GenericResponse<string> { Message = $"Error retrieving entry: {ex.Message}" });
            }
        }

        [HttpGet("All")]
        public async Task<ActionResult<IEnumerable<ProviderPurchaseDto>>> GetAll([FromQuery] int top = 30, [FromQuery] uint page = 1)
        {
            try
            {
                return Ok(await _providerPurchaseService.GetAllAsync(top, page));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving provider purchases");
                return BadRequest(new GenericResponse<string> { Message = $"Error retrieving entries: {ex.Message}" });
            }
        }

        [HttpGet("All/ByProvider")]
        public async Task<ActionResult<IEnumerable<ProviderPurchaseDto>>> GetByProviderId([FromQuery] int providerId, [FromQuery] int top = 30, [FromQuery] uint page = 1)
        {
            try
            {
                return Ok(await _providerPurchaseService.GetByProviderIdAsync(providerId, top, page));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving provider purchases for provider {ProviderId}", providerId);
                return BadRequest(new GenericResponse<string> { Message = $"Error retrieving entries: {ex.Message}" });
            }
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] ProviderPurchaseDto dto)
        {
            try
            {
                await _providerPurchaseService.UpdateAsync(dto);
                return Ok(new GenericResponse<string> { Data = "Updated", Message = "Success" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating provider purchase with ID {Id}", dto.Id);
                return BadRequest(new GenericResponse<string> { Message = $"Error updating entry: {ex.Message}" });
            }
        }

        [HttpPost("CreateWeightEntry")]
        public async Task<ActionResult<WeightEntryDto>> CreateWeightEntry([FromQuery] int purchaseId)
        {
            try
            {
                return Ok(await _providerPurchaseService.CreateWeightEntryAsync(purchaseId));
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Provider purchase with ID {Id} not found", purchaseId);
                return NotFound(new GenericResponse<string> { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Cannot create weight entry for provider purchase {Id}", purchaseId);
                return BadRequest(new GenericResponse<string> { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating weight entry for provider purchase {Id}", purchaseId);
                return BadRequest(new GenericResponse<string> { Message = $"Error creating weight entry: {ex.Message}" });
            }
        }

        [HttpDelete]
        public async Task<IActionResult> Delete([FromQuery] int id)
        {
            try
            {
                bool deleted = await _providerPurchaseService.DeleteAsync(id);
                if (!deleted)
                {
                    return NotFound($"Provider purchase with ID {id} not found.");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting provider purchase with ID {Id}", id);
                return BadRequest($"Error deleting provider purchase: {ex.Message}");
            }
        }
    }
}
