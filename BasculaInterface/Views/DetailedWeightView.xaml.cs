using BasculaInterface.ViewModels;

namespace BasculaInterface.Views;

public partial class DetailedWeightView : ContentPage
{
    private bool _entriesChanged = false;
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
            try
            {
                await viewModel.ConcludeWeightProcess();

                await Shell.Current.Navigation.PopAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "No se pudo concluir el proceso de pesaje: " + ex.Message, "OK");
                return;
            }
        }
    }
}