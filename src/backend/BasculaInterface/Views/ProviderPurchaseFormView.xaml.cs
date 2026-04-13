using BasculaInterface.ViewModels;
using Core.Application.DTOs;
using Core.Application.Services;

namespace BasculaInterface.Views;

public partial class ProviderPurchaseFormView : ContentPage
{
    private ProviderPurchaseFormViewModel ViewModel => (ProviderPurchaseFormViewModel)BindingContext;

    public ProviderPurchaseFormView(ProviderPurchaseFormViewModel viewModel)
    {
        BindingContext = viewModel
            ?? throw new ArgumentNullException(nameof(viewModel));

        InitializeComponent();

        this.Loaded += OnPageLoaded;
    }

    public ProviderPurchaseFormView()
        : this(new ProviderPurchaseFormViewModel(
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
        BtnDelete.IsVisible = true;

        WaitPopUp.Show("Cargando datos, espere");
        try
        {
            await ViewModel.LoadProviderByIdAsync(ViewModel.Purchase.ProviderId);
            LblProviderName.Text = ViewModel.SelectedProvider?.RazonSocial ?? "Proveedor no encontrado";

            await ViewModel.LoadProductByIdAsync(ViewModel.Purchase.ProductId);
            LblProductName.Text = ViewModel.SelectedProduct?.Nombre ?? "Producto no encontrado";

            EntryRequiredAmount.Text = ViewModel.Purchase.RequiredAmount.ToString("F2");
            DatePickerExpectedArrival.Date = ViewModel.Purchase.ExpectedArrival.ToLocalTime();
            EditorNotes.Text = ViewModel.Purchase.Notes ?? string.Empty;

            if (!ViewModel.Purchase.WeightEntryId.HasValue)
                BtnCreateWeightEntry.IsVisible = true;
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

    private void OnProductSelected(ProductoDto product)
    {
        ViewModel.SelectedProduct = product;
        LblProductName.Text = string.IsNullOrEmpty(product.Code)
            ? product.Nombre
            : $"{product.Code} - {product.Nombre}";
    }

    private async void BtnPickProduct_Clicked(object sender, EventArgs e)
    {
        await BtnPickProduct.ScaleTo(1.1, 100);
        await BtnPickProduct.ScaleTo(1.0, 100);

        ProductSelectView productSelectView = new ProductSelectView();
        productSelectView.OnProductSelected += OnProductSelected;
        await Shell.Current.Navigation.PushModalAsync(productSelectView);
    }

    private async void BtnCreateWeightEntry_Clicked(object sender, EventArgs e)
    {
        await BtnCreateWeightEntry.ScaleTo(1.1, 100);
        await BtnCreateWeightEntry.ScaleTo(1.0, 100);

        bool confirmed = await DisplayAlert("Confirmar", "¿Deseas crear una entrada de peso para este pedido?", "Sí", "No");
        if (!confirmed)
            return;

        WaitPopUp.Show("Creando entrada de peso, espere");
        try
        {
            WeightEntryDto created = await ViewModel.CreateWeightEntryAsync();

            BtnCreateWeightEntry.IsVisible = false;

            await DisplayAlert("Éxito", $"Entrada de peso creada (ID: {created.Id}). Aparecerá en pesos pendientes.", "OK");

            await Shell.Current.Navigation.PopAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "No se pudo crear la entrada de peso: " + ex.Message, "OK");
        }
        finally
        {
            WaitPopUp.Hide();
        }
    }

    private async void BtnSave_Clicked(object sender, EventArgs e)
    {
        if (ViewModel.SelectedProvider == null)
        {
            await DisplayAlert("Error", "Seleccione un proveedor.", "OK");
            return;
        }

        if (ViewModel.SelectedProduct == null)
        {
            await DisplayAlert("Error", "Seleccione un producto.", "OK");
            return;
        }

        if (!decimal.TryParse(EntryRequiredAmount.Text, out decimal amount) || amount <= 0)
        {
            await DisplayAlert("Error", "Ingrese una cantidad válida mayor a 0.", "OK");
            return;
        }

        ViewModel.Purchase.RequiredAmount = amount;
        ViewModel.Purchase.ExpectedArrival = DatePickerExpectedArrival.Date.ToUniversalTime();
        ViewModel.Purchase.Notes = string.IsNullOrWhiteSpace(EditorNotes.Text) ? null : EditorNotes.Text.Trim();

        WaitPopUp.Show("Guardando pedido, espere");
        try
        {
            await ViewModel.SaveAsync();

            await DisplayAlert("Éxito", ViewModel.IsEditing ? "Pedido actualizado." : "Pedido creado.", "OK");

            await Shell.Current.Navigation.PopAsync();
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
            IApiService apiService = MauiProgram.ServiceProvider.GetRequiredService<IApiService>();
            await apiService.DeleteAsync($"api/ProviderPurchase?id={ViewModel.Purchase.Id}");

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

    private async void BtnCancel_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.Navigation.PopAsync();
    }
}
