using BasculaInterface.Exceptions;
using BasculaInterface.Models;
using BasculaInterface.ViewModels;
using BasculaInterface.Views.PopUps;
using Core.Application.DTOs;
using Core.Application.Services;

namespace BasculaInterface.Views;

public partial class PendingWeightsView : ContentPage
{
    private CancellationTokenSource? _cancellationTokenSource = null;

    public PendingWeightsView(PendingWeightsViewModel viewModel)
    {
        InitializeComponent();

        BindingContext = viewModel
            ?? throw new ArgumentNullException(nameof(viewModel));

    }

    public PendingWeightsView() : this(MauiProgram.ServiceProvider.GetRequiredService<PendingWeightsViewModel>()) { }

    protected override async void OnAppearing()
    {
        base.OnAppearing();


        if (BindingContext is not PendingWeightsViewModel viewModel)
            return;

        WaitPopUp.Show("Cargando pesos pendientes, espere");
        await Task.Yield();
        try
        {
            EntryHost.Text = Preferences.Get("HostUrl", "bascula.cpe");

            await viewModel.LoadPendingWeightsAsync();

            if (Preferences.Get("ShowDocumentTypeFilter", false))
            {
                await viewModel.LoadExternalTargetBehaviors();

                PickerDocumentType.IsVisible = true;

                PickerDocumentType.SelectedIndex = 0;
            }

            if (Preferences.Get("PreferedDocumentType", null) is string preferedDocumentType)
            {
                int preferedId = int.TryParse(preferedDocumentType, out int result) ? result : 0;

                var index = viewModel.AvailableDocumentTypes.ToList().FindIndex(d => d.Id == preferedId);


                if (PickerDocumentType.IsVisible)
                {
                    if (index >= 0)
                        PickerDocumentType.SelectedIndex = index;
                }

                viewModel.ShowDocumentsWithId(preferedId);
            }

            if (Preferences.Get("SecondaryTerminal", false) || Preferences.Get("OnlyPedidos", false))
            {
                GridListTab.IsVisible = false;

                BtnNewWeighProcess.IsVisible = false;

                if (Preferences.Get("OnlyPedidos", false))
                    BtnNewWeightLessPedido.IsVisible = true;
            }
#if ANDROID
            BtnRefresh.IsVisible = false;
#endif
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

    private async void PendingWeightsCollectionView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
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
            catch (Exception ex)
            {
                await DisplayAlert("Error", "No se pudieron cargar los detalles del peso: " + ex.Message, "OK");
            }
            finally
            {
                PendingWeightsCollectionView.SelectedItem = null;
                WaitPopUp.Hide();
            }
        }

        PendingWeightsCollectionView.SelectedItem = null;
    }

    private async void BtnNewWeighProcess_Clicked(object sender, EventArgs e)
    {
        await BtnNewWeighProcess.ScaleTo(1.1, 100);
        await BtnNewWeighProcess.ScaleTo(1.0, 100);

        WaitPopUp.Show("Preparando bascula, espere...");

        try
        {
            BasculaViewModel basculaViewModel = MauiProgram.ServiceProvider.GetRequiredService<BasculaViewModel>();

            if (!Preferences.Get("BypasTurn", false) && !await basculaViewModel.CanWeight())
            {
                throw new InvalidOperationException("Bascula ocupada");
            }

            WeightingScreen weightingView = new(new WeightEntryDto(), providers: !BtnDescargas.IsEnabled);

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
        if (BindingContext is not PendingWeightsViewModel viewModel)
            return;

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

            if (Preferences.Get("ShowDocumentTypeFilter", false))
            {
                await viewModel.LoadExternalTargetBehaviors();

                PickerDocumentType.IsVisible = true;

                PickerDocumentType.SelectedIndex = 0;
            }

            if (Preferences.Get("PreferedDocumentType", null) is string preferedDocumentType)
            {
                int preferedId = int.TryParse(preferedDocumentType, out int result) ? result : 0;

                var index = viewModel.AvailableDocumentTypes.ToList().FindIndex(d => d.Id == preferedId);


                if (PickerDocumentType.IsVisible)
                {
                    if (index >= 0)
                        PickerDocumentType.SelectedIndex = index;

                    return;
                }

                viewModel.ShowDocumentsWithId(preferedId);
            }


            BtnReconnect.IsVisible = false;
            BorderEntryHost.IsVisible = false;

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
            BtnNewWeighProcess.IsVisible = false;
        }
        finally
        {
            WaitPopUp.Hide();
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
                //BorderEntryHost.IsVisible = true;
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
        await BtnExitAndroid.ScaleTo(1.1, 100);
        await BtnExitAndroid.ScaleTo(1.0, 100);

        await BtnExit.ScaleTo(1.1, 100);
        await BtnExit.ScaleTo(1.0, 100);

        await Shell.Current.Navigation.PopAsync();
    }

    private async void OnBtnNewWeighlessProcess_clicked(object sender, EventArgs e)
    {
        try
        {
            PartnerSelectView partnerSelectView = new PartnerSelectView();

            partnerSelectView.OnPartnerSelected += OnWeightlessPedidoPartnerSelected;

            await Shell.Current.Navigation.PushModalAsync(partnerSelectView);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "No se pudo cargar la pantalla de seleccion de cliente/proveedor: " + ex.Message, "OK");
        }
    }

    private async void OnWeightlessPedidoPartnerSelected(ClienteProveedorDto partner)
    {
        if (BindingContext is not PendingWeightsViewModel viewModel)
            return;

        WaitPopUp.Show("Iniciando pedido simple, espere...");
        try
        {
            string? preferedIdString = Preferences.Get("PreferedDocumentType", null);
            int? preferedExternalTypeId = int.TryParse(preferedIdString, out int result) ? result : null;
            WeightEntryDto weightEntry = new()
            {
                PartnerId = partner.Id,
                TareWeight = 0,
                BruteWeight = 0,
                CreatedAt = DateTime.Now,
                RegisteredBy = DeviceInfo.Name,
                ExternalTargetBehaviorFK = preferedExternalTypeId
            };

            await viewModel.PostNewWeightEntry(weightEntry, partner);

            await Task.Delay(500);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "No se pudo iniciar el proceso de pesaje: " + ex.Message, "OK");
        }
        finally
        {
            WaitPopUp.Hide();
        }
    }

    private void CargasDescargasToggle(object sender, EventArgs e)
    {
        if (BindingContext is not PendingWeightsViewModel viewModel)
            return;

        if (!BtnCargas.IsEnabled)
        {
            BtnCargas.IsEnabled = true;
            BtnDescargas.IsEnabled = false;
            PendingWeightsCollectionView.ItemsSource = viewModel.PendingWeightsDischarge;
        }
        else
        {
            BtnCargas.IsEnabled = false;
            BtnDescargas.IsEnabled = true;
            PendingWeightsCollectionView.ItemsSource = viewModel.PendingWeightsCharge;
        }
    }

    private async void BtnFinished_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.Navigation.PushModalAsync(new FinishedWeights());
    }

    private void PickerDocumentType_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (BindingContext is not PendingWeightsViewModel viewModel)
            return;

        if (PickerDocumentType.SelectedItem is ExternalTargetBehaviorDto selectedDocumentType)
        {
            viewModel.ShowDocumentsWithId(selectedDocumentType.Id);
        }
    }
}