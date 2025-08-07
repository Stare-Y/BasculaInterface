using BasculaInterface.ViewModels;
using BasculaInterface.Views.PopUps;
using CommunityToolkit.Maui.Views;
using Core.Application.DTOs;
using Microsoft.Maui.Graphics.Text;

namespace BasculaInterface.Views;

public partial class WeightingScreen : ContentPage
{
    public double Tara { get; set; }
    private bool _taraChanged = false;
    private double? _oldTara = null;
    private CancellationTokenSource? _cancellationTokenSource = null;
    public WeightingScreen(BasculaViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        
    }

    public WeightingScreen(WeightEntryDto weightEntry, ClienteProveedorDto? partner = null, ProductoDto? productoDto = null) : this(MauiProgram.ServiceProvider.GetRequiredService<BasculaViewModel>())
    {
        if (BindingContext is BasculaViewModel viewModel)
        {
            viewModel.WeightEntry = weightEntry;
            viewModel.Partner = partner;
            viewModel.Product = productoDto;

            if (weightEntry.BruteWeight > 0)
            {
                viewModel.SetTara(weightEntry.BruteWeight);
            }
            else
            {
                viewModel.SetTara(0);
            }
        }
    }

    public WeightingScreen() : this(MauiProgram.ServiceProvider.GetRequiredService<BasculaViewModel>()) { }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is BasculaViewModel viewModel)
        {
            if (!(viewModel.TaraCurrentValue <= 0))
            {
#if ANDROID

                TaraLabel.IsEnabled = true;
#endif
            }
            else
            {
                TaraLabel.IsEnabled = false;
            }

            TaraLabel.BackgroundColor = Colors.LightBlue;
            TaraLabel.TextColor = Colors.Black;

            await viewModel.ConnectSocket();

            if (viewModel.Partner is null || viewModel.Partner.Id == 0)
            {
                BtnPickPartner.IsVisible = true;
            }
            else
            {
                BtnPickPartner.IsVisible = false;
            }

            if (viewModel.Product is null)
            {
                if (viewModel.WeightEntry?.TareWeight != 0)
                {
                    BtnPickProduct.IsVisible = true;
                }
            }
            else
            {
                BtnPickProduct.IsVisible = false;
            }

            if (viewModel.WeightEntry is not null && !string.IsNullOrEmpty(viewModel.WeightEntry.VehiclePlate))
            {
                EntryVehiclePlate.IsEnabled = false;
            }
            else
            {
                EntryVehiclePlate.IsEnabled = true;
            }
        }
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();

        if (BindingContext is BasculaViewModel viewModel)
        {
            await viewModel.ReleaseSocket();
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
                WaitPopUp popup = new WaitPopUp();

                this.ShowPopup(popup);
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
                    popup.Close();
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Error inesperado: " + ex.Message, "OK");
            }
        }
    }

    private async void BtnCaptureNewWeight_Clicked(object sender, EventArgs e)
    {
        if (BindingContext is BasculaViewModel viewModel)
        {
            try
            {
                await viewModel.CaptureNewWeightEntry(_oldTara);

                await Shell.Current.Navigation.PopModalAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Error al capturar el nuevo peso: " + ex.Message, "OK");
            }
        }
    }

    private async void OnPartnerSelected(ClienteProveedorDto partner)
    {
        if (BindingContext is BasculaViewModel viewModel)
        {
            viewModel.Partner = partner;
            BtnPickPartner.IsVisible = false;
            await Shell.Current.Navigation.PopModalAsync();
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
                if(BindingContext is BasculaViewModel viewModel)
                {
                    if(!_taraChanged)
                        _oldTara = viewModel.TaraCurrentValue;
                    
                    viewModel.SetTaraFromPesoTotal();

                    _taraChanged = true;
                }

                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;

                await DisplayAlert("Tara Establecida", "La tara se ha restablecido correctamente.", "OK");
            });
        }
        catch(TaskCanceledException)
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
}