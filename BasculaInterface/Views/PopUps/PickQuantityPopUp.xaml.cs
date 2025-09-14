using CommunityToolkit.Maui.Views;

namespace BasculaInterface.Views.PopUps;

public partial class PickQuantityPopUp : Popup
{
    public PickQuantityPopUp(string product = "Elije la cantidad para el producto")
	{
		InitializeComponent();

		ProductNameLabel.Text = product;
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