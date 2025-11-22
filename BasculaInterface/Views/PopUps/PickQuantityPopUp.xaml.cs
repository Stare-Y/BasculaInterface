using CommunityToolkit.Maui.Views;

namespace BasculaInterface.Views.PopUps;

public partial class PickQuantityPopUp : Popup
{
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
    protected override void OnParentSet()
    {
        base.OnParentSet();
        QuantityEntry.Focus();
    }

    private void OnPopupCancelClicked(object sender, EventArgs e)
    {
        this.Close(null);
    }

    private void OnPopupAcceptClicked(object sender, EventArgs e)
    {
        if(double.TryParse(QuantityEntry.Text, out double quantity))
        {
            this.Close(quantity);
        }
        else
        {
            this.Close(null);
        }
    }

    private void QuantityEntry_TextChanged(object sender, TextChangedEventArgs e)
    {
        var entry = (Entry)sender;

        if (string.IsNullOrEmpty(entry.Text))
            return;

        // Allow digits and ONE dot
        if (!double.TryParse(entry.Text, out _))
        {
            // revert to old text if invalid
            entry.Text = e.OldTextValue;
        }
    }
}