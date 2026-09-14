namespace BasculaInterface.Views.PopUps;

/// <summary>
/// Self-authorize gate credential prompt for the BypasTurn per-use override (role-driven-terminal-modes
/// design.md Decision 3) — same shape as ChangePartnerConfirmPopUp/ChangeProductConfirmPopUp, generic
/// since there's no per-action name to display here (unlike a partner or product name).
/// </summary>
public partial class AuthorizeTurnBypassPopUp : ContentView
{
    private TaskCompletionSource<(string Identifier, string Password)?> _tcs = null!;

    public AuthorizeTurnBypassPopUp()
    {
        InitializeComponent();
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

    // Returns the self-authorize gate credential (UserCode-or-Username + plaintext password), or
    // null if the user cancelled. The plaintext travels to the server, which verifies it against
    // the resolved user's salted hash via the same AuthorizeTurnBypass endpoint every other gated
    // action's credential flows through (POST /api/Weight/AuthorizeTurnBypass).
    public Task<(string Identifier, string Password)?> ShowAsync()
    {
        _tcs = new TaskCompletionSource<(string Identifier, string Password)?>();

        this.IsVisible = true;
        IdentifierEntry.Focus();

        return _tcs.Task;
    }

    private void CloseWithResult((string Identifier, string Password)? result)
    {
        this.IsVisible = false;
        IdentifierEntry.Text = string.Empty;
        PasswordEntry.Text = string.Empty;
        _tcs?.TrySetResult(result);
    }

    private async void OnPopupCancelClicked(object sender, EventArgs e)
    {
        await btnCancel.ScaleTo(1.1, 100);
        await btnCancel.ScaleTo(1.0, 100);

        CloseWithResult(null);
    }

    private async void OnPopupAcceptClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(IdentifierEntry.Text) || string.IsNullOrEmpty(PasswordEntry.Text))
            return;

        await btnConfirm.ScaleTo(1.1, 100);
        await btnConfirm.ScaleTo(1.0, 100);

        CloseWithResult((IdentifierEntry.Text, PasswordEntry.Text));
    }

    private void PasswordEntry_Completed(object sender, EventArgs e)
    {
        OnPopupAcceptClicked(btnConfirm, EventArgs.Empty);
    }
}
