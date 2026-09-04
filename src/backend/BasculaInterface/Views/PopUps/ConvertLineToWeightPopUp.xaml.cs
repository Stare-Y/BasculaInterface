using Core.Application.DTOs;
using System.Globalization;

namespace BasculaInterface.Views.PopUps;

public partial class ConvertLineToWeightPopUp : ContentView
{
    // Null result = cancelled. An almacén-target selection is mandatory, so AlmacenTargetId is never 0 on a confirmed result.
    private TaskCompletionSource<(decimal TargetAmount, int AlmacenTargetId)?> _tcs = null!;

    public ConvertLineToWeightPopUp()
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
    /// Shows the popup. <paramref name="pendingAmount"/> is displayed for reference and
    /// used to pre-fill the target-amount entry (defaults to weighing the full pending
    /// amount, per design.md Decision 3). <paramref name="almacenTargets"/> are the hidden
    /// <c>ExternalTargetBehavior</c> rows — the operator MUST pick one (design.md
    /// Decision 5); the row whose <c>TargetAlmacen</c> matches
    /// <paramref name="defaultAlmacenCode"/> is pre-selected. Returns null if the
    /// list is empty (caller should have shown an error already) or the operator cancels.
    /// </summary>
    public Task<(decimal TargetAmount, int AlmacenTargetId)?> ShowAsync(
        decimal pendingAmount,
        IEnumerable<ExternalTargetBehaviorDto> almacenTargets,
        string? defaultAlmacenCode)
    {
        _tcs = new TaskCompletionSource<(decimal TargetAmount, int AlmacenTargetId)?>();

        List<ExternalTargetBehaviorDto> targetList = almacenTargets.ToList();
        if (targetList.Count == 0)
        {
            _tcs.TrySetResult(null);
            return _tcs.Task;
        }

        PendingLabel.Text = $"Pendiente: {pendingAmount:F2} kg";
        TargetAmountEntry.Text = pendingAmount.ToString("F2", CultureInfo.InvariantCulture);

        AlmacenPicker.ItemsSource = targetList;
        int defaultIndex = string.IsNullOrWhiteSpace(defaultAlmacenCode)
            ? -1
            : targetList.FindIndex(t => t.TargetAlmacen == defaultAlmacenCode);
        AlmacenPicker.SelectedIndex = defaultIndex;

        this.IsVisible = true;
        TargetAmountEntry.Focus();

        return _tcs.Task;
    }

    private void CloseWithResult((decimal TargetAmount, int AlmacenTargetId)? result)
    {
        this.IsVisible = false;
        TargetAmountEntry.Text = string.Empty;
        AlmacenPicker.SelectedIndex = -1;
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
        if (!decimal.TryParse(TargetAmountEntry.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal targetAmount) || targetAmount <= 0)
        {
            await Application.Current!.Windows[0].Page!.DisplayAlert("Valor inválido", "Ingrese una cantidad numérica mayor que cero.", "OK");
            return;
        }

        if (AlmacenPicker.SelectedItem is not ExternalTargetBehaviorDto selected)
        {
            await Application.Current!.Windows[0].Page!.DisplayAlert("Almacén requerido", "Seleccione el almacén destino antes de continuar.", "OK");
            return;
        }

        await btnConfirm.ScaleTo(1.1, 100);
        await btnConfirm.ScaleTo(1.0, 100);

        CloseWithResult((targetAmount, selected.Id));
    }
}
