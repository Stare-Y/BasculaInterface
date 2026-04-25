using Microsoft.IdentityModel.Tokens;

namespace BasculaInterface.Views.PopUps;

public partial class PickQuantityPopUp : ContentView
{
    private TaskCompletionSource<Dictionary<double, int?>> _tcs = null!;

    private string _product = "Elije la cantidad para el producto";

    public string Product
    {
        get => _product;
        set
        {
            _product = value;
            ProductNameLabel.Text = _product;
        }
    }

    public PickQuantityPopUp()
    {
        InitializeComponent();
        ApplyProductName();
        SubscribeToKeyboardEvents();
    }

    public PickQuantityPopUp(string product)
    {
        InitializeComponent();

        Product = string.IsNullOrWhiteSpace(product)
            ? "Elije la cantidad para el producto"
            : product;

        ApplyProductName();
        SubscribeToKeyboardEvents();
    }

    private void SubscribeToKeyboardEvents()
    {
#if WINDOWS
        this.Loaded += (s, e) =>
        {
            var window = this.Window?.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
            if (window?.Content is Microsoft.UI.Xaml.UIElement content)
            {
                content.KeyDown += OnWindowKeyDown;
            }
        };

        this.Unloaded += (s, e) =>
        {
            var window = this.Window?.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
            if (window?.Content is Microsoft.UI.Xaml.UIElement content)
            {
                content.KeyDown -= OnWindowKeyDown;
            }
        };
#endif
    }

#if WINDOWS
    private void OnWindowKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if (!this.IsVisible)
            return;

        if (e.Key == Windows.System.VirtualKey.Escape)
        {
            OnPopupCancelClicked(btnCancel, EventArgs.Empty);
            e.Handled = true;
        }
    }
#endif

    private void ApplyProductName()
    {
        ProductNameLabel.Text = Product;
    }

    // ============================
    //      SHOW ASYNC (RETORNO)
    // ============================
    public Task<Dictionary<double, int?>> ShowAsync(string? message = null)
    {
        _tcs = new TaskCompletionSource<Dictionary<double, int?>>();

        if (!string.IsNullOrWhiteSpace(message))
            Product = message;

        this.IsVisible = true;

        QuantityEntry.Focus();

        return _tcs.Task;
    }

    // ============================
    //             HIDE
    // ============================
    private void CloseWithResult(double? quantity, int? costales)
    {
        this.IsVisible = false;
        QuantityEntry.Text = string.Empty;
        CostalesEntry.Text = string.Empty;
        _tcs?.TrySetResult(
            new Dictionary<double, int?>
            {
                { quantity ?? 0, costales }
            });
    }

    private async void OnPopupCancelClicked(object sender, EventArgs e)
    {
        await btnCancel.ScaleTo(1.1, 100);
        await btnCancel.ScaleTo(1.0, 100);

        CloseWithResult(null, null);
    }

    private async void OnPopupAcceptClicked(object sender, EventArgs e)
    {
        if(string.IsNullOrWhiteSpace(QuantityEntry.Text))
        {
            LblKilos.TextColor = Colors.Red;
            return;
        }
        await btnConfirm.ScaleTo(1.1, 100);
        await btnConfirm.ScaleTo(1.0, 100);

        int? costales = int.TryParse(CostalesEntry.Text, out int parsedCostales) ? parsedCostales : (int?)null;

        if (double.TryParse(QuantityEntry.Text, out double quantity))
            CloseWithResult(quantity, costales);
        else
            CloseWithResult(null, null);
    }

    // ============================
    //        VALIDACION
    // ============================
    private void QuantityEntry_TextChanged(object sender, TextChangedEventArgs e)
    {
        Entry entry = (Entry)sender;

        if (string.IsNullOrEmpty(entry.Text))
            return;

        if (!double.TryParse(entry.Text, out _))
            entry.Text = e.OldTextValue;
    }

    private void CostalesEntry_TextChanged(object sender, TextChangedEventArgs e)
    {
        Entry entry = (Entry)sender;

        if (string.IsNullOrEmpty(entry.Text))
            return;

        if (!double.TryParse(entry.Text, out _))
            entry.Text = e.OldTextValue;
    }

    private void QuantityEntry_Completed(object sender, EventArgs e)
    {
        // Move focus to the next entry
        CostalesEntry.Focus();
    }

    private void CostalesEntry_Completed(object sender, EventArgs e)
    {
        // Trigger confirm when Enter is pressed on the last entry
        OnPopupAcceptClicked(btnConfirm, EventArgs.Empty);
    }
}