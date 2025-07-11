using System.Globalization;
using System.IO.Ports;
using System.Text.RegularExpressions;
using System.Timers;
using BasculaTerminalApi.Events;
using BasculaTerminalApi.Models;

namespace BasculaTerminalApi.Service
{
    public class BasculaService
    {
        private SerialPort _bascula = null!;

        private System.Timers.Timer _pollingTimer = null!;

        private int _lecturasFalsas = 0;

        private readonly string _askChar = string.Empty;

        //event to notify when a new weight is received
        public event EventHandler<OnBasculaReadEventArgs>? OnBasculaRead;

        public BasculaService(PrintSettings printSettings)
        {
            _bascula = new SerialPort(printSettings.BasculaPort, printSettings.BasculaBauds, Parity.None, 8, StopBits.One);

            _bascula.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);

            if (printSettings.NeedManualAsk)
            {
                _pollingTimer = new(750);
                _pollingTimer.Elapsed += new ElapsedEventHandler(OnTimerElapsed);
                _pollingTimer.AutoReset = true;
                _pollingTimer.Start();
                _askChar = printSettings.AskChar;
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
            value = value.Replace(" ", "").Trim();
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
