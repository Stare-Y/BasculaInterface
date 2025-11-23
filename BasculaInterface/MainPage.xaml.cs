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
            CheckBoxManualWeight.IsChecked = Preferences.Get("ManualWeight", false);
            CheckBoxRequirePartner.IsChecked = Preferences.Get("RequirePartner", false);
            CheckBoxOnlyPedidos.IsChecked = Preferences.Get("OnlyPedidos", false);
            CheckBoxBypasTurn.IsChecked = Preferences.Get("BypasTurn", false);

            if (MauiProgram.IsSecondaryTerminal)
            {
                CheckBoxSecondaryTerminal.IsChecked = true;
                CheckBoxManualWeight.IsEnabled = false;
                CheckBoxRequirePartner.IsEnabled = false;

                CheckBoxManualWeight.IsChecked = false;
                CheckBoxRequirePartner.IsChecked = false;
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
            ScrollCheckBox.IsVisible = false;
            CheckBoxSecondaryTerminal.IsVisible = false;
        }

        private async void BtnLogin_Released(object sender, EventArgs e)
        {
            BtnLogIn.Opacity = 0;
            await BtnLogIn.FadeTo(1, 200);

            try
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
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Error al tratar de cambiar el host: " + ex.Message, "OK");
            }
            finally
            {
                _popup?.Close();
                _popup = null;
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
                    ScrollCheckBox.IsVisible = true;
                    CheckBoxSecondaryTerminal.IsVisible = true;

                    _cancellationTokenSource?.Cancel();
                    _cancellationTokenSource?.Dispose();
                    _cancellationTokenSource = null;
                });
            }
            catch (TaskCanceledException)
            {
                // La tarea fue cancelada, no hacer nada
                _popup?.Close();
                _popup = null;
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

            CheckBoxManualWeight.IsEnabled = !e.Value;
        }

        private void CheckBoxManualWeight_CheckedChanged(object sender, CheckedChangedEventArgs e)
        {
            Preferences.Set("ManualWeight", e.Value);
        }

        private void CheckBoxRequirePartner_CheckedChanged(object sender, CheckedChangedEventArgs e)
        {
            Preferences.Set("RequirePartner", e.Value);
        }

        private void CheckBoxOnlyPedidos_CheckedChanged(object sender, CheckedChangedEventArgs e)
        {
            Preferences.Set("OnlyPedidos", e.Value);
        }

        private void CheckBoxBypasTurn_CheckedChanged(object sender, CheckedChangedEventArgs e)
        {
            Preferences.Set("BypasTurn", e.Value);
        }
    }
}
