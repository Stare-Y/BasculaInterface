using BasculaInterface.Services;
using BasculaInterface.Views;
using BasculaInterface.Views.PopUps;
using Core.Application.DTOs;
using Core.Application.Services;
using Core.Domain.Entities.Identity;
using System.ComponentModel;

namespace BasculaInterface
{
    public partial class MainPage : ContentPage, INotifyPropertyChanged
    {
        private readonly IApiService _apiService;
        private readonly ISessionService _sessionService;
        private readonly InactivityWatcherService _inactivityWatcher;

        // fix-session-inactivity-timeout design.md Decision 2: prevents OnInactivityTimeout's own
        // navigation from firing concurrently with a user-triggered one already in flight.
        private readonly SemaphoreSlim _navigationGate = new(1, 1);

        // Set when a timeout fires while the window is unfocused — the logout navigation is
        // deferred until OnWindowActivated fires (see the constructor and PerformLogoutNavigationAsync).
        private bool _logoutNavigationPending;

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

            // A timeout that fired while unfocused left its navigation pending — run it now that
            // focus is back (see OnInactivityTimeout's comment).
            _inactivityWatcher.OnWindowActivated += () =>
            {
                if (!_logoutNavigationPending)
                    return;

                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    if (!_logoutNavigationPending)
                        return;

                    _logoutNavigationPending = false;
                    await PerformLogoutNavigationAsync();
                });
            };
        }

        /// <summary>
        /// Real login (issue #134) — replaces the old press-and-hold gesture that collected no
        /// credential. Resolves the identifier by UserCode then Username server-side; on success
        /// starts the inactivity watch and navigates in, based on the logged-in user's resolved
        /// TerminalMode (role-driven-terminal-modes design.md Decision 1 — replaces the old
        /// device-local "OnlyFinished" Preferences toggle). Called after _sessionService.LoginAsync,
        /// so CurrentUser is already populated here.
        /// </summary>
        private async Task LogIn()
        {
            if (_sessionService.CurrentUser?.TerminalMode == TerminalMode.OnlyFinished)
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
                // Per-role, per-user value from the server (fix-session-inactivity-timeout design.md
                // Decision 3) — replaces the previous hardcoded 10-minute constant.
                _inactivityWatcher.Start(TimeSpan.FromMinutes(response.User.InactivityTimeoutMinutes));

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
                // Clearing the session happens immediately regardless of window focus — this is
                // the actual security boundary. Any API call made before the deferred navigation
                // below runs will already fail (unauthenticated) against the server.
                await _sessionService.LogoutAsync();
                _inactivityWatcher.Stop();

                if (_inactivityWatcher.IsWindowActive)
                {
                    await PerformLogoutNavigationAsync();
                }
                else
                {
                    // Navigating while the window lacks OS focus has been observed to leave the
                    // app stuck (visible/hoverable but unresponsive to clicks/keys) — a WinUI-level
                    // focus issue we can't fix directly. Defer instead: OnWindowActivated (wired in
                    // the constructor) runs this the moment focus actually returns.
                    _logoutNavigationPending = true;
                }
            });
        }

        private async Task PerformLogoutNavigationAsync()
        {
            // fix-session-inactivity-timeout design.md Decision 2: if a user-triggered navigation
            // is already in flight, skip this cycle rather than collide with it — safe, since
            // whatever is holding the gate already just reset the watcher.
            if (!await _navigationGate.WaitAsync(0))
                return;

            try
            {
                // Almost every screen in this app is opened via PushModalAsync onto the
                // ModalStack, not the plain NavigationStack — PopToRootAsync alone never reaches
                // them. Drain the modal stack first, then pop back to the root of whatever regular
                // stack remains (e.g. a PushAsync-based page opened inside a modal).
                INavigation navigation = Shell.Current.Navigation;
                while (navigation.ModalStack.Count > 0)
                    await navigation.PopModalAsync(animated: false);

                await navigation.PopToRootAsync(animated: false);
            }
            finally
            {
                _navigationGate.Release();
            }
        }

        private async void BtnSettings_Tapped(object sender, TappedEventArgs e)
        {
            await BtnSettings.ScaleTo(1.1, 100);
            await BtnSettings.ScaleTo(1.0, 100);

            await Shell.Current.Navigation.PushModalAsync(new EditSettingsView());
        }
    }
}
