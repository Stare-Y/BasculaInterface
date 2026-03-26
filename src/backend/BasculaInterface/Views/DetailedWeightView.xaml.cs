using BasculaInterface.Models;
using BasculaInterface.ViewModels;
using BasculaInterface.Views.PopUps;
using Core.Application.DTOs;

namespace BasculaInterface.Views;

public partial class DetailedWeightView : ContentPage
{
    private bool _entriesChanged = false;
    private bool _isInitializing = false;
    private CancellationTokenSource? _cts;

    public DetailedWeightView(DetailedWeightViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        SubscribeToKeyboardEvents();

        if (Preferences.Get("SecondaryTerminal", false))
        {
            BtnNuevoProducto.IsVisible = false;
            BtnNewEntry.IsVisible = false;
            BtnDeleteEntry.IsVisible = false;
            BtnPrintTicket.IsVisible = false;
            EntryNotes.IsEnabled = false;
        }
        else if (Preferences.Get("OnlyPedidos", false))
        {
            BtnNuevoProducto.IsVisible = true;
            BtnNewEntry.IsVisible = false;
        }
    }

    public DetailedWeightView() { }

    private void SubscribeToKeyboardEvents()
    {
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
        // Don't handle keys when popup is visible
        if (PickPopUp.IsVisible)
            return;

        if (e.Key == Windows.System.VirtualKey.F1 && BtnNuevoProducto.IsVisible)
        {
            BtnNuevoProducto_Clicked(BtnNuevoProducto, EventArgs.Empty);
            e.Handled = true;
        }
        else if (e.Key == Windows.System.VirtualKey.Escape)
        {
            BtnVolver_Clicked(BtnVolver, EventArgs.Empty);
            e.Handled = true;
        }
    }
#endif

    protected override async void OnAppearing()
    {
        base.OnAppearing();


        if (BindingContext is not DetailedWeightViewModel viewModel)
            return;

        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        if (_entriesChanged)
        {
            WaitPopUp.Show("Un momento...");
            try
            {
                await viewModel.FetchNewWeightDetails(token);

                _entriesChanged = false;
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "No se pudieron actualizar los detalles del peso: " + ex.Message, "OK");
                return;
            }
            finally
            {
                BtnSaveNotes.IsVisible = false;

                WaitPopUp.Hide();
            }
        }

