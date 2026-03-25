namespace BasculaInterface
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            // Apply saved theme preference (0 = System, 1 = Light, 2 = Dark)
            int savedTheme = Preferences.Get("AppTheme", 0);
            UserAppTheme = savedTheme switch
            {
                1 => AppTheme.Light,
                2 => AppTheme.Dark,
                _ => AppTheme.Unspecified
            };

            MainPage = new AppShell();
        }
    }
}
