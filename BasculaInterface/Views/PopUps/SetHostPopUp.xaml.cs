using CommunityToolkit.Maui.Views;

namespace BasculaInterface.Views.PopUps;

public partial class SetHostPopUp : Popup
{
	public string Host { get; set; }

    // the event
    public event Action<string>? OnHostSet;

    public SetHostPopUp(string host)
    {
        InitializeComponent();
        Host = host;
        BindingContext = this;
    }

    // the method to raise the event
    protected virtual void RaiseOnHostSet()
    {
        OnHostSet?.Invoke(Host);
    }

    private void OnPopupAcceptClicked(object sender, EventArgs e)
    {
        PopUpFrame.BackgroundColor = Colors.DarkGray;
        RaiseOnHostSet();
    }

    private void OnPopupCancelClicked(object sender, EventArgs e)
    {
        this.Close();
    }
}