using System.Diagnostics;
using System.Drawing;
using System.Drawing.Printing;
using System.Globalization;
using System.IO.Ports;
using System.Text.RegularExpressions;
using System.Timers;

namespace BasculaInterface
{
    public partial class MainPage : ContentPage
    {
        private readonly SerialPort _bascula;

        private System.Timers.Timer _pollingTimer;

        private int _lecturasFalsas = 0;

        public double Tara { get; set; }

        public MainPage()
        {
            InitializeComponent();
            _bascula = new SerialPort(MauiProgram.PortName, MauiProgram.PortBaud, Parity.None, 8, StopBits.One);

            _bascula.DataReceived += new SerialDataReceivedEventHandler(DataRecievedHandler);

            if (MauiProgram.NeedManualAsk)
            {
                _pollingTimer = new(MauiProgram.TimerElapse);
                _pollingTimer.Elapsed += new ElapsedEventHandler(OnTimerElapsed);
                _pollingTimer.AutoReset = true;
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _bascula.Open();

            if (MauiProgram.NeedManualAsk)
                _pollingTimer.Start();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _bascula.Close();

            if (MauiProgram.NeedManualAsk)
                _pollingTimer.Stop();
        }

        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                if (_bascula.IsOpen)
                {
                    // Este comando depende de tu modelo de báscula.
                    // Muchos usan "P\r\n" o solo "\r\n" para pedir el peso.
                    _bascula.Write(MauiProgram.AskChar);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al enviar comando: {ex.Message}");
            }
        }

        private void DataRecievedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            string readData = sp.ReadExisting();

            Debug.WriteLine($"Lectura del puerto {MauiProgram.PortName}: {readData}");


            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (Tara != 0)
                {
                    double currentWeight = ParseScreenWeight(readData);

                    DiferenciaLabel.Text = $"{Math.Abs(Tara - currentWeight):0.00} kg";


                    PesoLabel.Text = currentWeight.ToString("0.00 kg");
                }
                else
                {
                    double currentWeight = ParseScreenWeight(readData);
                    PesoLabel.Text = currentWeight.ToString("0.00 kg");
                }
            });
        }

        private void OnTaraClicked(object sender, EventArgs e)
        {
            Tara = ParseScreenWeight(PesoLabel.Text);

            TaraLabel.Text = Tara.ToString("0.00 kg");
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

                Debug.WriteLine($"Conteo de lectura falsa: {_lecturasFalsas}");

                return ParseScreenWeight(PesoLabel.Text);
            }
        }

        private void OnImprimirClicked(object sender, EventArgs e)
        {
            string fechaHora = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
#if WINDOWS
            PrintDocument document = new();
            document.PrinterSettings.PrinterName = MauiProgram.PrinterName;

            document.PrintPage += (sender, e) =>
            {
                System.Drawing.Font font = new ("Courier New", 16);
                e.Graphics.DrawString(
                    $"COOPERATIVA PEDRO EZQUEDA\n" +
                    $"Fecha: {fechaHora}\n" +
                    $"Tara: {Tara:0.00} kg\n" +
                    $"Neto: {PesoLabel.Text}\n" +
                    $"Diferencia: {Math.Abs(ParseScreenWeight(PesoLabel.Text) - Tara):0.00} kg", font, Brushes.Black, 10, 10);
            };

            Task.Run(() => document.Print());

#endif
            EscribirLog($"Tara: {Tara:0.00} kg | " +
                    $"Neto: {PesoLabel.Text} | " +
                    $"Diferencia: {Math.Abs(ParseScreenWeight(PesoLabel.Text) - Tara):0.00} kg");
        }

        private void EscribirLog(string mensaje)
        {
            string logDir = Path.Combine(AppContext.BaseDirectory, "logs");
            string logPath = Path.Combine(logDir, "log.txt");

            // Crear carpeta si no existe
            if (!Directory.Exists(logDir))
                Directory.CreateDirectory(logDir);

            // Abrir, escribir y cerrar el archivo inmediatamente
            using (StreamWriter writer = new StreamWriter(logPath, append: true))
            {
                writer.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {mensaje}");
            }
        }

        private void OnCeroClicked(object sender, EventArgs e)
        {
            Tara = 0;
            TaraLabel.Text = "0.00 kg";
            DiferenciaLabel.Text = "0.00 kg";
        }
    }

}
