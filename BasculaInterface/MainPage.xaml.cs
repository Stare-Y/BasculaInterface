using BasculaInterface.ViewModels;

namespace BasculaInterface
{
    public partial class MainPage : ContentPage
    {
        public double Tara { get; set; }

        private readonly BasculaViewModel _viewModel;

        public MainPage(BasculaViewModel viewModel)
        {
            InitializeComponent();

            BindingContext = viewModel;

            _viewModel = viewModel;
        }

        public MainPage() : this(MauiProgram.ServiceProvider.GetRequiredService<BasculaViewModel>())
        {
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.ConnectSocket(MauiProgram.BasculaSocketUrl);
        }

        protected override async void OnDisappearing()
        {
            base.OnDisappearing();
            await _viewModel.DisconnectSocket();
        }

        private void OnTaraClicked(object sender, EventArgs e)
        {
            var tara = double.Parse(_viewModel.Peso);

            _viewModel.TaraValue = tara;

            _viewModel.Tara = tara.ToString("F2");
        }

        private async void OnImprimirClicked(object sender, EventArgs e)
        {
            try
            {
                string fechaHora = DateTime.Now.ToString("dd-MM-yyyy\nHH:mm:ss");

//#if WINDOWS
//            PrintDocument document = new();
//            document.PrinterSettings.PrinterName = MauiProgram.PrinterName;

//            string template = MauiProgram.PrintTemplate;
//            string ticket = template
//                .Replace("{fechaHora}", fechaHora)
//                .Replace("{tara}", _viewModel.Tara)
//                .Replace("{neto}", _viewModel.Peso)
//                .Replace("{bruto}", _viewModel.Diferencia);


//            document.PrintPage += (sender, e) =>
//            {
//                System.Drawing.Font font = new ("Courier New", MauiProgram.PrintFontSize);
//                e.Graphics.DrawString(ticket    , font, Brushes.Black, 10, 10);
//            };

//            Task.Run(() => document.Print());
//#endif
                EscribirLog($"Tara: {_viewModel.Tara} kg | " +
                        $"Neto: {_viewModel.Peso} | " +
                        $"Diferencia: {_viewModel.Diferencia} kg");
            }
            catch(Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
            
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
                writer.WriteLine($"[{DateTime.Now:dd-MM-yyyy HH:mm:ss}] {mensaje}");
            }
        }

        private void OnCeroClicked(object sender, EventArgs e)
        {
            _viewModel.TaraValue = 0;
            _viewModel.Tara = "0.00";
            _viewModel.Diferencia = "0.00 ";
        }
    }

}
