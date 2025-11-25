using BasculaInterface.ViewModels;
using BasculaInterface.Views.PopUps;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using Core.Application.DTOs;

namespace BasculaInterface.Views;

public partial class WeightingScreen : ContentPage
{
    public double Tara { get; set; }
    private bool _taraChanged = false;
    private CancellationTokenSource? _cancellationTokenSource = null;
    private CancellationTokenSource? _cancellationTokenKeepAlive = null;
    //private WaitPopUp? _popup { get; set; }
    private PickQuantityPopUp _pickPopup { get; set; }
    public WeightingScreen(BasculaViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        PesoLabel.IsEnabled = Preferences.Get("ManualWeight", false);
        InicializePopUps();
    }

    public WeightingScreen(WeightEntryDto weightEntry, ClienteProveedorDto? partner = null, ProductoDto? productoDto = null, bool useIncommingTara = true) : this(MauiProgram.ServiceProvider.GetRequiredService<BasculaViewModel>())
    {
        if (BindingContext is BasculaViewModel viewModel)
        {
            viewModel.WeightEntry = weightEntry;
            viewModel.Partner = partner;
            viewModel.Product = productoDto;

            if (useIncommingTara || !MauiProgram.IsSecondaryTerminal)
            {
                if (weightEntry.BruteWeight > 0)
                {
                    viewModel.SetTara(weightEntry.BruteWeight);
                }
                else
                {
                    viewModel.SetTara(0);
                }
            }
            else if(productoDto is not null )
            {
                WeightDetailDto weightingDetail = weightEntry.WeightDetails
                    .FirstOrDefault(x => x.FK_WeightedProductId == productoDto.Id) 
                    ?? throw new InvalidOperationException("Cannot weight a product, if theres not already specified");

                if (weightingDetail.SecondaryTare is not null && weightingDetail.SecondaryTare > 0)
                {

                    viewModel.SetTara(weightingDetail.SecondaryTare.Value);
                    _taraChanged = true;
                }
                else
                {
                    weightingDetail.SecondaryTare = 0;
                    viewModel.SetTara(0);
                    GridTaraLabel.IsVisible = false;
                    GridSetTaraInicial.IsVisible = true;
                    BtnCaptureNewWeight.IsVisible = false;
                }
            }
        }
        _pickPopup = new PickQuantityPopUp();
        //_popup = new WaitPopUp();
    }

