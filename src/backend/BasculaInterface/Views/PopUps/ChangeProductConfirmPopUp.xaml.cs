namespace BasculaInterface.Views.PopUps;

public partial class ChangeProductConfirmPopUp : ContentView
{
    private TaskCompletionSource<string?> _tcs = null!;

    public ChangeProductConfirmPopUp()
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

    // Returns the plaintext password entered, or null if the user cancelled.
    // Hashing happens in the caller (ViewModel), keeping this popup a plain input control.
    public Task<string?> ShowAsync(string productName)
    {
        _tcs = new TaskCompletionSource<string?>();

        ProductNameLabel.Text = string.IsNullOrWhiteSpace(productName) ? "Producto" : productName;

        this.IsVisible = true;
        PasswordEntry.Focus();

        return _tcs.Task;
    }

    private void CloseWithResult(string? password)
    {
        this.IsVisible = false;
        PasswordEntry.Text = string.Empty;
        _tcs?.TrySetResult(password);
    }

    private async void OnPopupCancelClicked(object sender, EventArgs e)
    {
        await btnCancel.ScaleTo(1.1, 100);
        await btnCancel.ScaleTo(1.0, 100);

        CloseWithResult(null);
    }

    private async void OnPopupAcceptClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(PasswordEntry.Text))
            return;

        await btnConfirm.ScaleTo(1.1, 100);
        await btnConfirm.ScaleTo(1.0, 100);

        CloseWithResult(PasswordEntry.Text);
    }

    private void PasswordEntry_Completed(object sender, EventArgs e)
    {
        OnPopupAcceptClicked(btnConfirm, EventArgs.Empty);
    }
}
