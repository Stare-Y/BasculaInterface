using Core.Application.Services;

namespace BasculaTerminalApi.Service
{
    public class WeightLogisticService : IWeightLogisticService
    {
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private string? _deviceWeighting = null;
        private CancellationTokenSource? _cts;

        private readonly TimeSpan _turnTimeout = TimeSpan.FromSeconds(10);

        public WeightLogisticService() { }

        public async Task<bool> ReleaseWeight(string deviceId)
        {
            if (string.IsNullOrEmpty(deviceId))
                return false;

            await _semaphore.WaitAsync();

            Console.WriteLine($"Releasing weight from device: {deviceId}");

            try
            {
                if (deviceId != _deviceWeighting)
                    return false;

                ResetDeviceWeighting();

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al liberar el peso: {ex.Message}");
                return false;
            }
            finally
            {
                _semaphore.Release();
            }

        }

        public async Task<bool> RequestWeight(string deviceId)
        {
            if (string.IsNullOrEmpty(deviceId))
                return false;

            await _semaphore.WaitAsync();

            Console.WriteLine($"Requesting weight for device: {deviceId}");

            try
            {
                // otro dispositivo ya ocupa el turno
                if (!string.IsNullOrEmpty(_deviceWeighting) && _deviceWeighting != deviceId)
                    return false;

                _deviceWeighting = deviceId;

                // cancelar timer previo
                _cts?.Cancel();
                _cts?.Dispose();

                // crear un nuevo token para el turno actual
                _cts = new CancellationTokenSource();

                // lanzar la tarea que libera el turno tras _turnTimeout
                _ = KeepTurnAliveAsync(_cts.Token);

                return true;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task KeepTurnAliveAsync(CancellationToken token)
        {
            try
            {
                await Task.Delay(_turnTimeout, token);
                ResetDeviceWeighting(); // libera el turno después del timeout
            }
            catch (TaskCanceledException)
            {
                // esperado si el turno se renueva
            }
            catch (Exception ex)
            {
                // opcional: log
                Console.WriteLine($"Error en KeepTurnAliveAsync: {ex.Message}");
                ResetDeviceWeighting(); // asegurarse de liberar
            }
        }

        public void ResetDeviceWeighting()
        {
            Console.WriteLine($"Liberando turno de {_deviceWeighting}");

            _deviceWeighting = null;

            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }
    }
}
