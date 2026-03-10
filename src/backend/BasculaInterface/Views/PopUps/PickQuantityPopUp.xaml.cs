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
    }

    public PickQuantityPopUp(string product)
    {
        InitializeComponent();

        Product = string.IsNullOrWhiteSpace(product)
            ? "Elije la cantidad para el producto"
            : product;

        ApplyProductName();
    }

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
}