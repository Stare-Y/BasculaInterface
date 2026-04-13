using BasculaInterface.Exceptions;
using BasculaInterface.Models;
using BasculaInterface.ViewModels;
using BasculaInterface.Views.PopUps;
using Core.Application.DTOs;
using Core.Application.Services;
using System.Threading.Tasks;

namespace BasculaInterface.Views;

public partial class PendingWeightsView : ContentPage
{
    private CancellationTokenSource? _cts = null;
    private bool _isFirstLoad = true;

    public PendingWeightsView(PendingWeightsViewModel viewModel)
    {
        // Set BindingContext BEFORE InitializeComponent so XAML bindings resolve correctly
        BindingContext = viewModel
            ?? throw new ArgumentNullException(nameof(viewModel));

        InitializeComponent();

        // Subscribe to Loaded event for first-time initialization
        this.Loaded += OnPageLoaded;

#if WINDOWS
        this.Loaded += (s, e) =>
        {
            var window = this.Window?.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
            if (window?.Content is Microsoft.UI.Xaml.UIElement content)
            {
                content.KeyDown += OnWindowKeyDown;
            }
        };

        this.Unloaded += (s, e) =>
        {
            var window = this.Window?.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
            if (window?.Content is Microsoft.UI.Xaml.UIElement content)
            {
                content.KeyDown -= OnWindowKeyDown;
            }
        };
#endif
    }

#if WINDOWS
    private void OnWindowKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.F5 && BtnNewWeighProcess.IsVisible)
        {
            BtnNewWeighProcess_Clicked(BtnNewWeighProcess, EventArgs.Empty);
            e.Handled = true;
        }
        else if (e.Key == Windows.System.VirtualKey.F6 && BtnFinished.IsVisible)
        {
            BtnFinished_Clicked(BtnFinished, EventArgs.Empty);
            e.Handled = true;
        }
        else if (e.Key == Windows.System.VirtualKey.F8 && BtnNewWeightLessPedido.IsVisible)
        {
            OnBtnNewWeighlessProcess_clicked(BtnNewWeightLessPedido, EventArgs.Empty);
            e.Handled = true;
        }
        else if (e.Key == Windows.System.VirtualKey.Escape)
        {
            BtnExit_Clicked(BtnExit, EventArgs.Empty);
            e.Handled = true;
        }
    }
#endif

    public PendingWeightsView() : this(MauiProgram.ServiceProvider.GetRequiredService<PendingWeightsViewModel>()) { }

    private async void OnPageLoaded(object? sender, EventArgs e)
    {
        // Only run on first load
        if (!_isFirstLoad)
            return;

        _isFirstLoad = false;

        await LoadDataAsync();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Skip if first load (handled by OnPageLoaded)
        if (_isFirstLoad)
            return;

        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        if (BindingContext is not PendingWeightsViewModel viewModel)
            return;

        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        WaitPopUp.Show("Cargando pesos pendientes, espere");

        try
        {
            EntryHost.Text = Preferences.Get("HostUrl", "bascula.cpe");

            await viewModel.LoadPendingWeightsAsync(token);

            if (Preferences.Get("ShowDocumentTypeFilter", false))
            {
                await viewModel.LoadExternalTargetBehaviors(token);

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

                // Filter the collections BEFORE setting ItemsSource
                viewModel.ShowDocumentsWithId(preferedId);
            }

            // Set ItemsSource directly - compiled bindings (x:DataType) handle the rest
            // This must happen AFTER ShowDocumentsWithId to avoid blank rows
            if (!BtnCargas.IsEnabled)
            {
                PendingWeightsCollectionView.ItemsSource = viewModel.PendingWeightsCharge;
            }
            else
            {
                PendingWeightsCollectionView.ItemsSource = viewModel.PendingWeightsDischarge;
            }

            // Workaround for MAUI CollectionView first item sizing bug
#if ANDROID
            await ForceCollectionViewRelayout();
#endif

            if (Preferences.Get("SecondaryTerminal", false) || Preferences.Get("OnlyPedidos", false))
            {
                GridListTab.IsVisible = false;

                BtnNewWeighProcess.IsVisible = false;

                BtnFinished.IsVisible = false;

                if (Preferences.Get("OnlyPedidos", false))
                {
                    BtnNewWeightLessPedido.IsVisible = true;
                    BtnFinished.IsVisible = true;
                }
            }
#if ANDROID
            BtnRefresh.IsVisible = false;
#endif
            BtnReconnect.IsVisible = false;
        }
        catch (OperationCanceledException)
        {
            // Navigation cancelled, ignore
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

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    /// <summary>
    /// Forces the CollectionView to re-measure all items.
    /// Workaround for MAUI bug where items render with incorrect dimensions on first load.
    /// </summary>
    private async Task ForceCollectionViewRelayout()
    {
        await Task.Delay(50);

        await Dispatcher.DispatchAsync(() =>
        {
            // Use ScrollTo to force re-render without breaking compiled bindings
            if (PendingWeightsCollectionView.ItemsSource is System.Collections.IList list && list.Count > 0)
            {
                PendingWeightsCollectionView.ScrollTo(0, position: ScrollToPosition.Start, animate: false);
            }
        });
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

    private async Task Reconect(CancellationToken token = default)
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
        await Task.Yield();
        try
        {
            await viewModel.LoadPendingWeightsAsync(token);

            if (Preferences.Get("ShowDocumentTypeFilter", false))
            {
                await viewModel.LoadExternalTargetBehaviors(token);

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

            BtnReconnect.IsVisible = false;
            BorderEntryHost.IsVisible = false;
        }
        catch (OperationCanceledException)
        {
            // Reconnection cancelled, ignore
            return;
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

        _cts = new CancellationTokenSource();
        try
        {
            await Task.Delay(4444, _cts.Token);
            MainThread.BeginInvokeOnMainThread(() =>
            {
                //BorderEntryHost.IsVisible = true;
                _cts?.Cancel();
                _cts?.Dispose();
                _cts = null;
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

        if (_cts != null)
        {
            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }

        if (string.IsNullOrEmpty(EntryHost.Text))
        {
            await DisplayAlert("Error", "La URL del host no puede estar vacía.", "OK");
            return;
        }

        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        await Reconect(token);
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

            await Reconect();

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
            BtnComprasProveedor.IsVisible = true;
            PendingWeightsCollectionView.ItemsSource = viewModel.PendingWeightsDischarge;
        }
        else
        {
            BtnCargas.IsEnabled = false;
            BtnDescargas.IsEnabled = true;
            BtnComprasProveedor.IsVisible = false;
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

    private async void BtnComprasProveedor_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.Navigation.PushModalAsync(new ProviderPurchaseListView());
    }
}