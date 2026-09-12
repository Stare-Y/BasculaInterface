using BasculaInterface.Services;
using BasculaInterface.Views;
using BasculaInterface.Views.PopUps;
using Core.Application.DTOs;
using Core.Application.Services;
using System.ComponentModel;

namespace BasculaInterface
{
    public partial class MainPage : ContentPage, INotifyPropertyChanged
    {
        private readonly IApiService _apiService;
        private readonly ISessionService _sessionService;
        private readonly InactivityWatcherService _inactivityWatcher;

        public MainPage()
        {
            InitializeComponent();

            BindingContext = this;

            _apiService = MauiProgram.ServiceProvider.GetService(typeof(IApiService)) as IApiService
                ?? throw new InvalidOperationException("IApiService not registered.");
            _sessionService = MauiProgram.ServiceProvider.GetService(typeof(ISessionService)) as ISessionService
                ?? throw new InvalidOperationException("ISessionService not registered.");
            _inactivityWatcher = MauiProgram.ServiceProvider.GetService(typeof(InactivityWatcherService)) as InactivityWatcherService
                ?? throw new InvalidOperationException("InactivityWatcherService not registered.");

            _inactivityWatcher.OnTimeout += OnInactivityTimeout;
        }

        /// <summary>
        /// Real login (issue #134) — replaces the old press-and-hold gesture that collected no
        /// credential. Resolves the identifier by UserCode then Username server-side; on success
        /// starts the inactivity watch and navigates in, mirroring the old LogIn() destination
        /// choice (Preferences "OnlyFinished").
        /// </summary>
        private async Task LogIn()
        {
            if (Preferences.Get("OnlyFinished", false))
                await Shell.Current.Navigation.PushModalAsync(new FinishedWeights());
            else
                await Shell.Current.Navigation.PushModalAsync(new PendingWeightsView());
        }

        private async void BtnLogin_Clicked(object sender, EventArgs e)
        {
            await BtnLogIn.ScaleTo(1.1, 100);
            await BtnLogIn.ScaleTo(1.0, 100);

            LoginErrorLabel.IsVisible = false;

            string identifier = IdentifierEntry.Text ?? string.Empty;
            string password = PasswordEntry.Text ?? string.Empty;

            if (string.IsNullOrWhiteSpace(identifier) || string.IsNullOrWhiteSpace(password))
            {
                LoginErrorLabel.Text = "Ingrese usuario/código y contraseña.";
                LoginErrorLabel.IsVisible = true;
                return;
            }

            WaitPopUp.Show("Iniciando sesión...");
            try
            {
                LoginResponse response = await _apiService.PostAsync<LoginResponse>(
                    "api/Auth/Login",
                    new LoginRequest(identifier, password));

                await _sessionService.LoginAsync(response);

                PasswordEntry.Text = string.Empty;

                _inactivityWatcher.RegisterActivity();
                // A fixed default here; the client re-reads the server-configured value once an
                // authenticated config endpoint exists (design.md Open Questions notes this as a
                // parameter to confirm, not a scope boundary).
                _inactivityWatcher.Start(TimeSpan.FromMinutes(10));

                await LogIn();
            }
            catch (Exception ex)
            {
                LoginErrorLabel.Text = "Usuario o contraseña incorrectos.";
                LoginErrorLabel.IsVisible = true;
                System.Diagnostics.Debug.WriteLine($"Login failed: {ex.Message}");
            }
            finally
            {
                WaitPopUp.Hide();
            }
        }

        private void PasswordEntry_Completed(object sender, EventArgs e)
        {
            BtnLogin_Clicked(BtnLogIn, EventArgs.Empty);
        }

        private void OnInactivityTimeout()
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await _sessionService.LogoutAsync();
                _inactivityWatcher.Stop();

                // Pop back to this login page from wherever the operator was.
                await Shell.Current.Navigation.PopToRootAsync();
            });
        }

        private async void BtnSettings_Tapped(object sender, TappedEventArgs e)
        {
            await BtnSettings.ScaleTo(1.1, 100);
            await BtnSettings.ScaleTo(1.0, 100);

            await Shell.Current.Navigation.PushModalAsync(new EditSettingsView());
        }
    }
}
