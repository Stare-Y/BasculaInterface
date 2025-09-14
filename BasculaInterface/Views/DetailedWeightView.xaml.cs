using BasculaInterface.Models;
using BasculaInterface.ViewModels;
using BasculaInterface.Views.PopUps;
using CommunityToolkit.Maui.Views;
using Core.Application.DTOs;

namespace BasculaInterface.Views;

public partial class DetailedWeightView : ContentPage
{
    private bool _entriesChanged = false;
    private WaitPopUp? _popup;
    public DetailedWeightView(DetailedWeightViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        if (MauiProgram.IsSecondaryTerminal)
        {
            BtnNuevoProducto.IsVisible = false;
            BtnNewEntry.IsVisible = false;
            BtnFinishWeight.IsVisible = false;
        }
    }

    public DetailedWeightView() { }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is DetailedWeightViewModel viewModel)
        {
            if (_entriesChanged)
            {
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
            }

            if (!MauiProgram.IsSecondaryTerminal)
            {
                BtnRefresh.IsVisible = true;
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

                    return;
                }
            }

            BtnFinishWeight.IsVisible = false;
        }
    }

    private void DisplayWaitPopUp(string message = "Cargando, espere")
    {
        _popup = new WaitPopUp(message);

        this.ShowPopup(_popup);
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
        await Shell.Current.Navigation.PopAsync();
    }

    private async void BtnNewEntry_Clicked(object sender, EventArgs e)
    {
        DetailedWeightViewModel viewModel = GetViewModel();

        if (viewModel.WeightEntry != null && viewModel.Partner != null)
        {
            WeightingScreen weightingScreen = new WeightingScreen(viewModel.WeightEntry, viewModel.Partner);

            await Shell.Current.Navigation.PushModalAsync(weightingScreen);

            _entriesChanged = true;
        }
    }

    private async void BtnFinishWeight_Clicked(object sender, EventArgs e)
    {
        if (BindingContext is DetailedWeightViewModel viewModel)
        {
            DisplayWaitPopUp("Concluyendo proceso de pesaje, espere...");
            try
            {
                await viewModel.ConcludeWeightProcess();

                await viewModel.PrintTicketAsync(BuildTicket());

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
                _popup?.Close();
            }
        }
    }

    private async void BtnPrintTicket_Clicked(object sender, EventArgs e)
    {
        if (BindingContext is DetailedWeightViewModel viewModel)
        {
            DisplayWaitPopUp("Imprimiendo ticket, espere...");
            try
            {
                await viewModel.PrintTicketAsync(BuildTicket());

                await DisplayAlert("Éxito", "Ticket impreso correctamente.", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "No se pudo imprimir el ticket: " + ex.Message, "OK");
            }
            finally
            {
                _popup?.Close();
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

    private async void OnProductSelected(ProductoDto product)
    {
        if (BindingContext is DetailedWeightViewModel viewModel)
        {
            try
            {
                PickQuantityPopUp quantityPopUp = new PickQuantityPopUp(product.Nombre);
                var result = await this.ShowPopupAsync(quantityPopUp);

                double qty = 0;

                if(result is double pickedQty)
                {
                    qty = pickedQty;
                }

                await viewModel.AddProductToWeightEntry(product, qty);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "No se pudo agregar el producto: " + ex.Message, "OK");
            }
        }
    }

    private async void BtnNuevoProducto_Clicked(object sender, EventArgs e)
    {
        DisplayWaitPopUp("Cargando vista de productos, espere...");
        if (BindingContext is DetailedWeightViewModel viewModel)
        {
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
            finally
            {
                _popup?.Close();
            }
        }
    }

    private async void CollectionViewWeightDetails_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
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

            DisplayWaitPopUp("Preparando bascula, espere...");
            try
            {
                BasculaViewModel basculaViewModel = MauiProgram.ServiceProvider.GetRequiredService<BasculaViewModel>();

                if (!await basculaViewModel.CanWeight())
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

                WeightingScreen weightingScreen = new WeightingScreen(viewModel.WeightEntry!, viewModel.Partner, producto, useIncommingTara: false);

                await Shell.Current.Navigation.PushModalAsync(weightingScreen);

                _entriesChanged = true;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "No se pudo cargar la pantalla de pesaje: " + ex.Message, "OK");
            }
            finally
            {
                _popup?.Close();
            }
        }
        finally
        {
            CollectionViewWeightDetails.SelectedItem = null;
        }
    }

    private async void DeleteWeightDetail_Clicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.BindingContext is WeightEntryDetailRow selectedRow)
        {
            if (BindingContext is DetailedWeightViewModel viewModel)
            {
                DisplayWaitPopUp("Eliminando pesada, espere...");
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
                    _popup?.Close();
                }
            }
        }
    }

    private void BtnRefresh_Clicked(object sender, EventArgs e)
    {
        if (BindingContext is DetailedWeightViewModel viewModel)
        {
            viewModel.IsRefreshing = true;
        }
    }
}