using BasculaInterface.ViewModels.Base;
using Microsoft.AspNetCore.SignalR.Client;

namespace BasculaInterface.ViewModels
{
    public class BasculaViewModel : ViewModelBase
    {
        private string _peso = "0.00";
        public string Peso
        {
            get => _peso;
            set
            {
                if (_peso != value)
                {
                    _peso = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _tara = "0.00";
        public string Tara
        {
            get => _tara;
            set
            {
                if (_tara != value)
                {
                    _tara = value;
                    OnPropertyChanged();
                }
            }
        }
        private string _diferencia = "0.00";
        public string Diferencia
        {
            get => _diferencia;
            set
            {
                if (_diferencia != value)
                {
                    _diferencia = value;
                    OnPropertyChanged();
                }
            }
        }
        private string _estado = "Desconectado";
        public string Estado
        {
            get => _estado;
            private set
            {
                if (_estado != value)
                {
                    _estado = value;
                    OnPropertyChanged();
                }
            }
        }

        public double TaraValue { get; set; }

        private HubConnection? _basculaSocketHub;

        event Action<double>? OnWeightReceived;

        public BasculaViewModel() { }

        public async Task ConnectSocket(string socketUrl)
        {
            try
            {
                _basculaSocketHub = new HubConnectionBuilder()
                .WithUrl(socketUrl)
                .Build();

                _basculaSocketHub.On<double>("ReceiveNumber", data =>
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        OnWeightReceived?.Invoke(data);
                    });
                });

                OnWeightReceived += UpdateWeight;

                await _basculaSocketHub.StartAsync();
                Estado = "Conectado";
            }
            catch (Exception ex)
            {
                Estado = "Error: " + ex.Message;
            }
        }

        private void UpdateWeight(double data)
        {
            if (TaraValue != 0)
                Diferencia = (TaraValue - data).ToString("0.00");

            Peso = data.ToString("0.00");
        }

        public async Task DisconnectSocket()
        {
            try
            {
                if (_basculaSocketHub == null)
                    return;
                await _basculaSocketHub.StopAsync() ;
                await _basculaSocketHub.DisposeAsync();
                Estado = "Desconectado";
            }
            catch (Exception ex)
            {
                Estado = "Error: " + ex.Message;
            }
        }
    }
}
