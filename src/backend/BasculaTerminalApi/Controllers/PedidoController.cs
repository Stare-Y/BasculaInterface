using Core.Application.DTOs;
using Core.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BasculaTerminalApi.Controllers
{
    public record ConvertLineToWeightRequest(int? WeightEntryId, decimal? TargetAmount, string? ExternalTarget);
    public record DeletePedidoRequest(string GateIdentifier, string GatePassword)
    {
        public GateCredential ToGateCredential() => new(GateIdentifier, GatePassword);
    }
    public record DeletePedidoLineRequest(string GateIdentifier, string GatePassword)
    {
        public GateCredential ToGateCredential() => new(GateIdentifier, GatePassword);
    }

    [ApiController]
    [Route("api/[Controller]")]
    public class PedidoController : ControllerBase
    {
        private readonly IPedidoService _pedidoService;
        private readonly ILogger<PedidoController> _logger;

        public PedidoController(IPedidoService pedidoService, ILogger<PedidoController> logger)
        {
            _pedidoService = pedidoService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<ActionResult<PedidoDto>> Create([FromBody] PedidoDto dto)
        {
            try
            {
                return Ok(await _pedidoService.CreateAsync(dto));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating pedido");
                return BadRequest(new GenericResponse<string> { Message = $"Error creating entry: {ex.Message}" });
            }
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<PedidoDto>> GetById(int id)
        {
            try
            {
                return Ok(await _pedidoService.GetByIdAsync(id));
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Pedido with ID {Id} not found", id);
                return NotFound(new GenericResponse<string> { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving pedido with ID {Id}", id);
                return BadRequest(new GenericResponse<string> { Message = $"Error retrieving entry: {ex.Message}" });
            }
        }

        [HttpGet("All")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<PedidoDto>>> GetAll([FromQuery] int top = 30, [FromQuery] uint page = 1)
        {
            try
            {
                return Ok(await _pedidoService.GetAllAsync(top, page));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving pedidos");
                return BadRequest(new GenericResponse<string> { Message = $"Error retrieving entries: {ex.Message}" });
            }
        }

        [HttpGet("All/ByProvider")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<PedidoDto>>> GetByProviderId([FromQuery] int providerId, [FromQuery] int top = 30, [FromQuery] uint page = 1)
        {
            try
            {
                return Ok(await _pedidoService.GetByProviderIdAsync(providerId, top, page));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving pedidos for provider {ProviderId}", providerId);
                return BadRequest(new GenericResponse<string> { Message = $"Error retrieving entries: {ex.Message}" });
            }
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] PedidoDto dto)
        {
            try
            {
                await _pedidoService.UpdateAsync(dto);
                return Ok(new GenericResponse<string> { Data = "Updated", Message = "Success" });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Pedido with ID {Id} not found", dto.Id);
                return NotFound(new GenericResponse<string> { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating pedido with ID {Id}", dto.Id);
                return BadRequest(new GenericResponse<string> { Message = $"Error updating entry: {ex.Message}" });
            }
        }

        [HttpPatch("{id}/Delete")]
        public async Task<IActionResult> DeleteSafely(int id, [FromBody] DeletePedidoRequest request)
        {
            try
            {
                bool deleted = await _pedidoService.DeleteSafelyAsync(id, request.ToGateCredential());
                if (!deleted)
                {
                    return NotFound(new GenericResponse<string> { Message = $"Pedido with ID {id} not found." });
                }

                return Ok(new GenericResponse<string> { Data = "Deleted", Message = "Success" });
            }
            catch (UnauthorizedAccessException)
            {
                return BadRequest(new GenericResponse<string> { Message = "Credenciales inválidas o sin autorización." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting pedido with ID {Id}", id);
                return BadRequest(new GenericResponse<string> { Message = $"Error deleting pedido: {ex.Message}" });
            }
        }

        [HttpPost("Line")]
        public async Task<ActionResult<PedidoLineDto>> CreateLine([FromBody] PedidoLineDto dto)
        {
            try
            {
                return Ok(await _pedidoService.CreateLineAsync(dto));
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Pedido with ID {Id} not found for new line", dto.PedidoId);
                return NotFound(new GenericResponse<string> { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating pedido line");
                return BadRequest(new GenericResponse<string> { Message = $"Error creating line: {ex.Message}" });
            }
        }

        [HttpPut("Line")]
        public async Task<IActionResult> UpdateLine([FromBody] PedidoLineDto dto)
        {
            try
            {
                await _pedidoService.UpdateLineAsync(dto);
                return Ok(new GenericResponse<string> { Data = "Updated", Message = "Success" });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "PedidoLine with ID {Id} not found", dto.Id);
                return NotFound(new GenericResponse<string> { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating pedido line with ID {Id}", dto.Id);
                return BadRequest(new GenericResponse<string> { Message = $"Error updating line: {ex.Message}" });
            }
        }

        [HttpPatch("Line/{id}/Delete")]
        public async Task<IActionResult> DeleteLineSafely(int id, [FromBody] DeletePedidoLineRequest request)
        {
            try
            {
                bool deleted = await _pedidoService.DeleteLineSafelyAsync(id, request.ToGateCredential());
                if (!deleted)
                {
                    return NotFound(new GenericResponse<string> { Message = $"PedidoLine with ID {id} not found." });
                }

                return Ok(new GenericResponse<string> { Data = "Deleted", Message = "Success" });
            }
            catch (UnauthorizedAccessException)
            {
                return BadRequest(new GenericResponse<string> { Message = "Credenciales inválidas o sin autorización." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting pedido line with ID {Id}", id);
                return BadRequest(new GenericResponse<string> { Message = $"Error deleting line: {ex.Message}" });
            }
        }

        [HttpPatch("Line/{id}/Close")]
        public async Task<IActionResult> CloseLine(int id)
        {
            try
            {
                await _pedidoService.CloseLineAsync(id);
                return Ok(new GenericResponse<string> { Data = "Closed", Message = "Success" });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "PedidoLine with ID {Id} not found", id);
                return NotFound(new GenericResponse<string> { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error closing pedido line with ID {Id}", id);
                return BadRequest(new GenericResponse<string> { Message = $"Error closing line: {ex.Message}" });
            }
        }

        [HttpPost("Line/{id}/ConvertToWeight")]
        public async Task<ActionResult<WeightEntryDto>> ConvertLineToWeight(int id, [FromBody] ConvertLineToWeightRequest request)
        {
            try
            {
                return Ok(await _pedidoService.ConvertLineToWeightAsync(id, request.WeightEntryId, request.TargetAmount, request.ExternalTarget));
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "PedidoLine or related entity with ID {Id} not found", id);
                return NotFound(new GenericResponse<string> { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Cannot convert pedido line {Id} to weight", id);
                return BadRequest(new GenericResponse<string> { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting pedido line {Id} to weight", id);
                return BadRequest(new GenericResponse<string> { Message = $"Error converting line to weight: {ex.Message}" });
            }
        }
    }
}
