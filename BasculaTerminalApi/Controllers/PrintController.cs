using Core.Application.Services;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using static iText.StyledXmlParser.Jsoup.Select.Evaluator;

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

        [HttpPost]
        public IActionResult Print([FromBody] string ticket)
        {
            try
            {
                _printService.Print(ticket);

                Debug.WriteLine($"Ticket Printed: {ticket}");

                return Accepted("Ticket impreso");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error printing ticket with {ticket}", ticket);
                return BadRequest($"Error printing ticket: {ex.Message}");
            }
        }
    }
}
