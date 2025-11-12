using Core.Application.DTOs;
using Core.Application.Services;
using Microsoft.AspNetCore.Mvc;
using static iText.StyledXmlParser.Jsoup.Select.Evaluator;

namespace BasculaTerminalApi.Controllers
{
    [ApiController]
    [Route("api/[Controller]")]
    public class WeightController : ControllerBase
    {
        private readonly IWeightService _weightService = null!;
        private readonly IWeightLogisticService _weightLogisticService = null!;
        private readonly ILogger<WeightController> _logger;
        public WeightController(IWeightService weightService, IWeightLogisticService weightLogisticService, ILogger<WeightController> logger)
        {
            _weightService = weightService;
            _weightLogisticService = weightLogisticService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<ActionResult<WeightEntryDto>> Create([FromBody] WeightEntryDto weightEntryDto)
        {
            return Ok(await _weightService.CreateAsync(weightEntryDto));
        }

        [HttpGet("ById")]
        public async Task<ActionResult<WeightEntryDto>> GetById([FromQuery] int id)
        {
            return Ok(await _weightService.GetByIdAsync(id));
        }

        [HttpGet("Pending")]
        public async Task<ActionResult<IEnumerable<WeightEntryDto>>> GetPendingWeights([FromQuery] int top = 30, [FromQuery] uint page = 1)
        {
            return Ok(await _weightService.GetPendingWeights(top, page));
        }

        [HttpGet("All")]
        public async Task<ActionResult<IEnumerable<WeightEntryDto>>> GetAll([FromQuery] int top = 30, [FromQuery] uint page = 1)
        {
            return Ok(await _weightService.GetAllAsync(top, page));
        }

        [HttpGet("All/ByPartner")]
        public async Task<ActionResult<IEnumerable<WeightEntryDto>>> GetAllByPartner([FromQuery] int partnerId, [FromQuery] int top = 30, [FromQuery] uint page = 1)
        {
            return Ok(await _weightService.GetAllByPartnerAsync(partnerId, top, page));
        }

        [HttpGet("All/ByDateRange")]
        public async Task<ActionResult<IEnumerable<WeightEntryDto>>> GetAllByDateRange([FromBody] GetByDateRangeCommand command, [FromQuery] int top = 30, [FromQuery] uint page = 1)
        {
            return Ok(await _weightService.GetByDateRange(command.startDate, command.endDate, top, page));
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] WeightEntryDto weightEntryDto)
        {
            await _weightService.UpdateAsync(weightEntryDto);

            return Ok("Update Successful :D");
        }

        [HttpDelete]
        public async Task<IActionResult> Delete([FromQuery] int id)
        {
            try
            {
                bool deleted = await _weightService.DeleteAsync(id);
                if (!deleted)
                {
                    return NotFound($"Weight entry with ID {id} not found.");
                }

                return NoContent();
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
    }
}
