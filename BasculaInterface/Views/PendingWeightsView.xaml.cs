using BasculaInterface.ViewModels;

namespace BasculaInterface.Views;

public partial class PendingWeightsView : ContentPage
{
    public PendingWeightsView(PendingWeightsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
    }

    public PendingWeightsView() : this(MauiProgram.ServiceProvider.GetRequiredService<PendingWeightsViewModel>()) { }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is PendingWeightsViewModel viewModel)
        {
            await viewModel.LoadPendingWeightsAsync();
        }
    }
}