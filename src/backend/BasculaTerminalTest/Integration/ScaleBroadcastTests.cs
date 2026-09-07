using System.Diagnostics;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;

namespace BasculaTerminalTest.Integration
{
    /// <summary>
    /// End-to-end path a scale reading actually travels: <c>IBasculaService.OnBasculaRead</c> →
    /// <c>SerialPortHub</c> → SignalR <c>"ReceiveLecture"</c> → connected client. The physical
    /// serial port is replaced by <see cref="TestDoubles.FakeBasculaService"/>; everything else
    /// (hub, SignalR pipeline, transport) is the real thing.
    /// </summary>
    [Collection(IntegrationCollection.Name)]
    [Trait("Category", "Integration")]
    public class ScaleBroadcastTests
    {
        private readonly BasculaApiFactory _factory;

        public ScaleBroadcastTests(BasculaApiFactory factory) => _factory = factory;

        [Fact]
        public async Task A_simulated_scale_reading_reaches_a_connected_signalr_client()
        {
            await using var connection = new HubConnectionBuilder()
                .WithUrl(new Uri(_factory.Server.BaseAddress, "basculaSocket"), options =>
                {
                    options.Transports = HttpTransportType.LongPolling;
                    options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                })
                .Build();

            var received = new List<double>();
            var gotOne = new TaskCompletionSource();
            connection.On<double>("ReceiveLecture", w =>
            {
                lock (received) received.Add(w);
                gotOne.TrySetResult();
            });

            await connection.StartAsync();

            // SerialPortHub subscribes to the fake in its constructor, which SignalR runs during
            // the connection lifecycle — wait for that before pushing a reading.
            var sw = Stopwatch.StartNew();
            while (!_factory.Bascula.HasSubscribers && sw.Elapsed < TimeSpan.FromSeconds(5))
                await Task.Delay(50);
            Assert.True(_factory.Bascula.HasSubscribers, "SerialPortHub never subscribed to OnBasculaRead");

            _factory.Bascula.RaiseWeight(42.5);

            await gotOne.Task.WaitAsync(TimeSpan.FromSeconds(5));

            lock (received) Assert.Contains(42.5, received);
        }
    }
}
