using BasculaInterface.Views;
using BasculaInterface.Views.PopUps;
using CommunityToolkit.Maui.Views;

namespace BasculaInterface
{
    public partial class MainPage : ContentPage
    {
        private WaitPopUp? _popup;
        public MainPage()
        {
            InitializeComponent();
        }

        private void DisplayWaitPopUp(string message = "Cargando, espere")
        {
            _popup = new WaitPopUp(message);

            this.ShowPopup(_popup);
        }

        private async void BtnLogIn_Clicked(object sender, EventArgs e)
        {
            DisplayWaitPopUp("Iniciando sesión, espere");

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
