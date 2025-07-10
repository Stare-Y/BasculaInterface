namespace BasculaInterface
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            // Register routes for navigation
            Routing.RegisterRoute(nameof(Views.PendingWeightsView), typeof(Views.PendingWeightsView));
        }
    }
}
