using Microsoft.AspNetCore.SignalR.Client;

namespace BasculaTerminalTest
{
    public class BasculaClient
    {
        private HubConnection _hubConnection = null!;
        [Fact]
        public async Task WebSocketTesting()
        {
            _hubConnection = new HubConnectionBuilder()
                .WithUrl("http://localhost:5284/basculaSocket")
                .Build();

            var receivedNumbers = new List<double>();

            _hubConnection.On<double>("ReceiveNumber", (number) =>
            {
                receivedNumbers.Add(number);
                Console.WriteLine($"Received Number: {number}");
            });

            await _hubConnection.StartAsync();

            await Task.Delay(15000);

            await _hubConnection.StopAsync();

            Assert.NotEmpty(receivedNumbers);
            foreach(var number in receivedNumbers)
            {
                Assert.InRange(number, 1, 100);
            }
        }
            
        private record NumberPayload(double Number);
    }
}