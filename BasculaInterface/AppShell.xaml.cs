namespace BasculaInterface
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            // Register routes for navigation
            Routing.RegisterRoute(nameof(Views.PendingWeightsView), typeof(Views.PendingWeightsView));
            Routing.RegisterRoute(nameof(Views.DetailedWeightView), typeof(Views.DetailedWeightView));
            Routing.RegisterRoute(nameof(Views.WeightingScreen), typeof(Views.WeightingScreen));
        }
    }
}
