using BasculaInterface.ViewModels;
using BasculaInterface.Views.PopUps;
using Core.Application.DTOs;
using Windows.Devices.I2c.Provider;

namespace BasculaInterface.Views;

public partial class PartnerSelectView : ContentPage
{
    public Action<ClienteProveedorDto>? OnPartnerSelected;
    private bool _providers = false;
    public PartnerSelectView(PartnerSelectorViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;

        if (string.IsNullOrWhiteSpace(LabelResultado.Text))
        {
            LabelResultado.Text = "Sin socio seleccionado";
        }
    }

    public PartnerSelectView(bool providers = false) : this(MauiProgram.ServiceProvider.GetRequiredService<PartnerSelectorViewModel>())
    {
        _providers = providers;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await Task.Delay(169);
        SearchBar.Focus();
    }

    private async void SearchBar_SearchButtonPressed(object sender, EventArgs e)
    {
        if(BindingContext is PartnerSelectorViewModel viewModel)
        {
            WaitPopUp.Show("Buscando socios, espere");
            try
            {
                string searchTerm = SearchBar.Text?.Trim() ?? string.Empty;
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    await viewModel.SearchPartners(searchTerm, _providers);
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
                WaitPopUp.Hide();
            }
        }
    }

    private async void OnConfirmClicked(object sender, EventArgs e)
    {
        await BtnConfirm.ScaleTo(1.1, 100);
        await BtnConfirm.ScaleTo(1.0, 100);

        try
        {
            if (ResultsCollectionView.SelectedItem is not ClienteProveedorDto partner)
            {
                await DisplayAlert("Error", "No has seleccionado ningún socio.", "OK");
                return;
            }

            if (!partner.OrderRequestAllowed && !partner.IsProvider)
            {
                await DisplayAlert("Error", "El socio seleccionado no puede solicitar pedidos, no tiene credito habilitado", "OK");
                return;
            }

            if (!partner.IgnoreCreditLimit && partner.CreditLimit != 0 && partner.AvailableCredit < 0 && !partner.IsProvider)
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
            ResultsCollectionView.SelectedItems?.Clear();
            ResultsCollectionView.SelectedItem = null;
            BtnConfirm.IsEnabled = false;
        }
    }

    private void ResultsCollectionView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is ClienteProveedorDto socio)
        {
            LabelResultado.Text = socio.RazonSocial;
            BtnConfirm.IsVisible = true;
        }
        else
        {
            LabelResultado.Text = "Ningún producto seleccionado";
            BtnConfirm.IsVisible = false;
        }
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await BtnBack.ScaleTo(1.1, 100);
        await BtnBack.ScaleTo(1.0, 100);

        await Shell.Current.Navigation.PopAsync();
    }
}