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
        public PrintController(IPrintService printService)
        {
            _printService = printService;
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
                return BadRequest($"Error printing ticket: {ex.Message}");
            }
        }
    }
}
