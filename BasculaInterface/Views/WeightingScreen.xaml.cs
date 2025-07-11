using BasculaInterface.ViewModels;
using BasculaInterface.Views.PopUps;
using CommunityToolkit.Maui.Views;
using Core.Application.DTOs;

namespace BasculaInterface.Views;

public partial class WeightingScreen : ContentPage
{
    public double Tara { get; set; }
    private CancellationTokenSource _cancellationTokenSource = null!;
    public WeightingScreen(BasculaViewModel viewModel)
	{
		InitializeComponent();
        BindingContext = viewModel;
	}
    public WeightingScreen(WeightEntryDto weightEntry, ClienteProveedorDto? partner = null) : this(MauiProgram.ServiceProvider.GetRequiredService<BasculaViewModel>())
    {
        if(BindingContext is BasculaViewModel viewModel)
        {
            viewModel.WeightEntry = weightEntry;
            viewModel.Partner = partner;

            if(weightEntry.TareWeight > 0)
            {
                viewModel.TaraValue = weightEntry.TareWeight;
                viewModel.Tara = weightEntry.TareWeight.ToString("F2");
            }
            else
            {
                viewModel.TaraValue = 0;
                viewModel.Tara = "0.00";
            }
        }
    }

    public WeightingScreen() : this(MauiProgram.ServiceProvider.GetRequiredService<BasculaViewModel>()) { }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if(BindingContext is BasculaViewModel viewModel)
        {
            await viewModel.ConnectSocket(MauiProgram.BasculaSocketUrl);
        }
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();

        if (BindingContext is BasculaViewModel viewModel)
        {
            await viewModel.DisconnectSocket();
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
                    await viewModel.DisconnectSocket();
                    await viewModel.ConnectSocket(MauiProgram.BasculaSocketUrl);
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

    private void Cero_Pressed(object sender, EventArgs e)
    {
        _cancellationTokenSource = new();

        Task.Run(async () =>
        {
            try
            {
                await Task.Delay(4000, _cancellationTokenSource.Token);

                MainThread.BeginInvokeOnMainThread(() =>
                {

                    SetHostPopUp popup = new(MauiProgram.BasculaSocketUrl);

                    popup.OnHostSet += async (host) =>
                    {
                        await OnHostSet(host, popup);
                    };

                    this.ShowPopup(popup);
                });
            }
            catch (TaskCanceledException)
            {
                // La tarea fue cancelada, no hacer nada
            }
            catch (Exception ex)
            {
                // Manejar cualquier otra excepción
                await DisplayAlert("Error", "Error al tratar de cambiar e host: " + ex.Message, "OK");
            }
        });
    }

    private async Task OnHostSet(string host, SetHostPopUp popup)
    {
        if (BindingContext is BasculaViewModel viewModel)
        {
            WaitPopUp waitPopUp = new();

            if (BindingContext is BasculaViewModel)
                this.ShowPopup(waitPopUp);
            try
            {
                MauiProgram.BasculaSocketUrl = host;
                await viewModel.ConnectSocket(host);
                popup.Close();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Error al conectar a la báscula: " + ex.Message, "OK");
            }
            finally
            {
                waitPopUp.Close();
            }
        }
    }

    private void Cero_Released(object sender, EventArgs e)
    {
        if (BindingContext is BasculaViewModel viewModel)
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();

            viewModel.TaraValue = 0;
            viewModel.Tara = "0.00";
            viewModel.Diferencia = "0.00 ";
        }
    }
}