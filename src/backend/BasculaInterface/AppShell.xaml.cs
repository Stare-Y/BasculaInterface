using BasculaInterface.Services;

namespace BasculaInterface
{
    public partial class AppShell : Shell
    {
        private readonly InactivityWatcherService _inactivityWatcher;

        public AppShell()
        {
            _inactivityWatcher = MauiProgram.ServiceProvider.GetService(typeof(InactivityWatcherService)) as InactivityWatcherService
                ?? throw new InvalidOperationException("InactivityWatcherService not registered.");

            InitializeComponent();
        }

        // fix-session-inactivity-timeout design.md Decision 1: global activity hook — see the
        // GestureRecognizer's doc comment in AppShell.xaml.
        private void OnAnyTap(object? sender, TappedEventArgs e)
        {
            _inactivityWatcher.RegisterActivity();
        }
    }
}
