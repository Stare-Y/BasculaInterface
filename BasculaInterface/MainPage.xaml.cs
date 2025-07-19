using BasculaInterface.Views;
using BasculaInterface.Views.PopUps;
using CommunityToolkit.Maui.Views;
using System.ComponentModel;
using System.Diagnostics;

namespace BasculaInterface
{
    public partial class MainPage : ContentPage, INotifyPropertyChanged
    {
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
                EntryHost.Text = Preferences.Get("HostUrl", string.Empty);
            }
            catch(Exception ex)
            {
                Debug.Write(ex.Message);
            }
        }

        private void DisplayWaitPopUp(string message = "Cargando, espere")
        {
            _popup = new WaitPopUp(message);

            this.ShowPopup(_popup);
        }

        private async void BtnLogIn_Clicked(object sender, EventArgs e)
        {
            DisplayWaitPopUp("Iniciando sesión, espere");

            if(EntryHost.Text != string.Empty)
            {
                if (EntryHost.Text.Contains("http"))
                {
                    MauiProgram.BasculaSocketUrl = EntryHost.Text;
                }
                else
                {
                    MauiProgram.BasculaSocketUrl = "http://" + EntryHost.Text + ":6969/";
                }
            }

            try
            {
                await Task.Delay(1000); // Simulate a delay for login process
                await Shell.Current.Navigation.PushModalAsync(new PendingWeightsView());
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
    }
}
