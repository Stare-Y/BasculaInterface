using BasculaTerminalApi.Events;
using BasculaTerminalApi.Service;
using Microsoft.AspNetCore.SignalR;

namespace BasculaTerminalApi.Controllers
{
    public class SerialPortHub : Hub
    {
        private static IHubContext<SerialPortHub> _context = null!;

        public SerialPortHub(IHubContext<SerialPortHub> context, BasculaService basculaService)
        {
            _context = context;

            basculaService.OnBasculaRead += SendWeightNumber;
        }
        private async void SendWeightNumber(object sender, OnBasculaReadEventArgs e)
        {
            var number = e?.Weight ?? throw new Exception("Error leyendo evento de lectura de peso");
            await _context.Clients.All.SendAsync("ReceiveNumber", number);

            if (number > 0)
            {
                Console.WriteLine($"Peso: {number}");
            }
            else
            {
                Console.WriteLine("Lectura inválida o error en la báscula.");
            }   
        }
    }
}