        WaitPopUp.Show("Un momento...");
        try
        {
            await viewModel.LoadExternalTargetBehaviors(token);

            if (viewModel.WeightEntry!.ExternalTargetBehaviorFK is not null && viewModel.WeightEntry!.ExternalTargetBehaviorFK > 0)
            {
                // Set flag to prevent SelectedIndexChanged from triggering API calls
                _isInitializing = true;
                PickerTargetBehavior.SelectedItem = viewModel.ExternalTargetBehaviors.FirstOrDefault(behavior => behavior.Id == viewModel.WeightEntry!.ExternalTargetBehaviorFK);
                _isInitializing = false;
            }
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"No se pudieron obtener los posibles documentos objetivo ({ex.Message}).", "OK");
        }
        finally
        {
            BtnSaveNotes.IsVisible = false;

            WaitPopUp.Hide();
        }

        if (Preferences.Get("SecondaryTerminal", false) || Preferences.Get("OnlyPedidos", false))
        {
            if (Preferences.Get("OnlyPedidos", false))
            {
                if (!(viewModel.WeightEntryDetailRows.Count < 1 && viewModel.WeightEntryDetailRows.Any(row => row.Weight < 1 && row.IsGranel)))
                    BtnFinishWeight.IsVisible = true;

                if (viewModel.Partner is null || viewModel.Partner.Id <= 0)
                    BtnPickPartner.IsVisible = true;
                else
                    BtnPickPartner.IsVisible = false;

                PickerTargetBehavior.IsEnabled = true;

                return;
            }

            PickerTargetBehavior.IsVisible = false;

            return;
        }

        BtnFinishWeight.IsVisible = true;
        PickerTargetBehavior.IsEnabled = true;

        if (viewModel.WeightEntryDetailRows.Count < 1)
        {
            if (Preferences.Get("SecondaryTerminal", false))
                BtnFinishWeight.IsVisible = true;

            if (viewModel.Partner is null || viewModel.Partner.Id <= 0)
                BtnPickPartner.IsVisible = true;
            else
                BtnPickPartner.IsVisible = false;

            return;
        }
        if (viewModel.WeightEntryDetailRows.Any(row => row.Weight < 1 && row.IsGranel))
        {
            BtnFinishWeight.IsVisible = false;
        }
        else
        {
            BtnFinishWeight.IsVisible = true;
        }

        if (viewModel.Partner is null || viewModel.Partner.Id <= 0)
            BtnPickPartner.IsVisible = true;
        else
            BtnPickPartner.IsVisible = false;

        // Workaround for MAUI CollectionView first item sizing bug
        await ForceCollectionViewRelayout();
    }

    /// <summary>
    /// Forces the CollectionView to re-measure all items.
    /// This is a workaround for a known MAUI bug where the first item 
    /// in a CollectionView renders with incorrect dimensions.
    /// </summary>
    private async Task ForceCollectionViewRelayout()
    {
        // Small delay to let the layout system settle
        await Task.Delay(100);

        // Force re-layout by refreshing the ItemsSource binding
        await Dispatcher.DispatchAsync(() =>
        {
            var viewModel = GetViewModel();
            if (viewModel.WeightEntryDetailRows.Count > 0)
            {
                // Save current source and reassign to force re-measure
                var source = CollectionViewWeightDetails.ItemsSource;
                CollectionViewWeightDetails.ItemsSource = null;
                CollectionViewWeightDetails.ItemsSource = source;
            }
        });
    }

    /// <summary>
    /// Handles the Loaded event for the CollectionView.
    /// Forces a re-layout to fix first item sizing issues.
    /// </summary>
    private async void CollectionViewWeightDetails_Loaded(object? sender, EventArgs e)
    {
#if ANDROID
        await ForceCollectionViewRelayout();
#endif
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    private DetailedWeightViewModel GetViewModel()
    {
        if (BindingContext is DetailedWeightViewModel viewModel)
        {
            return viewModel;
        }
        else
        {
            throw new InvalidOperationException("BindingContext is not set to DetailedWeightViewModel.");
        }
    }

    private async void OnSaveNotesClicked(object sender, EventArgs e)
    {
        await BtnSaveNotes.ScaleTo(1.1, 100);
        await BtnSaveNotes.ScaleTo(1.0, 100);

        if (BindingContext is not DetailedWeightViewModel viewModel)
            return;

        if (viewModel.WeightEntry is null)
            return;
        try
        {
            WaitPopUp.Show("Momento...");

            await viewModel.UpdateWeightEntry();
            BtnSaveNotes.IsVisible = false;
            await DisplayAlert("Éxito", "Notas guardadas correctamente.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "No se pudieron guardar las notas: " + ex.Message, "OK");
            return;
        }
        finally
        {
            WaitPopUp.Hide();
        }
    }

    private async void BtnVolver_Clicked(object sender, EventArgs e)
    {
        await BtnVolver.ScaleTo(1.1, 100);
        await BtnVolver.ScaleTo(1.0, 100);

        if (BindingContext is not DetailedWeightViewModel viewModel)
        {
            return;
        }

        await Shell.Current.Navigation.PopAsync();
    }

    private async void BtnNewEntry_Clicked(object sender, EventArgs e)
    {

        await BtnNewEntry.ScaleTo(1.1, 100);
        await BtnNewEntry.ScaleTo(1.0, 100);

        try
        {
            WaitPopUp.Show("Preparando bascula...");

            DetailedWeightViewModel viewModel = GetViewModel();

            if (viewModel.WeightEntry != null && viewModel.Partner != null)
            {
                WeightingScreen weightingScreen = new WeightingScreen(viewModel.WeightEntry, viewModel.Partner);

                if (weightingScreen.BindingContext is not BasculaViewModel basculaViewModel)
                    throw new InvalidOperationException("No se pudo validar el estado de la bascuila en el VM");

                if (!Preferences.Get("BypasTurn", false) && !await basculaViewModel.CanWeight())
                {
                    throw new InvalidOperationException("Bascula ocupada");
                }

                await Shell.Current.Navigation.PushModalAsync(weightingScreen);

                _entriesChanged = true;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "Error inesperado al tratar de entrar a la bascula: " + ex.Message, "OK");
            return;
        }
        finally
        {
            WaitPopUp.Hide();
        }
    }

    private async void BtnFinishWeight_Clicked(object sender, EventArgs e)
    {
        await BtnFinishWeight.ScaleTo(1.1, 100);
        await BtnFinishWeight.ScaleTo(1.0, 100);

        if (BindingContext is DetailedWeightViewModel viewModel)
        {

            WaitPopUp.Show("Concluyendo proceso de pesaje, espere...");
            try
            {
                await viewModel.ConcludeWeightProcess();

                await DisplayAlert("Éxito", "Proceso de pesaje concluido correctamente.", "OK");

                await Shell.Current.Navigation.PopAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Error inesperado al concluir el proceso: " + ex.Message, "OK");
                return;
            }
            finally
            {
                WaitPopUp.Hide();
            }
        }
    }

    private void OnNotesTextChanged(object sender, TextChangedEventArgs e)
    {
        if (BindingContext is not DetailedWeightViewModel viewModel)
            return;

        if (viewModel.WeightEntry is null)
            return;

        viewModel.WeightEntry.Notes = e.NewTextValue;
        BtnSaveNotes.IsVisible = true;
    }

    private async void BtnPrintTicket_Clicked(object sender, EventArgs e)
    {
        await BtnPrintTicket.ScaleTo(1.1, 100);
        await BtnPrintTicket.ScaleTo(1.0, 100);

        if (BindingContext is DetailedWeightViewModel viewModel)
        {
            WaitPopUp.Show("Imprimiendo ticket, espere...");
            try
            {
                await viewModel.PrintTicketAsync();

                await DisplayAlert("Éxito", "Ticket impreso correctamente.", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "No se pudo imprimir el ticket: " + ex.Message, "OK");
            }
            finally
            {
                WaitPopUp.Hide();
            }
        }
    }

    private string BuildTicket()
    {
        if (BindingContext is DetailedWeightViewModel viewModel)
        {
            string header = $"\n\n\tCooperativa\n\tPedro\n\tEzqueda\n\n" +
                $"Socio: {(viewModel.Partner?.RazonSocial.Length > 13 ? viewModel.Partner?.RazonSocial.Substring(0, 15) : viewModel.Partner?.RazonSocial),-15}\n" +
                $"Placas: {viewModel.WeightEntry?.VehiclePlate}\n\n" +
                $"Tara: {viewModel.WeightEntry?.TareWeight} kg\n" +
                $"Bruto: {viewModel.WeightEntry?.BruteWeight} kg\n\n";

            string details = string.Join("\n", viewModel.WeightEntryDetailRows.Select(row => $"{(row.Description.Length > 15 ? row.Description.Substring(0, 15) : row.Description),-15}|{row.Weight} kg"));

            details += $"\n\nNeto: {viewModel.WeightEntryDetailRows.Sum(row => row.Weight)} kg";

            return header + details;
        }

        return "error generando ticket";
    }

    private async void OnPartnerSelected(ClienteProveedorDto partner)
    {
        if (BindingContext is not DetailedWeightViewModel viewModel)
            return;

        if (viewModel.WeightEntry is null)
        {
            await DisplayAlert("Error", "La entrada de peso no está inicializada.", "OK");
            return;
        }

        viewModel.Partner = partner;
        viewModel.WeightEntry.PartnerId = partner.Id;

        try
        {
            await viewModel.UpdateWeightEntry();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "No se pudo actualizar la entrada de peso con el socio seleccionado: " + ex.Message, "OK");
            return;
        }

        BtnPickPartner.IsVisible = false;
    }

    private async void BtnPickPartner_Clicked(object sender, EventArgs e)
    {
        if (BindingContext is not DetailedWeightViewModel viewModel)
            return;

        await BtnPickPartner.ScaleTo(1.1, 100);
        await BtnPickPartner.ScaleTo(1.0, 100);

        PartnerSelectView partnerSelectView = new PartnerSelectView();
        partnerSelectView.OnPartnerSelected += OnPartnerSelected;
        await Shell.Current.Navigation.PushModalAsync(partnerSelectView);
    }

    private async void OnProductSelected(ProductoDto product)
    {
        if (BindingContext is not DetailedWeightViewModel viewModel)
        {
            await DisplayAlert("Error", "El contexto de enlace no es correcto.", "OK");
            return;
        }
        try
        {
            if (viewModel.Partner is null || viewModel.Partner.Id <= 0)
            {
                throw new InvalidOperationException("No se ha seleccionado un socio, eso se debe hacer primero.");
            }

            Dictionary<double, int?> result = await PickPopUp.ShowAsync(product.Nombre);

            double qty = 0;

            if (result.Keys.First() is double pickedQty)
            {
                qty = pickedQty;
            }

            double composedCost = (qty * product.Precio) + viewModel.TotalCost;

            // Quick local check - skip API call if partner ignores credit limit or has no limit
            if (viewModel.Partner.IgnoreCreditLimit || viewModel.Partner.CreditLimit <= 0)
            {
                // Partner ignores credit limit or has no limit configured, skip validation
            }
            else
            {
                // First do a quick local check against available credit
                if (composedCost > viewModel.Partner.AvailableCredit)
                {
                    throw new InvalidOperationException("No hay suficiente crédito para agregar este producto.");
                }

                // If local check passes, validate with API (considers other pending weight entries)
                WaitPopUp.Show("Validando credito...");
                try
                {
                    CreditValidationResponse creditValidation = await viewModel.ValidatePartnerCreditAsync(qty * product.Precio);

                    if (!creditValidation.IsValid)
                    {
                        throw new InvalidOperationException("No hay suficiente crédito para agregar este producto.");
                    }
                }
                finally
                {
                    WaitPopUp.Hide();
                }
            }

            // Show loading screen while adding product and refreshing data
            WaitPopUp.Show("Agregando producto...");
            try
            {
                await viewModel.AddProductToWeightEntry(product, qty, result[result.Keys.First()]);
            }
            finally
            {
                WaitPopUp.Hide();
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "No se pudo agregar el producto: \n" + ex.Message, "OK");
        }
    }

    private async void BtnNuevoProducto_Clicked(object sender, EventArgs e)
    {
        await BtnNuevoProducto.ScaleTo(1.1, 100);
        await BtnNuevoProducto.ScaleTo(1.0, 100);

        if (BindingContext is not DetailedWeightViewModel viewModel)
        {
            await DisplayAlert("Error", "El contexto de enlace no es correcto.", "OK");
            return;
        }

        if (viewModel.Partner is null || viewModel.Partner.Id <= 0)
        {
            await DisplayAlert("Error", "No se ha seleccionado un socio, eso se debe hacer primero", "OK");
            return;
        }

        if (
            viewModel.WeightEntry is not null &&
            (viewModel.WeightEntry.ExternalTargetBehaviorFK is null ||
            viewModel.WeightEntry.ExternalTargetBehaviorFK <= 0))
        {
            await DisplayAlert("Error", "Primero selecciona el TIPO DE DOCUMENTO", "Ok");
            return;
        }

        try
        {
            ProductSelectView productSelectView = new ProductSelectView();

            productSelectView.OnProductSelected += OnProductSelected;

            await Shell.Current.Navigation.PushModalAsync(productSelectView);

            _entriesChanged = true;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "No se pudo cargar la selección de productos: " + ex.Message, "OK");
        }
    }

    private async void CollectionViewWeightDetails_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (Preferences.Get("OnlyPedidos", false))
        {
            return;
        }
        WeightEntryDetailRow? row = (WeightEntryDetailRow)CollectionViewWeightDetails.SelectedItem;

        if (BindingContext is not DetailedWeightViewModel viewModel)
        {
            CollectionViewWeightDetails.SelectedItem = null;
            return;
        }

        try
        {
            if (row is null)
                return;

            if (row.Tare > 0)
                return;

            if (!row.IsGranel)
                return;

            if (
                !Preferences.Get("BypasTurn", false) &&
                ((!string.IsNullOrEmpty(row.WeightedBy) || !string.IsNullOrWhiteSpace(row.WeightedBy)) &&
                row.WeightedBy.Trim().ToLower() != DeviceInfo.Name.Trim().ToLower()))
            {
                await DisplayAlert("Error", $"El pesaje ya lo esta llevando {row.WeightedBy}.", "Ok");
                return;
            }

            WaitPopUp.Show("Preparando bascula, espere...");
            try
            {
                BasculaViewModel basculaViewModel = MauiProgram.ServiceProvider.GetRequiredService<BasculaViewModel>();

                if (!Preferences.Get("BypasTurn", false) && !await basculaViewModel.CanWeight())
                {
                    throw new InvalidOperationException("Bascula ocupada");
                }

                ProductoDto? producto = null;
                if (row.FK_WeightedProductId.HasValue)
                {
                    producto = new ProductoDto
                    {
                        Id = row.FK_WeightedProductId.Value,
                        Nombre = row.Description
                    };
                }

                WeightingScreen weightingScreen = new(viewModel.WeightEntry!, viewModel.Partner, producto, useIncommingTara: false);

                await Shell.Current.Navigation.PushModalAsync(weightingScreen);

                _entriesChanged = true;
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
        finally
        {
            CollectionViewWeightDetails.SelectedItem = null;
        }
    }

    private async void DeleteWeightDetail_Clicked(object sender, EventArgs e)
    {
        //await BtnWeightDetail.FadeTo(1, 200);

        if (sender is Button btn && btn.BindingContext is WeightEntryDetailRow selectedRow)
        {
            if (BindingContext is not DetailedWeightViewModel viewModel)
                return;

            WaitPopUp.Show("Eliminando pesada, espere...");
            try
            {

                await viewModel.RemoveWeightEntryDetail(selectedRow);

                if (viewModel.WeightEntryDetailRows.Count > 0)
                {
                    if (viewModel.WeightEntryDetailRows.Any(row => row.Weight < 1))
                    {
                        BtnFinishWeight.IsVisible = false;
                    }
                    else
                    {
                        BtnFinishWeight.IsVisible = true;
                    }
                }

                await DisplayAlert("Éxito", "Registro eliminado correctamente.", "OK");

                _entriesChanged = true;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "No se pudo eliminar la pesada: " + ex.Message, "OK");
            }
            finally
            {
                WaitPopUp.Hide();
            }
        }
    }

    private async void BtnRefresh_Clicked(object sender, EventArgs e)
    {
        await BtnRefresh.ScaleTo(1.1, 100);
        await BtnRefresh.ScaleTo(1.0, 100);

        if (BindingContext is DetailedWeightViewModel viewModel)
        {
            viewModel.IsRefreshing = true;
        }
    }

    private async void BtnDeleteEntry_Clicked(object sender, EventArgs e)
    {
        await BtnDeleteEntry.ScaleTo(1.1, 100);
        await BtnDeleteEntry.ScaleTo(1.0, 100);

        if (BindingContext is not DetailedWeightViewModel viewModel)
        {
            return;
        }

        bool confirmed = await DisplayAlert("Confirmación", "¿Estás seguro de que deseas eliminar toda la entrada de peso? Esta acción no se puede deshacer.", "No", "Si");

        if (confirmed)
            return;

        WaitPopUp.Show("Eliminando entrada de peso, espere...");
        try
        {
            await viewModel.DeleteWeightEntry();

            await Shell.Current.Navigation.PopAsync();
        }
        finally
        {
            WaitPopUp.Hide();
        }
    }

    private async void PickerTargetBehavior_SelectedIndexChanged(object sender, EventArgs e)
    {
        // Skip if we're programmatically setting the picker during initialization
        if (_isInitializing)
            return;

        if (BindingContext is not DetailedWeightViewModel viewModel)
        { return; }

        WaitPopUp.Show("Actualizando Documento Objetivo...");
        try
        {
            ExternalTargetBehaviorDto? selectedItem = PickerTargetBehavior.SelectedItem as ExternalTargetBehaviorDto;

            if (selectedItem is null) { return; }

            viewModel.WeightEntry!.ExternalTargetBehaviorFK = selectedItem.Id;

            await viewModel.UpdateWeightEntry();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            WaitPopUp.Hide();
        }
    }
}