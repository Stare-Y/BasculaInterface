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

    protected override void OnAppearing()
    {
        base.OnAppearing();
        SearchBar.Focus();
    }

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
        try
        {
            if (ResultsCollectionView.SelectedItem is not ClienteProveedorDto partner)
            {
                await DisplayAlert("Error", "No has seleccionado ningún socio.", "OK");
                return;
            }

            if (!partner.OrderRequestAllowed)
            {
                await DisplayAlert("Error", "El socio seleccionado no puede solicitar pedidos, no tiene credito habilitado", "OK");
                return;
            }

            if (!partner.IgnoreCreditLimit && partner.CreditLimit != 0 && partner.AvailableCredit < 0)
            {
                await DisplayAlert("Error", "El socio seleccionado no puede solicitar pedidos, excede su limite de credito", "OK");
                return;
            }

            bool confirmed = await DisplayAlert("Confirmación", $"¿Deseas seleccionar a {partner.RazonSocial}?", "No", "Si");
            if (!confirmed)
            {
                OnPartnerSelected?.Invoke(partner);
                await Shell.Current.Navigation.PopAsync();
            }
        }
        finally
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