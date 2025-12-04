namespace BasculaInterface.Views.PopUps;

public partial class PickQuantityPopUp : ContentView
{
    private TaskCompletionSource<double?> _tcs = null!;

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
    public Task<double?> ShowAsync(string? message = null)
    {
        _tcs = new TaskCompletionSource<double?>();

        if (!string.IsNullOrWhiteSpace(message))
            Product = message;

        this.IsVisible = true;

        QuantityEntry.Focus();

        return _tcs.Task;
    }

    // ============================
    //             HIDE
    // ============================
    private void CloseWithResult(double? quantity)
    {
        this.IsVisible = false;
        _tcs?.TrySetResult(quantity);
    }

    private void OnPopupCancelClicked(object sender, EventArgs e)
    {
        CloseWithResult(null);
    }

    private void OnPopupAcceptClicked(object sender, EventArgs e)
    {
        if (double.TryParse(QuantityEntry.Text, out double quantity))
            CloseWithResult(quantity);
        else
            CloseWithResult(null);
    }

    // ============================
    //        VALIDACIÓN
    // ============================
    private void QuantityEntry_TextChanged(object sender, TextChangedEventArgs e)
    {
        var entry = (Entry)sender;

        if (string.IsNullOrEmpty(entry.Text))
            return;

        if (!double.TryParse(entry.Text, out _))
            entry.Text = e.OldTextValue;
    }
}