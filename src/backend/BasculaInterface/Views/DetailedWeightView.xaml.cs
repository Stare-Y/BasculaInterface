using BasculaInterface.Models;
using BasculaInterface.ViewModels;
using BasculaInterface.Views.PopUps;
using Core.Application.DTOs;

namespace BasculaInterface.Views;

public partial class DetailedWeightView : ContentPage
{
    private bool _entriesChanged = false;

    public DetailedWeightView(DetailedWeightViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        if (Preferences.Get("SecondaryTerminal", false))
        {
            BtnNuevoProducto.IsVisible = false;
            BtnNewEntry.IsVisible = false;
            BtnDeleteEntry.IsVisible = false;
        }
        else if (Preferences.Get("OnlyPedidos", false))
        {
            BtnNuevoProducto.IsVisible = true;
            BtnNewEntry.IsVisible = false;
        }
    }

    public DetailedWeightView() { }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is not DetailedWeightViewModel viewModel)
            return;

        if (_entriesChanged)
        {
            WaitPopUp.Show("Un momento...");
            try
            {
                await viewModel.FetchNewWeightDetails();
                _entriesChanged = false;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "No se pudieron actualizar los detalles del peso: " + ex.Message, "OK");
                return;
            }
            finally
            {
                WaitPopUp.Hide();
            }
        }

        if (Preferences.Get("SecondaryTerminal", false) || Preferences.Get("OnlyPedidos", false))
        {
            if (Preferences.Get("OnlyPedidos", false))
            {
                if (!(viewModel.WeightEntryDetailRows.Count < 1 && viewModel.WeightEntryDetailRows.Any(row => row.Weight < 1)))
                    BtnFinishWeight.IsVisible = true;

                //if(viewModel.Partner is null || viewModel.Partner.Id <= 0)
                //    BtnPickPartner.IsVisible = true;
            }
            return;
        }

        if (viewModel.WeightEntryDetailRows.Count < 1)
        {
            if (!Preferences.Get("SecondaryTerminal", false))
                BtnFinishWeight.IsVisible = true;

            return;
        }
        if (viewModel.WeightEntryDetailRows.Any(row => row.Weight < 1))
        {
            BtnFinishWeight.IsVisible = false;
        }
        else
        {
            BtnFinishWeight.IsVisible = true;
        }

        //if(viewModel.Partner is null || viewModel.Partner.Id <= 0)
        //    BtnPickPartner.IsVisible = true;
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

    private async void BtnVolver_Clicked(object sender, EventArgs e)
    {
        await BtnVolver.ScaleTo(1.1, 100);
        await BtnVolver.ScaleTo(1.0, 100);

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

                await viewModel.PrintTicketAsync();

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

        viewModel.Partner = partner;

        await viewModel.UpdateWeightEntry();

        // BtnPickPartner.IsVisible = false;
    }

    private async void BtnPickPartner_Clicked(object sender, EventArgs e)
    {
        if (BindingContext is not BasculaViewModel viewModel)
            return;

        // await BtnPickPartner.ScaleTo(1.1, 100);
        // await BtnPickPartner.ScaleTo(1.0, 100);

        PartnerSelectView partnerSelectView = new PartnerSelectView(viewModel.Providers);
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
            /*ickQuantityPopUp quantityPopUp = new PickQuantityPopUp(product.Nombre);*/

            double? result = await PickPopUp.ShowAsync(product.Nombre);

            double qty = 0;

            if (result is double pickedQty)
            {
                qty = pickedQty;
            }

            double composedCost = (qty * product.Precio) + viewModel.TotalCost;

            if (!viewModel.Partner.IgnoreCreditLimit && viewModel.Partner.CreditLimit > 0 && composedCost > viewModel.Partner.AvailableCredit)
            {
                throw new InvalidOperationException($"No hay suficiente credito para agregar este producto con la cantidad  seleccionada (excede por ${composedCost - viewModel.Partner?.AvailableCredit}).");
            }

            await viewModel.AddProductToWeightEntry(product, qty);
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
            {
                return;
            }

            if (!string.IsNullOrEmpty(row.WeightedBy) && row.WeightedBy != DeviceInfo.Name)
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
            if (BindingContext is DetailedWeightViewModel viewModel)
            {
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
}