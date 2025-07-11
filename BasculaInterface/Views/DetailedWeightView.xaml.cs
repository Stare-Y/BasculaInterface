using BasculaInterface.ViewModels;

namespace BasculaInterface.Views;

public partial class DetailedWeightView : ContentPage
{
	public DetailedWeightView(DetailedWeightViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}

	public DetailedWeightView() { }

    protected override void OnAppearing()
    {
        base.OnAppearing();
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
        await Shell.Current.Navigation.PopModalAsync();
    }

    private async void BtnNewEntry_Clicked(object sender, EventArgs e)
    {
        DetailedWeightViewModel viewModel = GetViewModel();

        if (viewModel.WeightEntry != null && viewModel.Partner != null)
        {
            WeightingScreen weightingScreen = new WeightingScreen(viewModel.WeightEntry, viewModel.Partner);

            await Shell.Current.Navigation.PushModalAsync(weightingScreen);
        }
    }

    private void BtnFinishWeight_Clicked(object sender, EventArgs e)
    {

    }
}