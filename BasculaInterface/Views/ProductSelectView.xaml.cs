using BasculaInterface.ViewModels;
using BasculaInterface.Views.PopUps;
using CommunityToolkit.Maui.Views;
using Core.Application.DTOs;

namespace BasculaInterface.Views;

public partial class ProductSelectView : ContentPage
{
    private WaitPopUp? _popup;
    public Action<ProductoDto>? OnProductSelected;
    public ProductSelectView(ProductSelectorViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}

	public ProductSelectView() : this(MauiProgram.ServiceProvider.GetRequiredService<ProductSelectorViewModel>()) { }

    private void DisplayWaitPopUp(string message = "Cargando, espere")
    {
        _popup = new WaitPopUp(message);

        this.ShowPopup(_popup);
    }

    private async void SearchBar_SearchButtonPressed(object sender, EventArgs e)
    {
        if(BindingContext is ProductSelectorViewModel viewModel)
        {
            DisplayWaitPopUp("Buscando productos, espere");

            try
            {
                string searchTerm = SearchBar.Text?.Trim() ?? string.Empty;

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    await viewModel.SearchProducts(searchTerm);
                }
                else
                {
                    await DisplayAlert("Error", "El término de búsqueda no puede estar vacío.", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "No se pudieron buscar los productos: " + ex.Message, "OK");
            }
            finally
            {
                _popup?.Close();
            }
        }
    }

    private void ResultsCollectionView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if(ResultsCollectionView.SelectedItem is not null)
        {
            BtnConfirm.IsEnabled = true;
        }
        else
        {
            BtnConfirm.IsEnabled = false;
        }
    }

    private async void OnConfirmClicked(object sender, EventArgs e)
    {
        ProductoDto productoDto = (ProductoDto)ResultsCollectionView.SelectedItem;

        bool confirmed = await DisplayAlert("Confirmación", $"¿Deseas seleccionar {productoDto.Nombre}?", "No", "Si");
        if (!confirmed)
        {
            OnProductSelected?.Invoke(productoDto);
            await Shell.Current.Navigation.PopModalAsync();
        }
        else
        {
            ResultsCollectionView.SelectedItem = null;
            BtnConfirm.IsEnabled = false;
        }
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.Navigation.PopModalAsync();
    }
}