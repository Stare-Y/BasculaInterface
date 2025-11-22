using CommunityToolkit.Maui.Views;

namespace BasculaInterface.Views.PopUps;

public partial class WaitPopUp : Popup
{
	private string _message { get; set; }
	public string Message { 
		get => _message; 
		set{
			_message = value;
            LblMessage.Text = _message;
        } 
	}
	public WaitPopUp()
	{
		InitializeComponent();
	}
	public WaitPopUp(string message) : this()
	{
		LblMessage.Text = message;
    }
}