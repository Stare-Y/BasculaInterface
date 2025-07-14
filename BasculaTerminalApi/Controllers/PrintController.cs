using System.Diagnostics;
using BasculaTerminalApi.Service;
using Microsoft.AspNetCore.Mvc;

namespace BasculaTerminalApi.Controllers
{
    [ApiController]
    [Route("api/print")]
    public class PrintController : ControllerBase
    {
        private readonly PrintService _printService;
        public PrintController(PrintService printService)
        {
            _printService = printService;
        }

        [HttpPost]
        public async Task<IActionResult> Print([FromBody] string ticket)
        {
            try
            {
#pragma warning disable CA1416 // Validar la compatibilidad de la plataforma
                await _printService.Print(ticket);
#pragma warning restore CA1416 // Validar la compatibilidad de la plataforma

                Debug.WriteLine($"Ticket Printed: {ticket}");

                return Ok("Ticket impreso xd");
            }
            catch (Exception ex)
            {
                return BadRequest($"Error printing ticket: {ex.Message}");
            }
        }
    }
}
