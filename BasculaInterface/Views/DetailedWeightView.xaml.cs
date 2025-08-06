using BasculaInterface.ViewModels;
using BasculaInterface.Views.PopUps;
using CommunityToolkit.Maui.Views;

namespace BasculaInterface.Views;

public partial class DetailedWeightView : ContentPage 
{
    private bool _entriesChanged = false;
    private WaitPopUp? _popup;
    public DetailedWeightView(DetailedWeightViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
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
        if(BindingContext is DetailedWeightViewModel viewModel)
        {
            DisplayWaitPopUp("Concluyendo proceso de pesaje, espere...");
            try
            {
                await viewModel.ConcludeWeightProcess();

                await viewModel.PrintTicketAsync(BuildTicket());

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
            }
            catch(Exception ex)
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
        if(BindingContext is DetailedWeightViewModel viewModel)
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
}