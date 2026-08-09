namespace BasculaInterface.Views.PopUps;

public partial class RowActionMenuPopUp : ContentView
{
    private TaskCompletionSource<string?> _tcs = null!;

    public RowActionMenuPopUp()
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
            OnCancelClicked(btnCancel, EventArgs.Empty);
            e.Handled = true;
        }
    }
#endif

    // Returns the selected menu action's text ("Cambiar producto"/"Cambiar socio"), or null if
    // the operator cancelled. The caller (View code-behind) routes on the returned value the same
    // way it previously branched on DisplayActionSheet's result.
    public Task<string?> ShowAsync(string title)
    {
        _tcs = new TaskCompletionSource<string?>();

        TitleLabel.Text = string.IsNullOrWhiteSpace(title) ? "Opciones" : title;

        this.IsVisible = true;

        return _tcs.Task;
    }

    private void CloseWithResult(string? action)
    {
        this.IsVisible = false;
        _tcs?.TrySetResult(action);
    }

    private async void OnChangeProductClicked(object sender, EventArgs e)
    {
        await btnChangeProduct.ScaleTo(1.1, 100);
        await btnChangeProduct.ScaleTo(1.0, 100);

        CloseWithResult("Cambiar producto");
    }

    private async void OnChangePartnerClicked(object sender, EventArgs e)
    {
        await btnChangePartner.ScaleTo(1.1, 100);
        await btnChangePartner.ScaleTo(1.0, 100);

        CloseWithResult("Cambiar socio");
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await btnCancel.ScaleTo(1.1, 100);
        await btnCancel.ScaleTo(1.0, 100);

        CloseWithResult(null);
    }
}
