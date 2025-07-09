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
                await _printService.Print(ticket);

                Debug.WriteLine($"Ticket Printed: {ticket}");

                return Ok("Ticket sent to print");
            }
            catch (Exception ex)
            {
                return BadRequest($"Error printing ticket: {ex.Message}");
            }
        }
    }
}
