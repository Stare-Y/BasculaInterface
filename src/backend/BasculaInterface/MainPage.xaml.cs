using BasculaInterface.Views;
using BasculaInterface.Views.PopUps;
using System.ComponentModel;
using System.Threading.Tasks;

namespace BasculaInterface
{
    public partial class MainPage : ContentPage, INotifyPropertyChanged
    {
        private CancellationTokenSource? _cancellationTokenSource = null;
        public MainPage()
        {
            InitializeComponent();

            BindingContext = this;

            LoadPreferences();
        }

        private async Task LogIn()
        {
            SetPreferences();

            if (EntryHost.Text.Contains("http://"))
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
            await BtnLogIn.ScaleTo(1.1, 100);
            await BtnLogIn.ScaleTo(1.0, 100);

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
                WaitPopUp.Hide();
            }
            catch (Exception ex)
            {
                // Manejar cualquier otra excepción
                await DisplayAlert("Error", "Error al tratar de cambiar el host: " + ex.Message, "OK");
            }
        }

        private void CheckBoxSecondaryTerminal_CheckedChanged(object sender, CheckedChangedEventArgs e)
        {
            Preferences.Set("SecondaryTerminal", e.Value);
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

        private void LoadPreferences()
        {
            CheckBoxSecondaryTerminal.IsChecked = Preferences.Get("SecondaryTerminal", false);
            CheckBoxManualWeight.IsChecked = Preferences.Get("ManualWeight", false);
            CheckBoxRequirePartner.IsChecked = Preferences.Get("RequirePartner", false);
            CheckBoxOnlyPedidos.IsChecked = Preferences.Get("OnlyPedidos", false);
            CheckBoxBypasTurn.IsChecked = Preferences.Get("BypasTurn", false);
            EntryHost.Text = Preferences.Get("HostUrl", "bascula.cpe");

            //TODO: add this thing to settings lul
            Preferences.Set("FilterClasif6", true);
        }

        private void SetPreferences()
        {
            Preferences.Set("SecondaryTerminal", CheckBoxSecondaryTerminal.IsChecked);
            Preferences.Set("ManualWeight", CheckBoxManualWeight.IsChecked);
            Preferences.Set("RequirePartner", CheckBoxRequirePartner.IsChecked);
            Preferences.Set("OnlyPedidos", CheckBoxOnlyPedidos.IsChecked);
            Preferences.Set("BypasTurn", CheckBoxBypasTurn.IsChecked);
        }

        private async void BtnSettings_Tapped(object sender, TappedEventArgs e)
        {
            await BtnSettings.ScaleTo(1.1, 100);
            await BtnSettings.ScaleTo(1.0, 100);

            await Shell.Current.Navigation.PushModalAsync(new EditSettingsView());
        }
    }
}
