using BasculaInterface.ViewModels;
using BasculaInterface.Views.PopUps;
using CommunityToolkit.Maui.Views;
using Core.Application.DTOs;

namespace BasculaInterface.Views;

public partial class PartnerSelectView : ContentPage
{
    private WaitPopUp? _popup;
    public Action<ClienteProveedorDto>? OnPartnerSelected;
    // Constructor that accepts a ViewModel
    public PartnerSelectView(PartnerSelectorViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
    }

    public PartnerSelectView() : this(MauiProgram.ServiceProvider.GetRequiredService<PartnerSelectorViewModel>()) { }

    private void DisplayWaitPopUp(string message = "Cargando, espere")
    {
        _popup = new WaitPopUp(message);

        this.ShowPopup(_popup);
    }

    private async void SearchBar_SearchButtonPressed(object sender, EventArgs e)
    {
        if(BindingContext is PartnerSelectorViewModel viewModel)
        {
            DisplayWaitPopUp("Buscando socios, espere");
            try
            {
                string searchTerm = SearchBar.Text?.Trim() ?? string.Empty;
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    await viewModel.SearchPartners(searchTerm);
                }
                else
                {
                    await DisplayAlert("Error", "El término de búsqueda no puede estar vacío.", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "No se pudieron buscar los socios: " + ex.Message, "OK");
            }
            finally
            {
                _popup?.Close();
            }
        }
    }

    private async void OnConfirmClicked(object sender, EventArgs e)
    {
        ClienteProveedorDto partner = (ClienteProveedorDto)ResultsCollectionView.SelectedItem;

        bool confirmed = await DisplayAlert("Confirmación", $"¿Deseas seleccionar {partner.RazonSocial}?", "No", "Si");
        if (!confirmed)
        {
            OnPartnerSelected?.Invoke(partner);
        }
        else
        {
            ResultsCollectionView.SelectedItem = null;
            BtnConfirm.IsEnabled = false;
        }
    }

    private void ResultsCollectionView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ResultsCollectionView.SelectedItem is not null)
        {
            BtnConfirm.IsEnabled = true;
        }
        else
        {
            BtnConfirm.IsEnabled = false;
        }
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.Navigation.PopAsync();
    }
}