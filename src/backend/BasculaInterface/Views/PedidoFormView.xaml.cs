using BasculaInterface.Models;
using BasculaInterface.ViewModels;
using Core.Application.DTOs;
using Core.Application.Services;

namespace BasculaInterface.Views;

public partial class PedidoFormView : ContentPage
{
    private PedidoFormViewModel ViewModel => (PedidoFormViewModel)BindingContext;

    public PedidoFormView(PedidoFormViewModel viewModel)
    {
        BindingContext = viewModel
            ?? throw new ArgumentNullException(nameof(viewModel));

        InitializeComponent();

        this.Loaded += OnPageLoaded;
    }

    public PedidoFormView()
        : this(new PedidoFormViewModel(
            MauiProgram.ServiceProvider.GetRequiredService<IApiService>())) { }

    private async void OnPageLoaded(object? sender, EventArgs e)
    {
        if (!ViewModel.IsEditing)
        {
            LblTitle.Text = "Nuevo Pedido";
            DatePickerExpectedArrival.Date = DateTime.Today.AddDays(1);
            return;
        }

        LblTitle.Text = "Editar Pedido";

        bool isConcluded = ViewModel.Pedido.Concluded;

        LblStatus.IsVisible = true;
        LblStatus.Text = isConcluded ? "Completado" : "Pendiente";
        LblStatus.TextColor = isConcluded ? Colors.Green : Colors.Gray;

        BtnDelete.IsVisible = !isConcluded;
        LinesSection.IsVisible = true;

        if (isConcluded)
        {
            BtnPickProvider.IsEnabled = false;
            DatePickerExpectedArrival.IsEnabled = false;
            EditorNotes.IsEnabled = false;
            BtnSave.IsVisible = false;
            BtnPickNewLineProduct.IsEnabled = false;
            BtnAddLine.IsEnabled = false;
        }

        WaitPopUp.Show("Cargando datos, espere");
        try
        {
            await ViewModel.LoadProviderByIdAsync(ViewModel.Pedido.ProviderId);
            LblProviderName.Text = ViewModel.SelectedProvider?.RazonSocial ?? "Proveedor no encontrado";

            DatePickerExpectedArrival.Date = ViewModel.Pedido.ExpectedArrival.ToLocalTime();
            EditorNotes.Text = ViewModel.Pedido.Notes ?? string.Empty;

            await ViewModel.LoadAlmacenTargetsAsync();
            await ViewModel.ReloadAsync();
            LinesCollectionView.ItemsSource = ViewModel.LineRows;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "No se pudieron cargar los datos: " + ex.Message, "OK");
        }
        finally
        {
            WaitPopUp.Hide();
        }
    }

    private void OnProviderSelected(ClienteProveedorDto provider)
    {
        ViewModel.SelectedProvider = provider;
        LblProviderName.Text = provider.RazonSocial;
    }

    private async void BtnPickProvider_Clicked(object sender, EventArgs e)
    {
        await BtnPickProvider.ScaleTo(1.1, 100);
        await BtnPickProvider.ScaleTo(1.0, 100);

        PartnerSelectView partnerSelectView = new PartnerSelectView(providers: true);
        partnerSelectView.OnPartnerSelected += OnProviderSelected;
        await Shell.Current.Navigation.PushModalAsync(partnerSelectView);
    }

    private void OnNewLineProductSelected(ProductoDto product)
    {
        ViewModel.NewLineProduct = product;
        LblNewLineProduct.Text = string.IsNullOrEmpty(product.Code)
            ? product.Nombre
            : $"{product.Code} - {product.Nombre}";
    }

    private async void BtnPickNewLineProduct_Clicked(object sender, EventArgs e)
    {
        await BtnPickNewLineProduct.ScaleTo(1.1, 100);
        await BtnPickNewLineProduct.ScaleTo(1.0, 100);

        ProductSelectView productSelectView = new ProductSelectView();
        productSelectView.OnProductSelected += OnNewLineProductSelected;
        await Shell.Current.Navigation.PushModalAsync(productSelectView);
    }

    private async void BtnSave_Clicked(object sender, EventArgs e)
    {
        if (ViewModel.SelectedProvider == null)
        {
            await DisplayAlert("Error", "Seleccione un proveedor.", "OK");
            return;
        }

        ViewModel.Pedido.ExpectedArrival = DatePickerExpectedArrival.Date.ToUniversalTime();
        ViewModel.Pedido.Notes = string.IsNullOrWhiteSpace(EditorNotes.Text) ? null : EditorNotes.Text.Trim();

        bool wasEditing = ViewModel.IsEditing;

        WaitPopUp.Show("Guardando pedido, espere");
        try
        {
            await ViewModel.SaveHeaderAsync();

            await DisplayAlert("Éxito", wasEditing ? "Pedido actualizado." : "Pedido creado. Ahora puede agregar productos.", "OK");

            if (!wasEditing)
            {
                // Newly created header — reveal the lines section so products can be added
                // without leaving the page (mirrors how a fresh pedido must exist server-side
                // before a PedidoLine can reference it).
                LblTitle.Text = "Editar Pedido";
                BtnDelete.IsVisible = true;
                LinesSection.IsVisible = true;
                await ViewModel.LoadAlmacenTargetsAsync();
                LinesCollectionView.ItemsSource = ViewModel.LineRows;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "No se pudo guardar el pedido: " + ex.Message, "OK");
        }
        finally
        {
            WaitPopUp.Hide();
        }
    }

    private async void BtnDelete_Clicked(object sender, EventArgs e)
    {
        bool confirmed = await DisplayAlert("Confirmar", "¿Deseas eliminar este pedido?", "Sí", "No");
        if (!confirmed)
            return;

        WaitPopUp.Show("Eliminando pedido, espere");
        try
        {
            await ViewModel.DeletePedidoAsync();

            await DisplayAlert("Éxito", "Pedido eliminado.", "OK");

            await Shell.Current.Navigation.PopAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "No se pudo eliminar el pedido: " + ex.Message, "OK");
        }
        finally
        {
            WaitPopUp.Hide();
        }
    }

    private async void BtnAddLine_Clicked(object sender, EventArgs e)
    {
        if (ViewModel.NewLineProduct == null)
        {
            await DisplayAlert("Error", "Seleccione un producto.", "OK");
            return;
        }

        if (!decimal.TryParse(EntryNewLineAmount.Text, out decimal amount) || amount <= 0)
        {
            await DisplayAlert("Error", "Ingrese una cantidad válida mayor a 0.", "OK");
            return;
        }

        decimal? price = decimal.TryParse(EntryNewLinePrice.Text, out decimal parsedPrice) ? parsedPrice : null;

        WaitPopUp.Show("Agregando producto, espere");
        try
        {
            await ViewModel.AddLineAsync(ViewModel.NewLineProduct.Id, amount, price, null, ChkNewLineDisTaring.IsChecked);

            LinesCollectionView.ItemsSource = null;
            LinesCollectionView.ItemsSource = ViewModel.LineRows;

            ViewModel.NewLineProduct = null;
            LblNewLineProduct.Text = "Sin producto seleccionado";
            EntryNewLineAmount.Text = string.Empty;
            EntryNewLinePrice.Text = string.Empty;
            ChkNewLineDisTaring.IsChecked = false;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "No se pudo agregar el producto: " + ex.Message, "OK");
        }
        finally
        {
            WaitPopUp.Hide();
        }
    }

    private async void BtnConvertLine_Clicked(object sender, EventArgs e)
    {
        if (sender is not Button button || button.CommandParameter is not PedidoLineViewRow row)
            return;

        if (ViewModel.AlmacenTargets.Count == 0)
        {
            await DisplayAlert("Almacenes no configurados",
                "No hay almacenes destino configurados. Configure los ExternalTargetBehavior ocultos antes de convertir líneas a peso.", "OK");
            return;
        }

        var result = await ConvertPopUp.ShowAsync(
            row.Line.PendingAmount, ViewModel.AlmacenTargets, row.DefaultAlmacenCode);
        if (result is null)
            return;

        WaitPopUp.Show("Convirtiendo a peso, espere");
        try
        {
            await ViewModel.ConvertLineToWeightAsync(
                row.Line.Id, result.Value.TargetAmount, result.Value.AlmacenTargetId);

            LinesCollectionView.ItemsSource = null;
            LinesCollectionView.ItemsSource = ViewModel.LineRows;

            await DisplayAlert("Éxito", "Se creó la entrada de peso. Aparecerá en pesos pendientes.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "No se pudo convertir la línea a peso: " + ex.Message, "OK");
        }
        finally
        {
            WaitPopUp.Hide();
        }
    }

    private async void BtnCloseLine_Clicked(object sender, EventArgs e)
    {
        if (sender is not Button button || button.CommandParameter is not PedidoLineViewRow row)
            return;

        bool confirmed = await DisplayAlert("Confirmar",
            $"¿Cerrar la línea de \"{row.ProductName}\" con {row.Line.PendingAmount:N2} kg aún pendientes? Esto acepta el envío como incompleto.",
            "Sí", "No");
        if (!confirmed)
            return;

        WaitPopUp.Show("Cerrando línea, espere");
        try
        {
            await ViewModel.CloseLineAsync(row.Line.Id);

            LinesCollectionView.ItemsSource = null;
            LinesCollectionView.ItemsSource = ViewModel.LineRows;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "No se pudo cerrar la línea: " + ex.Message, "OK");
        }
        finally
        {
            WaitPopUp.Hide();
        }
    }

    private async void BtnCancel_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.Navigation.PopAsync();
    }
}
