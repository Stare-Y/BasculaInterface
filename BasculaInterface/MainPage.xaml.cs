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
            if (MauiProgram.IsSecondaryTerminal)
            {
                CheckBoxSecondaryTerminal.IsChecked = true;
            }
            else
            {
                CheckBoxSecondaryTerminal.IsChecked = false;
            }
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

            if (EntryHost.Text.Contains("http"))
            {
                Preferences.Set("HostUrl", EntryHost.Text);
            }
            else
            {
                Preferences.Set("HostUrl", "http://" + EntryHost.Text + "/");
            }


            if (EntryHost.Text == string.Empty)
            {
                await DisplayAlert("Error", "Por favor, ingrese la URL del host.", "OK");
                return;
            }

            await Shell.Current.Navigation.PushModalAsync(new PendingWeightsView());
            BorderEntryHost.IsVisible = false;
            StackCheckBox.IsVisible = false;
            CheckBoxSecondaryTerminal.IsVisible = false;
        }

        private async void BtnLogin_Released(object sender, EventArgs e)
        {
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
                try
                {
                    if (string.IsNullOrEmpty(EntryHost.Text))
                    {
                        await DisplayAlert("Error", "La URL del host no puede estar vacía.", "OK");
                        return;
                    }
                    await LogIn();
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", "Error al tratar de cambiar el host: " + ex.Message, "OK");
                }
                finally
                {
                    _popup?.Close();

                }
            }
        }


        private async void BtnLogin_Pressed(object sender, EventArgs e)
        {
            DisplayWaitPopUp("Cargando, por favor espere...");
            _cancellationTokenSource = new CancellationTokenSource();
            try
            {
                await Task.Delay(4444, _cancellationTokenSource.Token);
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    BorderEntryHost.IsVisible = true;
                    StackCheckBox.IsVisible = true;
                    CheckBoxSecondaryTerminal.IsVisible = true;

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

        private void CheckBoxSecondaryTerminal_CheckedChanged(object sender, CheckedChangedEventArgs e)
        {
            MauiProgram.IsSecondaryTerminal = e.Value;

            Preferences.Set("SecondaryTerminal", e.Value);
        }
    }
}
