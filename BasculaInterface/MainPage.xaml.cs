using System.Drawing;
using System.Drawing.Printing;
using System.IO.Ports;
using System.Timers;

namespace BasculaInterface
{
    public partial class MainPage : ContentPage
    {
        private readonly SerialPort _bascula;

        private System.Timers.Timer _pollingTimer;

        public double Tara { get; set; }

        public MainPage()
        {
            InitializeComponent();
            _bascula = new SerialPort(MauiProgram.PortName, MauiProgram.PortBaud, Parity.None, 8, StopBits.One);

            _bascula.DataReceived += new SerialDataReceivedEventHandler(DataRecievedHandler);

            if (MauiProgram.NeedManualAsk)
            {
                _pollingTimer = new(1000);
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



            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (Tara != 0)
                {
                    double currentWeight = ParseScreenWeight(readData);

                    PesoLabel.Text = $"{currentWeight - Tara} kg";
                }
                else
                {
                    PesoLabel.Text = readData;
                }
            });
        }

        private void OnTaraClicked(object sender, EventArgs e)
        {
            Tara = ParseScreenWeight(PesoLabel.Text);

            TaraLabel.Text = Tara.ToString("0.000 kg");
        }

        private double ParseScreenWeight(string value)
        {
            string clearedString = value.Replace("kg", "", StringComparison.OrdinalIgnoreCase).Trim();

            if (double.TryParse(clearedString, System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out double peso))
            {
                return peso;
            }

            return 0;
        }

        private void OnImprimirClicked(object sender, EventArgs e)
        {
#if WINDOWS
            PrintDocument document = new();
            document.PrinterSettings.PrinterName = MauiProgram.PrinterName;

            document.PrintPage += (sender, e) =>
            {
                System.Drawing.Font font = new ("Courier New", 10);
                e.Graphics.DrawString($"COOPERATIVA PEDRO EZQUEDA\nTara: {Tara}\nPeso: {PesoLabel.Text}\nDiferencia: {ParseScreenWeight(PesoLabel.Text)}", font, Brushes.Black, 10, 10);
            };

            document.Print();
#endif
        }

        private void OnCeroClicked(object sender, EventArgs e)
        {
            Tara = 0;
            TaraLabel.Text = "0.000 kg";
        }
    }

}
