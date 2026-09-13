using Core.Application.Services;

namespace Infrastructure.Service
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
                CancellationTokenSource ownedCts = new();
                _cts = ownedCts;

                // lanzar la tarea que libera el turno tras _turnTimeout. ownedCts is captured by
                // value (a parameter, not the _cts field) so this task only ever resets the turn it
                // was actually created for — see KeepTurnAliveAsync's comment.
                _ = KeepTurnAliveAsync(ownedCts.Token, ownedCts);

                return true;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Auto-releases the turn after <see cref="_turnTimeout"/> if nobody renewed it.
        /// Two things this must get right (fix-weight-lock-race):
        /// 1. All access to <see cref="_deviceWeighting"/>/<see cref="_cts"/> must go through
        ///    <see cref="_semaphore"/> — this previously called <see cref="ResetDeviceWeighting"/>
        ///    unsynchronized, racing directly against <see cref="RequestWeight"/>/<see cref="ReleaseWeight"/>.
        /// 2. This must only reset the turn it was itself created for. <paramref name="ownedCts"/> is
        ///    the exact <see cref="CancellationTokenSource"/> instance <see cref="RequestWeight"/> had
        ///    just assigned to <see cref="_cts"/> when this task was launched — reading the <c>_cts</c>
        ///    field here instead (a mutable field, reassigned on every renewal) would let a
        ///    slow-to-fire, already-superseded timer clobber a turn a renewal or a different device
        ///    already legitimately holds.
        /// </summary>
        private async Task KeepTurnAliveAsync(CancellationToken token, CancellationTokenSource ownedCts)
        {
            try
            {
                await Task.Delay(_turnTimeout, token);
                await ResetIfStillOwnedAsync(ownedCts);
            }
            catch (TaskCanceledException)
            {
                // esperado si el turno se renueva
            }
            catch (Exception ex)
            {
                // opcional: log
                Console.WriteLine($"Error en KeepTurnAliveAsync: {ex.Message}");
                await ResetIfStillOwnedAsync(ownedCts); // asegurarse de liberar
            }
        }

        private async Task ResetIfStillOwnedAsync(CancellationTokenSource ownedCts)
        {
            await _semaphore.WaitAsync();
            try
            {
                if (ReferenceEquals(_cts, ownedCts))
                {
                    ResetDeviceWeighting();
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>Caller must hold <see cref="_semaphore"/> — this only mutates state, it doesn't
        /// synchronize access to it.</summary>
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
