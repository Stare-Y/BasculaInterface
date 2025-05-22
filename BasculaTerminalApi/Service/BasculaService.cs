using System.Globalization;
using System.IO.Ports;
using System.Text.RegularExpressions;
using System.Timers;
using BasculaTerminalApi.Events;

namespace BasculaTerminalApi.Service
{
    public class BasculaService
    {
        private SerialPort _bascula = null!;

        private System.Timers.Timer _pollingTimer;

        private int _lecturasFalsas = 0;

        //event to notify when a new weight is received
        public event EventHandler<OnBasculaReadEventArgs> OnBasculaRead;

        public BasculaService()
        {
            _bascula = new SerialPort("COM1", 9600, Parity.None, 8, StopBits.One);

            _bascula.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);

            if (true)
            {
                _pollingTimer = new(750);
                _pollingTimer.Elapsed += new ElapsedEventHandler(OnTimerElapsed);
                _pollingTimer.AutoReset = true;
                _pollingTimer.Start();
            }

            _bascula.Open();
        }

        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                if (_bascula.IsOpen)
                {
                    // Este comando depende de tu modelo de báscula.
                    // Muchos usan "P\r\n" o solo "\r\n" para pedir el peso.
                    _bascula.Write("p");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al enviar comando: {ex.Message}");
            }
        }

        private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            string readData = sp.ReadExisting();

            double currentWeight = ParseScreenWeight(readData);

            //invoke on weight received event
            if (currentWeight != -1)
            {
                OnBasculaRead?.Invoke(this, new OnBasculaReadEventArgs(currentWeight));
            }
        }

        private double ParseScreenWeight(string value)
        {
            var match = Regex.Match(value, @"-?\d+(\.\d+)?");

            if (match.Success && double.TryParse(match.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double result))
            {
                _lecturasFalsas = 0;
                return result;
            }
            else
            {
                if (++_lecturasFalsas > 5)
                {
                    _lecturasFalsas = 0;
                    return 0;
                }
            }

            return -1;
        }
    }
}
