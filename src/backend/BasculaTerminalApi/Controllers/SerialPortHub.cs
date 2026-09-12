using Core.Application.Services;
using Core.Domain.Events;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace BasculaTerminalApi.Controllers
{
    // Deliberately no auth of any kind (design.md Decision 3 / Non-goals of
    // add-user-authentication-and-audit-log) — the bascula websocket must stay fully open.
    [AllowAnonymous]
    public class SerialPortHub : Hub
    {
        private readonly IHubContext<SerialPortHub> _context = null!;

        public SerialPortHub(IHubContext<SerialPortHub> context, IBasculaService basculaService)
        {
            _context = context;

            basculaService.OnBasculaRead += SendWeightNumber;
        }

        private async void SendWeightNumber(object?sender, OnBasculaReadEventArgs e)
        {
            double number = e?.Weight ?? throw new Exception("Error leyendo evento de lectura de peso");

            await _context.Clients.All.SendAsync("ReceiveLecture", number);
        }
    }
}
