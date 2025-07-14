using BasculaInterface.Exceptions;
using BasculaInterface.Models;
using BasculaInterface.ViewModels;
using BasculaInterface.Views.PopUps;
using CommunityToolkit.Maui.Views;
using Core.Application.DTOs;
using Core.Application.Services;

namespace BasculaInterface.Views;

public partial class PendingWeightsView : ContentPage
{
    private WaitPopUp? _popup;

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
            try
            {
                await viewModel.LoadPendingWeightsAsync();
                
                BtnNewWeighProcess.IsVisible = true;
                BtnReconnect.IsVisible = false;
            }
            catch (OriginEmptyException)
            {
                await DisplayAlert("Info", "No hay pesos pendientes, puedes crear uno nuevo :D", "OK");
                BtnNewWeighProcess.IsVisible = true;
                BtnReconnect.IsVisible = false;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "No se pudieron cargar los pesos pendientes: " + ex.Message, "OK");
                BtnReconnect.IsVisible = true;
                BtnNewWeighProcess.IsVisible = false;
            }
        }
    }

    private void DisplayWaitPopUp(string message = "Cargando, espere")
    {
        _popup = new WaitPopUp(message);

        this.ShowPopup(_popup);
    }

    private async void PendingWeightsCollectionView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        PendingWeightViewRow? row = (PendingWeightViewRow)PendingWeightsCollectionView.SelectedItem;

        if (row != null)
        {
            DisplayWaitPopUp("Cargando detalles del peso, espere");
            try
            {
                DetailedWeightViewModel targetViewModel = new DetailedWeightViewModel(MauiProgram.ServiceProvider.GetRequiredService<IApiService>());

                await targetViewModel.LoadProductsAsync(row.WeightEntry, row.Partner);

                DetailedWeightView targetView = new(targetViewModel);

                await Shell.Current.Navigation.PushAsync(targetView);
            }
            finally
            {
                _popup?.Close();
            }
        }

        PendingWeightsCollectionView.SelectedItem = null;
    }

    private async void BtnNewWeighProcess_Clicked(object sender, EventArgs e)
    {
        WeightingScreen weightingView = new(new WeightEntryDto());

        await Shell.Current.Navigation.PushModalAsync(weightingView);
    }

    private async void BtnReconnect_Clicked(object sender, EventArgs e)
    {
        if(BindingContext is PendingWeightsViewModel viewModel)
        {
            DisplayWaitPopUp("Reconectando, espere");
            try
            {
                await viewModel.LoadPendingWeightsAsync();

                BtnNewWeighProcess.IsVisible = true;
                BtnReconnect.IsVisible = false;
            }
            catch (OriginEmptyException)
            {
                await DisplayAlert("Info", "No hay pesos pendientes, puedes crear uno nuevo :D", "OK");
                BtnNewWeighProcess.IsVisible = true;
                BtnReconnect.IsVisible = false;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "No se pudieron cargar los pesos pendientes: " + ex.Message, "OK");
                BtnReconnect.IsVisible = true;
                BtnNewWeighProcess.IsVisible = false;
            }
            finally
            {
                _popup?.Close();
            }
        }
    }
}