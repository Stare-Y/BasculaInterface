using BasculaInterface.Exceptions;
using BasculaInterface.Models;
using BasculaInterface.ViewModels;
using BasculaInterface.Views.PopUps;
using Core.Application.DTOs;
using Core.Application.Services;

namespace BasculaInterface.Views;

public partial class PendingWeightsView : ContentPage
{
    //private WaitPopUp? _popup;
    private CancellationTokenSource? _cancellationTokenSource = null;

    public PendingWeightsView(PendingWeightsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        BtnNewWeighProcess.IsVisible = !Preferences.Get("OnlyPedidos", false);
    }

    public PendingWeightsView() : this(MauiProgram.ServiceProvider.GetRequiredService<PendingWeightsViewModel>()) { }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is PendingWeightsViewModel viewModel)
        {
            WaitPopUp.Show("Cargando pesos pendientes, espere");
            await Task.Yield();
            try
            {
                EntryHost.Text = Preferences.Get("HostUrl", "bascula.cpe");

                await viewModel.LoadPendingWeightsAsync();

                if (!MauiProgram.IsSecondaryTerminal)
                {
                    BtnNewWeighProcess.IsVisible = !Preferences.Get("OnlyPedidos", false);
                    BtnRefresh.IsVisible = true;
                }

                BtnReconnect.IsVisible = false;

                return;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "No se pudieron cargar los pesos pendientes: " + ex.Message, "OK");
                BtnReconnect.IsVisible = true;
                BtnRefresh.IsVisible = false;
                BtnNewWeighProcess.IsVisible = false;
            }
            finally
            {
                WaitPopUp.Hide();
            }
        }
    }

    private async void PendingWeightsCollectionView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        //BtnSeleccionar.Opacity = 0; 
        //await BtnSeleccionar.FadeTo(1, 200);

        PendingWeightViewRow? row = (PendingWeightViewRow)PendingWeightsCollectionView.SelectedItem;

        if (row != null)
        {
            WaitPopUp.Show("Cargando detalles del peso, espere");
            try
            {
                DetailedWeightViewModel targetViewModel = new DetailedWeightViewModel(MauiProgram.ServiceProvider.GetRequiredService<IApiService>());

                await targetViewModel.LoadProductsAsync(row.WeightEntry, row.Partner);

                DetailedWeightView targetView = new(targetViewModel);

                await Shell.Current.Navigation.PushAsync(targetView);
            }
            finally
            {
                WaitPopUp.Hide();
            }
        }

        PendingWeightsCollectionView.SelectedItem = null;
    }

    private async void BtnNewWeighProcess_Clicked(object sender, EventArgs e)
    {
        BtnNewWeighProcess.Opacity = 0;
        await BtnNewWeighProcess.FadeTo(1, 200);

        WaitPopUp.Show("Preparando bascula, espere...");

        try
        {
            BasculaViewModel basculaViewModel = MauiProgram.ServiceProvider.GetRequiredService<BasculaViewModel>();

            if (!await basculaViewModel.CanWeight())
            {
                throw new InvalidOperationException("Bascula ocupada");
            }

            WeightingScreen weightingView = new(new WeightEntryDto());

            await Shell.Current.Navigation.PushModalAsync(weightingView);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "No se pudo cargar la pantalla de pesaje: " + ex.Message, "OK");
        }
        finally
        {
            WaitPopUp.Hide();
        }
    }

    private async Task Reconect()
    {
        if (BindingContext is PendingWeightsViewModel viewModel)
        {
            if (EntryHost.Text.Contains("http"))
            {
                Preferences.Set("HostUrl", EntryHost.Text);
            }
            else
            {
                Preferences.Set("HostUrl", "http://" + EntryHost.Text + "/");
            }

            WaitPopUp.Show("Reconectando, espere");
            try
            {
                await viewModel.LoadPendingWeightsAsync();

                if (!MauiProgram.IsSecondaryTerminal)
                {
                    BtnNewWeighProcess.IsVisible = true;
                }

                BtnReconnect.IsVisible = false;
                BorderEntryHost.IsVisible = false;

            }
            catch (OriginEmptyException)
            {
                await DisplayAlert("Info", "No hay pesos pendientes, puedes crear uno nuevo :D", "OK");
                if (!MauiProgram.IsSecondaryTerminal)
                {
                    BtnNewWeighProcess.IsVisible = true;

                }

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
                WaitPopUp.Hide();
            }
        }
    }

    private async void BtnReconnect_Pressed(object sender, EventArgs e)
    {
        BtnRefresh.Opacity = 0;
        await BtnRefresh.FadeTo(1, 200);

        _cancellationTokenSource = new CancellationTokenSource();
        try
        {
            await Task.Delay(4444, _cancellationTokenSource.Token);
            MainThread.BeginInvokeOnMainThread(() =>
            {
                BorderEntryHost.IsVisible = true;

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

    private async void BtnReconnect_Released(object sender, EventArgs e)
    {
        BtnReconnect.Opacity = 0;
        await BtnReconnect.FadeTo(2, 200);

        if (_cancellationTokenSource != null)
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null;
            if (string.IsNullOrEmpty(EntryHost.Text))
            {
                await DisplayAlert("Error", "La URL del host no puede estar vac�a.", "OK");
                return;
            }
            await Reconect();
        }
    }

    private async void BtnExit_Clicked(object sender, EventArgs e)
    {
        BtnExitAndroid.Opacity = 0;
        await BtnExitAndroid.FadeTo(1, 200);

        BtnExit.Opacity = 0;
        await BtnExit.FadeTo(1, 200);

        await Shell.Current.Navigation.PopAsync();
    }
}