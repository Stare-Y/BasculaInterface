using BasculaInterface.Views;
using BasculaInterface.Views.PopUps;
using CommunityToolkit.Maui.Views;
using System.ComponentModel;
using System.Diagnostics;

namespace BasculaInterface
{
    public partial class MainPage : ContentPage, INotifyPropertyChanged
    {
        private CancellationTokenSource? _cancellationTokenSource = null;
        private WaitPopUp? _popup;
        public MainPage()
        {
            InitializeComponent();
            BindingContext = this;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            try
            {
                EntryHost.Text = Preferences.Get("HostUrl", "bascula.cpe");
            }
            catch (Exception ex)
            {
                Debug.Write(ex.Message);
            }
        }

        private void DisplayWaitPopUp(string message = "Cargando, espere")
        {
            _popup = new WaitPopUp(message);

            this.ShowPopup(_popup);
        }

        private async Task LogIn()
        {
            DisplayWaitPopUp("Iniciando sesión, espere");

            if (EntryHost.Text.Contains("http"))
            {
                MauiProgram.BasculaSocketUrl = EntryHost.Text;
            }
            else
            {
                MauiProgram.BasculaSocketUrl = "http://" + EntryHost.Text + "/";
            }

            Preferences.Set("HostUrl", MauiProgram.BasculaSocketUrl);

            try
            {
                if (EntryHost.Text == string.Empty)
                {
                    await DisplayAlert("Error", "Por favor, ingrese la URL del host.", "OK");
                    return;
                }

                await Task.Delay(1000); // Simulate a delay for login process
                await Shell.Current.Navigation.PushModalAsync(new PendingWeightsView());
                EntryHost.IsVisible = false;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "No se pudo iniciar sesión: " + ex.Message, "OK");
            }
            finally
            {
                _popup?.Close();
            }
        }

        private async void BtnLogin_Released(object sender, EventArgs e)
        {
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
                if (string.IsNullOrEmpty(EntryHost.Text))
                {
                    await DisplayAlert("Error", "La URL del host no puede estar vacía.", "OK");
                    return;
                }
                await LogIn();
            }
        }


        private async void BtnLogin_Pressed(object sender, EventArgs e)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            try
            {
                await Task.Delay(4444, _cancellationTokenSource.Token);
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    EntryHost.IsVisible = true;

                    _cancellationTokenSource?.Cancel();
                    _cancellationTokenSource?.Dispose();
                    _cancellationTokenSource = null;
                });
            }
            catch (TaskCanceledException)
            {
                // La tarea fue cancelada, no hacer nada
            }
            catch (Exception ex)
            {
                // Manejar cualquier otra excepción
                await DisplayAlert("Error", "Error al tratar de cambiar el host: " + ex.Message, "OK");
            }
        }
    }
}
