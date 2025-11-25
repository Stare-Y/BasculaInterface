
namespace BasculaInterface.Views.PopUps;

public partial class WaitPopUp : ContentView
{
    private string _message = "Espera...";

    public string Message
    {
        get => _message;
        set
        {
            _message = value;
            LblMessage.Text = _message;
        }
    }

    public WaitPopUp()
    {
        InitializeComponent();
    }

    public void Show(string? message = null)
    {
        if (!string.IsNullOrWhiteSpace(message))
            Message = message;

        this.IsVisible = true;
    }

    public void Hide()
    {
        this.IsVisible = false;
    }
}