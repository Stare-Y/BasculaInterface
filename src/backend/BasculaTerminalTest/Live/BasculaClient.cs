using Microsoft.AspNetCore.SignalR.Client;

namespace BasculaTerminalTest.Live
{
    /// <summary>
    /// Live smoke check against a running BasculaTerminalApi with a real (or simulated) scale
    /// attached. Requires the API listening on localhost:5284 and weight events actually flowing —
    /// it is NOT run in CI or by a normal <c>dotnet test</c>. Run it explicitly with
    /// <c>dotnet test --filter "Category=Live"</c> when validating hardware/serial wiring.
    /// </summary>
    public class BasculaClient
    {
        private HubConnection _hubConnection = null!;

        [Fact]
        [Trait("Category", "Live")]
        public async Task WebSocketTesting()
        {
            _hubConnection = new HubConnectionBuilder()
                .WithUrl("http://localhost:5284/basculaSocket")
                .Build();

            var receivedNumbers = new List<double>();

            // Must match SerialPortHub.SendWeightNumber, which broadcasts "ReceiveLecture"
            // (and BasculaViewModel listens for the same). The old "ReceiveNumber" name never
            // fired, so this test silently passed as long as it timed out first.
            _hubConnection.On<double>("ReceiveLecture", (number) =>
            {
                receivedNumbers.Add(number);
                Console.WriteLine($"Received Number: {number}");
            });

            await _hubConnection.StartAsync();

            await Task.Delay(15000);

            await _hubConnection.StopAsync();

            Assert.NotEmpty(receivedNumbers);
            foreach (var number in receivedNumbers)
            {
                Assert.InRange(number, 1, 100);
            }
        }
    }
}
