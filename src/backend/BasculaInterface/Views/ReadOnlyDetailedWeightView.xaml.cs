using BasculaInterface.ViewModels;
using Core.Application.DTOs;

namespace BasculaInterface.Views;

public partial class ReadOnlyDetailedWeightView : ContentPage
{
	public ReadOnlyDetailedWeightView(ReadOnlyDetailedViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
    }
	public ReadOnlyDetailedWeightView()
	{
		InitializeComponent();
    }
	
	protected override async void OnAppearing()
	{
		base.OnAppearing();
		if (BindingContext is not ReadOnlyDetailedViewModel viewModel)
			return;

		WaitPopUp.Show("Un momento porfavor...");

		try
		{
			await viewModel.FetchNewWeightDetails();

			if(viewModel.WeightEntry is not null && viewModel.WeightEntry.ConptaqiComercialFK.HasValue)
			{
				BtnContpaqId.Text = "Contpaqi: " + viewModel.WeightEntry.ConptaqiComercialFK.Value.ToString();
				BtnContpaqId.IsEnabled = false;
            }

            await viewModel.LoadExternalTargetBehaviors();

            if (viewModel.WeightEntry!.ExternalTargetBehaviorFK is not null || viewModel.WeightEntry!.ExternalTargetBehaviorFK > 0)
            {
                PickerTargetBehavior.SelectedItem = viewModel.ExternalTargetBehaviors.FirstOrDefault(behavior => behavior.Id == viewModel.WeightEntry!.ExternalTargetBehaviorFK);
            }

            PickerTargetBehavior.IsEnabled = viewModel.WeightEntry.ConptaqiComercialFK is null;
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

    private async void BtnContpaqId_Clicked(object sender, EventArgs e)
    {
        if (BindingContext is not ReadOnlyDetailedViewModel viewModel)
            return;

		WaitPopUp.Show("Enviando a Contpaqi Comercial, espere...");

		try
		{
            string result = await viewModel.SendToContpaqiComercial();

			if (viewModel.WeightEntry is not null && viewModel.WeightEntry.ConptaqiComercialFK.HasValue)
			{
				BtnContpaqId.Text = "Contpaqi: " + viewModel.WeightEntry.ConptaqiComercialFK.Value.ToString();
				BtnContpaqId.IsEnabled = false;
            }

            await DisplayAlert("Resultado:", result, "OK");
        }
		catch (Exception ex)
		{
			await DisplayAlert("Error", "No se pudo enviar a Contpaqi Comercial: " + ex.Message, "OK");
			return;
		}
		finally
		{
            if (viewModel.WeightEntry is not null && viewModel.WeightEntry.ConptaqiComercialFK.HasValue)
                BtnContpaqId.IsEnabled = false;

            WaitPopUp.Hide();
        }
    }

    private async void BtnPrintTicket_Clicked(object sender, EventArgs e)
    {

        await BtnPrintTicket.ScaleTo(1.1, 100);
        await BtnPrintTicket.ScaleTo(1.0, 100);

        if (BindingContext is ReadOnlyDetailedViewModel viewModel)
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

    private async void BtnVolver_Clicked(object sender, TappedEventArgs e)
    {
        await BtnVolver.ScaleTo(1.1, 100);
        await BtnVolver.ScaleTo(1.0, 100);

        await Shell.Current.Navigation.PopAsync();
    }

    private async  void BtnRefresh_Clicked(object sender, TappedEventArgs e)
    {
        await BtnRefresh.ScaleTo(1.1, 100);
        await BtnRefresh.ScaleTo(1.0, 100);

        if (BindingContext is ReadOnlyDetailedViewModel viewModel)
        {
            viewModel.IsRefreshing = true;
        }
    }

    private async void PickerTargetBehavior_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (BindingContext is not ReadOnlyDetailedViewModel viewModel)
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