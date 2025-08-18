using BasculaTerminalApi.Events;
using BasculaTerminalApi.Service;
using Microsoft.AspNetCore.SignalR;

namespace BasculaTerminalApi.Controllers
{
    public class SerialPortHub : Hub
    {
        private readonly IHubContext<SerialPortHub> _context = null!;

        public SerialPortHub(IHubContext<SerialPortHub> context, BasculaService basculaService)
        {
            _context = context;

            basculaService.OnBasculaRead += SendWeightNumber;
        }

        private async void SendWeightNumber(object sender, OnBasculaReadEventArgs e)
        {
            double number = e?.Weight ?? throw new Exception("Error leyendo evento de lectura de peso");

            await _context.Clients.All.SendAsync("ReceiveLecture", number);
        }
    }
}
