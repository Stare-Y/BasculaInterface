using BasculaInterface.Models;
using BasculaInterface.ViewModels;
using Core.Application.Services;
using Core.Domain.Entities;
using System.Threading.Tasks;

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

    private async void PendingWeightsCollectionView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        PendingWeightViewRow? row = (PendingWeightViewRow)PendingWeightsCollectionView.SelectedItem;

        if (row != null)
        {
            DetailedWeightViewModel targetViewModel = new DetailedWeightViewModel(MauiProgram.ServiceProvider.GetRequiredService<IApiService>());
            
            await targetViewModel.LoadProductsAsync(row.WeightEntry, row.Partner);

            DetailedWeightView targetView = new(targetViewModel);

            await Shell.Current.Navigation.PushModalAsync(targetView);
        }

        PendingWeightsCollectionView.SelectedItem = null;
    }
}