using System.Globalization;

namespace BasculaInterface.Views.PopUps;

public partial class ChangeAmountConfirmPopUp : ContentView
{
    // Null result = cancelled. Otherwise (NewValue, Identifier, Password) — all required to
    // confirm; Identifier+Password are the self-authorize gate credential (issue #134).
    private TaskCompletionSource<(double NewValue, string Identifier, string Password)?> _tcs = null!;

    public ChangeAmountConfirmPopUp()
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

    /// <summary>
    /// Shows the popup. <paramref name="currentValue"/> is displayed for reference only.
    /// <paramref name="isGranel"/> switches the title/current-value labels between "peso" (kg)
    /// and "cantidad" (piece count) phrasing, mirroring RowActionMenuPopUp's same switch.
    /// Returns (NewValue, Identifier, Password) on confirm, or null if the operator cancelled.
    /// </summary>
    public Task<(double NewValue, string Identifier, string Password)?> ShowAsync(double currentValue, bool isGranel = true)
    {
        _tcs = new TaskCompletionSource<(double NewValue, string Identifier, string Password)?>();

        TitleLabel.Text = isGranel ? "Cambiar peso a:" : "Cambiar cantidad a:";
        CurrentValueLabel.Text = isGranel
            ? $"Actual: {currentValue:F2} kg"
            : $"Actual: {currentValue:F2}";
        NewValueEntry.Text = string.Empty;
        IdentifierEntry.Text = string.Empty;
        PasswordEntry.Text = string.Empty;

        this.IsVisible = true;
        NewValueEntry.Focus();

        return _tcs.Task;
    }

    private void CloseWithResult((double NewValue, string Identifier, string Password)? result)
    {
        this.IsVisible = false;
        NewValueEntry.Text = string.Empty;
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
        if (!double.TryParse(NewValueEntry.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out double newValue) || newValue <= 0)
        {
            await Application.Current!.Windows[0].Page!.DisplayAlert("Valor inválido", "Ingrese un valor numérico mayor que cero.", "OK");
            return;
        }

        if (string.IsNullOrEmpty(IdentifierEntry.Text) || string.IsNullOrEmpty(PasswordEntry.Text))
            return;

        await btnConfirm.ScaleTo(1.1, 100);
        await btnConfirm.ScaleTo(1.0, 100);

        CloseWithResult((newValue, IdentifierEntry.Text, PasswordEntry.Text));
    }

    private void PasswordEntry_Completed(object sender, EventArgs e)
    {
        OnPopupAcceptClicked(btnConfirm, EventArgs.Empty);
    }
}
