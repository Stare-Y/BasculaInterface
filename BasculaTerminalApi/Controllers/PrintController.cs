using Core.Application.DTOs;
using Core.Application.Services;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace BasculaTerminalApi.Controllers
{
    [ApiController]
    [Route("api/[Controller]")]
    public class PrintController : ControllerBase
    {
        private readonly IPrintService _printService;
        private readonly ILogger<PrintController> _logger;
        public PrintController(IPrintService printService, ILogger<PrintController> logger)
        {
            _printService = printService;
            _logger = logger;
        }

        [HttpPost("Text")]
        public IActionResult Print([FromBody] string ticket)
        {
            try
            {
                _printService.Print(ticket);

                _logger.LogInformation("Plain Text printed successfuly.");

                return Accepted();
            }
            catch (Exception ex)
            {
                return BadRequest($"Error printing ticket: {ex.Message}");
            }
        }

        [HttpPost("WeightEntry")]
        public async Task<IActionResult> Print([FromBody] WeightEntryDto weightEntry)
        {
            try
            {
                await _printService.Print(weightEntry);

                _logger.LogInformation("WeightEntry printed successfuly.");

                return Accepted();
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "Error printing ticket with {ticket}", ticket);
                return BadRequest($"Error printing ticket: {ex.Message}");
            }
        }
    }
}
