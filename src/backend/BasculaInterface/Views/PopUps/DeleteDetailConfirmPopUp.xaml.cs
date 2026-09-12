namespace BasculaInterface.Views.PopUps;

public partial class DeleteDetailConfirmPopUp : ContentView
{
    private TaskCompletionSource<(string Identifier, string Password)?> _tcs = null!;

    public DeleteDetailConfirmPopUp()
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
    // the resolved user's salted hash — this popup stays a plain input control (issue #134).
    // `title` lets this same control be reused for deleting a WeightEntry or a Pedido, not just
    // a WeightDetail (issue #133 / extend-delete-password-gate) — defaults to the original
    // wording so the existing detail-delete call site doesn't need to change.
    public Task<(string Identifier, string Password)?> ShowAsync(string detailDescription, string title = "Eliminar producto:")
    {
        _tcs = new TaskCompletionSource<(string Identifier, string Password)?>();

        TitleLabel.Text = title;
        DetailDescriptionLabel.Text = string.IsNullOrWhiteSpace(detailDescription) ? "Producto" : detailDescription;

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
