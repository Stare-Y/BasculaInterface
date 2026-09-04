using BasculaInterface.Models;
using BasculaInterface.ViewModels;
using Core.Application.Services;

namespace BasculaInterface.Views;

public partial class PedidoListView : ContentPage
{
    private bool _isFirstLoad = true;

    public PedidoListView(PedidoListViewModel viewModel)
    {
        BindingContext = viewModel
            ?? throw new ArgumentNullException(nameof(viewModel));

        InitializeComponent();

        this.Loaded += OnPageLoaded;
    }

    public PedidoListView()
        : this(MauiProgram.ServiceProvider.GetRequiredService<PedidoListViewModel>()) { }

    private async void OnPageLoaded(object? sender, EventArgs e)
    {
        if (!_isFirstLoad)
            return;

        _isFirstLoad = false;
        await LoadDataAsync();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_isFirstLoad)
            return;

        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        if (BindingContext is not PedidoListViewModel viewModel)
            return;

        WaitPopUp.Show("Cargando pedidos, espere");
        try
        {
            await viewModel.LoadPedidosAsync();
            PedidosCollectionView.ItemsSource = viewModel.Pedidos;
            UpdatePaginationControls(viewModel);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "No se pudieron cargar los pedidos: " + ex.Message, "OK");
        }
        finally
        {
            WaitPopUp.Hide();
        }
    }

    private void UpdatePaginationControls(PedidoListViewModel viewModel)
    {
        BtnPrevPage.IsEnabled = viewModel.CanGoBack;
        BtnNextPage.IsEnabled = viewModel.CanGoForward;
        LblPage.Text = viewModel.PageText;
    }

    private async void PedidosCollectionView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PedidosCollectionView.SelectedItem is not PedidoViewRow row)
            return;

        WaitPopUp.Show("Cargando pedido, espere");
        try
        {
            PedidoFormViewModel formViewModel = new(
                MauiProgram.ServiceProvider.GetRequiredService<IApiService>());

            formViewModel.LoadExisting(row.Pedido);

            PedidoFormView formView = new(formViewModel);
            await Shell.Current.Navigation.PushAsync(formView);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "No se pudo abrir el pedido: " + ex.Message, "OK");
        }
        finally
        {
            PedidosCollectionView.SelectedItem = null;
            WaitPopUp.Hide();
        }
    }

    private async void BtnCreate_Clicked(object sender, EventArgs e)
    {
        try
        {
            PedidoFormViewModel formViewModel = new(
                MauiProgram.ServiceProvider.GetRequiredService<IApiService>());

            PedidoFormView formView = new(formViewModel);
            await Shell.Current.Navigation.PushAsync(formView);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "No se pudo abrir el formulario: " + ex.Message, "OK");
        }
    }

    private async void BtnRefresh_Clicked(object sender, EventArgs e)
    {
        await LoadDataAsync();
    }

    private async void BtnPrevPage_Clicked(object sender, EventArgs e)
    {
        if (BindingContext is not PedidoListViewModel viewModel)
            return;

        WaitPopUp.Show("Cargando pedidos, espere");
        try
        {
            await viewModel.GoToPreviousPageAsync();
            PedidosCollectionView.ItemsSource = viewModel.Pedidos;
            UpdatePaginationControls(viewModel);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "No se pudieron cargar los pedidos: " + ex.Message, "OK");
        }
        finally
        {
            WaitPopUp.Hide();
        }
    }

    private async void BtnNextPage_Clicked(object sender, EventArgs e)
    {
        if (BindingContext is not PedidoListViewModel viewModel)
            return;

        WaitPopUp.Show("Cargando pedidos, espere");
        try
        {
            await viewModel.GoToNextPageAsync();
            PedidosCollectionView.ItemsSource = viewModel.Pedidos;
            UpdatePaginationControls(viewModel);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "No se pudieron cargar los pedidos: " + ex.Message, "OK");
        }
        finally
        {
            WaitPopUp.Hide();
        }
    }

    private async void BtnBack_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.Navigation.PopAsync();
    }
}
