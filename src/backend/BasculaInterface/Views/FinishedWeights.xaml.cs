using BasculaInterface.Exceptions;
using BasculaInterface.Models;
using BasculaInterface.ViewModels;
using Core.Application.Services;

namespace BasculaInterface.Views;

public partial class FinishedWeights : ContentPage
{
    private CancellationTokenSource? _cancellationTokenSource = null;

    public FinishedWeights(FinishedWeightsViewModel viewModel)
	{
		InitializeComponent();

        this.BindingContext = viewModel 
            ?? throw new ArgumentNullException(nameof(viewModel));
    }

    public FinishedWeights(): this(MauiProgram.ServiceProvider.GetRequiredService<FinishedWeightsViewModel>()) { }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is FinishedWeightsViewModel viewModel)
        {
            WaitPopUp.Show("Cargando pesos terminados, espere");
            await Task.Yield();
            try
            {
                await viewModel.LoadPendingWeightsAsync();

#if ANDROID
                BtnRefresh.IsVisible = false;
#endif
                BtnReconnect.IsVisible = false;

                return;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "No se pudieron cargar los pesos finalizados: " + ex.Message, "OK");
                BtnReconnect.IsVisible = true;
                BtnRefresh.IsVisible = false;
            }
            finally
            {
                WaitPopUp.Hide();
            }
        }
    }

    private async void BtnReconnect_Pressed(object sender, EventArgs e)
    {
        await BtnRefresh.ScaleTo(1.1, 100);
        await BtnRefresh.ScaleTo(1.0, 100);

        _cancellationTokenSource = new CancellationTokenSource();
        try
        {
            await Task.Delay(4444, _cancellationTokenSource.Token);
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            });
        }
        catch (TaskCanceledException)
        {
            // La tarea fue cancelada, no hacer nada
        }
        catch (Exception ex)
        {
            // Manejar cualquier otra excepci�n
            await DisplayAlert("Error", "Error al tratar de cambiar el host: " + ex.Message, "OK");
        }
    }
    private async void BtnExit_Clicked(object sender, EventArgs e)
    {
        await BtnExitAndroid.ScaleTo(1.1, 100);
        await BtnExitAndroid.ScaleTo(1.0, 100);

        await BtnExit.ScaleTo(1.1, 100);
        await BtnExit.ScaleTo(1.0, 100);

        await Shell.Current.Navigation.PopAsync();
    }

    private async void PendingWeightsCollectionView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        PendingWeightViewRow? row = (PendingWeightViewRow)PendingWeightsCollectionView.SelectedItem;

        if (row != null)
        {
            WaitPopUp.Show("Cargando detalles del peso, espere");
            try
            {
                ReadOnlyDetailedViewModel targetViewModel = new ReadOnlyDetailedViewModel(MauiProgram.ServiceProvider.GetRequiredService<IApiService>());

                await targetViewModel.LoadProductsAsync(row.WeightEntry, row.Partner);

                ReadOnlyDetailedWeightView targetView = new(targetViewModel);

                await Shell.Current.Navigation.PushAsync(targetView);
            }
            finally
            {
                WaitPopUp.Hide();
            }
        }

        PendingWeightsCollectionView.SelectedItem = null;
    }

    private async void BtnReconnect_Released(object sender, EventArgs e)
    {
        BtnReconnect.Opacity = 0;
        await BtnReconnect.FadeTo(2, 200);

        if (_cancellationTokenSource != null)
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null;
            await Reconect();
        }
    }

    private async Task Reconect()
    {
        if (BindingContext is not FinishedWeightsViewModel viewModel)
            return;

        WaitPopUp.Show("Reconectando, espere");

        try
        {
            await viewModel.LoadPendingWeightsAsync();

            BtnReconnect.IsVisible = false;
        }
        catch (OriginEmptyException)
        {
            await DisplayAlert("Info", "No hay pesos pendientes, puedes crear uno nuevo :D", "OK");

            BtnReconnect.IsVisible = false;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "No se pudieron cargar los pesos pendientes: " + ex.Message, "OK");

            BtnReconnect.IsVisible = true;
        }
        finally
        {
            WaitPopUp.Hide();
        }
    }
}