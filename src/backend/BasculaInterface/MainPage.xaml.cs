using BasculaInterface.Views;
using BasculaInterface.Views.PopUps;
using System.ComponentModel;

namespace BasculaInterface
{
    public partial class MainPage : ContentPage, INotifyPropertyChanged
    {
        private CancellationTokenSource? _cancellationTokenSource = null;
        public MainPage()
        {
            InitializeComponent();

            BindingContext = this;
        }

        private async Task LogIn()
        {
            await Shell.Current.Navigation.PushModalAsync(new PendingWeightsView());
            StackCheckBox.IsVisible = false;
            ScrollCheckBox.IsVisible = false;
        }

        private async void BtnLogin_Released(object sender, EventArgs e)
        {
            await BtnLogIn.ScaleTo(1.1, 100);
            await BtnLogIn.ScaleTo(1.0, 100);

            try
            {
                if (_cancellationTokenSource != null)
                {
                    _cancellationTokenSource.Cancel();
                    _cancellationTokenSource.Dispose();
                    _cancellationTokenSource = null;

                    await LogIn();

                    StackCheckBox.IsVisible = false;
                    ScrollCheckBox.IsVisible = false;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Error al tratar de cambiar el host: " + ex.Message, "OK");
            }
            finally
            {
                WaitPopUp.Hide();
            }
        }

        private async void BtnLogin_Pressed(object sender, EventArgs e)
        {
            WaitPopUp.Show("Cargando, por favor espere...");
            _cancellationTokenSource = new CancellationTokenSource();
            try
            {
                await Task.Delay(4444, _cancellationTokenSource.Token);
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    StackCheckBox.IsVisible = true;
                    ScrollCheckBox.IsVisible = true;

                    _cancellationTokenSource?.Cancel();
                    _cancellationTokenSource?.Dispose();
                    _cancellationTokenSource = null;
                });
            }
            catch (TaskCanceledException)
            {
                // La tarea fue cancelada, no hacer nada
                WaitPopUp.Hide();
            }
            catch (Exception ex)
            {
                // Manejar cualquier otra excepción
                await DisplayAlert("Error", "Error al tratar de cambiar el host: " + ex.Message, "OK");
            }
        }

        private async void BtnSettings_Tapped(object sender, TappedEventArgs e)
        {
            await BtnSettings.ScaleTo(1.1, 100);
            await BtnSettings.ScaleTo(1.0, 100);

            await Shell.Current.Navigation.PushModalAsync(new EditSettingsView());

            StackCheckBox.IsVisible = false;
            ScrollCheckBox.IsVisible = false;
        }
    }
}