    public WeightingScreen() : this(MauiProgram.ServiceProvider.GetRequiredService<BasculaViewModel>()) {
        InicializePopUps();
    }
    private void InicializePopUps()
    {
        _pickPopup = new PickQuantityPopUp();
    
    }
    private async Task KeepWeightAlive()
    {
        if (BindingContext is BasculaViewModel viewModel)
        {

            if (!await viewModel.CanWeight())
            {
                if (_cancellationTokenKeepAlive is not null)
                {
                    _cancellationTokenKeepAlive.Cancel();
                    _cancellationTokenKeepAlive.Dispose();
                    _cancellationTokenKeepAlive = null;
                }

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await DisplayAlert("Error", "La báscula dejó de estar disponible", "OK");
                    await Shell.Current.Navigation.PopModalAsync();
                });

                return;
            }
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is BasculaViewModel viewModel)
        {
            try
            {
                //TaraLabel.BackgroundColor = Colors.LightBlue;
                //TaraLabel.TextColor = Colors.Black;

                await viewModel.ConnectSocket();

                //BtnPickPartner.IsVisible = viewModel.Partner is null || viewModel.Partner.Id == 0;
                BtnPickProduct.IsVisible = viewModel.Product is null && (viewModel.WeightEntry?.TareWeight != 0);
                EntryVehiclePlate.IsEnabled = viewModel.WeightEntry is null || string.IsNullOrEmpty(viewModel.WeightEntry.VehiclePlate);

                if (Preferences.Get("BypasTurn", false))
                    return;

                _cancellationTokenKeepAlive = new CancellationTokenSource();
                _ = StartKeepAliveLoop(_cancellationTokenKeepAlive.Token);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
                await Shell.Current.Navigation.PopModalAsync();
            }
        }
    }

    private async Task StartKeepAliveLoop(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                await KeepWeightAlive();
                await Task.Delay(TimeSpan.FromSeconds(4), token);
            }
        }
        catch (TaskCanceledException)
        {
            // esperado al cerrar la pantalla
        }
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();

        if (BindingContext is BasculaViewModel viewModel)
        {
            if (_cancellationTokenKeepAlive is not null)
            {
                _cancellationTokenKeepAlive.Cancel();
                _cancellationTokenKeepAlive.Dispose();
                _cancellationTokenKeepAlive = null;
            }
            await viewModel.ReleaseSocket();
            await viewModel.ReleaseWeight();
        }
    }

    private async void OnImprimirClicked(object sender, EventArgs e)
    {
        if (BindingContext is BasculaViewModel viewModel)
        {
            string fechaHora = DateTime.Now.ToString("dd-MM-yyyy\nHH:mm:ss");

            string template = MauiProgram.PrintTemplate;
            string ticket = template
                .Replace("{fechaHora}", fechaHora)
                .Replace("{tara}", viewModel.Tara)
                .Replace("{neto}", viewModel.Peso)
                .Replace("{bruto}", viewModel.Diferencia);

            await viewModel.PrintTicketAsync(ticket);

            EscribirLog($"Tara: {viewModel.Tara} kg | " +
                    $"Neto: {viewModel.Peso} | " +
                    $"Diferencia: {viewModel.Diferencia} kg");
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

    private async void BtnReconect_Clicked(object sender, EventArgs e)
    {
        if (BindingContext is BasculaViewModel viewModel)
        {
            try
            {
                waitPopUp.Show();
                //if (_popup is null) _popup = new WaitPopUp();
                //this.ShowPopup(_popup);
                try
                {
                    await viewModel.ReleaseSocket();
                    await viewModel.ConnectSocket();
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", "Error al reconectar a la báscula: " + ex.Message, "OK");
                }
                finally
                {
                    waitPopUp.Hide();
                 }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Error inesperado: " + ex.Message, "OK");
            }
        }
    }
    private void DisplayWaitPopUp(string message = "Cargando, espere")
    {
        waitPopUp.Show(message);
        //if(_popup is null ) _popup = new WaitPopUp();
        //_popup.Message = message;

        //this.ShowPopup(_popup);
    }

    private async void BtnCaptureNewWeight_Clicked(object sender, EventArgs e)
    {
        if (BindingContext is BasculaViewModel viewModel)
        {
            DisplayWaitPopUp("Capturando peso, espere...");
            try
            {
                await viewModel.CaptureNewWeightEntry();

                await Shell.Current.Navigation.PopModalAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Error al capturar el nuevo peso: " + ex.Message, "OK");
            }
            finally
            {
                waitPopUp.Hide();
                //_popup?.Close();
            }
        }
    }

    private void OnPartnerSelected(ClienteProveedorDto partner)
    {
        if (BindingContext is BasculaViewModel viewModel)
        {
            viewModel.Partner = partner;
            BtnPickPartner.IsVisible = false;
        }
    }

    private void BtnPickPartner_Clicked(object sender, EventArgs e)
    {
        PartnerSelectView partnerSelectView = new PartnerSelectView();
        partnerSelectView.OnPartnerSelected += OnPartnerSelected;
        Shell.Current.Navigation.PushModalAsync(partnerSelectView);
    }

    private async void OnProductSelected(ProductoDto product)
    {
        if (BindingContext is BasculaViewModel viewModel)
        {
            viewModel.Product = product;

            BtnPickProduct.IsVisible = false;

            await Shell.Current.Navigation.PopModalAsync();
            _pickPopup.Product = product.Nombre;

            object? quantity = await pickPopUp.ShowAsync(product.Nombre);

            if (quantity is double qty)
                viewModel.ProductQuantity = qty;
        }
    }

    private void BtnPickProduct_Clicked(object sender, EventArgs e)
    {
        ProductSelectView productSelectView = new ProductSelectView();
        productSelectView.OnProductSelected += OnProductSelected;
        Shell.Current.Navigation.PushModalAsync(productSelectView);
    }

    private void BtnBack_Clicked(object sender, EventArgs e)
    {
        Shell.Current.Navigation.PopModalAsync();
    }

    private async void TaraLabel_Pressed(object sender, EventArgs e)
    {
        _cancellationTokenSource = new CancellationTokenSource();
        try
        {
            await Task.Delay(2121, _cancellationTokenSource.Token);
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                if (BindingContext is BasculaViewModel viewModel)
                {
                    viewModel.SetTaraFromPesoTotal();

                    _taraChanged = true;
                }

                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;

                await DisplayAlert("Tara Establecida", "La tara se ha restablecido correctamente.", "OK");
            });
        }
        catch (TaskCanceledException)
        {
            // La tarea fue cancelada, no hacer nada
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "Error al establecer la tara: " + ex.Message, "OK");
        }
    }

    private void TaraLabel_Released(object sender, EventArgs e)
    {
        if (_cancellationTokenSource != null)
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null;
        }
    }

    private async void BtnSetTaraInicial_Clicked(object sender, EventArgs e)
    {
        if (BindingContext is BasculaViewModel viewModel)
        {
            try
            {
                WeightDetailDto weightingDetail = viewModel.WeightEntry?.WeightDetails.FirstOrDefault(x => x.FK_WeightedProductId == viewModel.Product?.Id)
                    ?? throw new InvalidOperationException("Cannot weight a product, if theres not already specified");

                viewModel.SetTaraFromPesoTotal();

                await viewModel.PutSecondaryTara();

                _taraChanged = true;

                GridSetTaraInicial.IsVisible = false;

                GridTaraLabel.IsVisible = true;

                CaptureWeightLabel.Text = "Registrar Peso Final";

                BtnCaptureNewWeight.IsVisible = true;

                await DisplayAlert("Tara Establecida", "La tara se ha restablecido correctamente.", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Error al establecer la tara: " + ex.Message, "OK");
            }
        }
    }

    private void PesoLabel_TextChanged(object sender, TextChangedEventArgs e)
    {
        if(Preferences.Get("ManualWeight", false))
        {
            var entry = (Entry)sender;

            if (string.IsNullOrEmpty(entry.Text))
                return;

            if (entry.Text.ToUpper().Contains("KG"))
                return;

            // Allow digits and ONE dot
            if (!double.TryParse(entry.Text, out double manualWeight))
            {
                // revert to old text if invalid
                entry.Text = e.OldTextValue;
                return;
            }
            else
            {
                if (BindingContext is not BasculaViewModel viewModel)
                    return;

                viewModel.UpdateWeight(manualWeight, false);
            }
        }
    }
}