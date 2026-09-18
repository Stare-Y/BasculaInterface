using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;

namespace BasculaBotTests
{
    /// <summary>Shared driving helpers reused by every role roleplay: login, row-selection
    /// confirmation, and simple visibility checks — kept here instead of on <see cref="AppDriver"/>
    /// since they're roleplay-specific (they know about MainPage's fields, PendingWeightsView's
    /// landing control, etc.), not generic FlaUI plumbing.</summary>
    public static class RoleplaySupport
    {
        public static readonly TimeSpan DefaultWait = TimeSpan.FromSeconds(15);

        public static Window GetMainWindow(AppDriver app)
        {
            Window? window = app.TryGetMainWindow();
            if (window is null)
                throw new InvalidOperationException(
                    $"App did not produce a readable window. Diagnostics: {app.WriteDiagnostics()}");
            return window;
        }

        /// <summary>Logs in via MainPage as the given BOT* user and waits for PendingWeightsView
        /// to appear. Every template in this batch resolves to Main/Secondary/PedidosOnly, never
        /// OnlyFinished (design.md's templates table has no overrides), so the landing screen is
        /// always PendingWeightsView.</summary>
        public static void LoginAs(AppDriver app, Window window, BotUserTemplate template, string password)
        {
            AutomationElement identifier = AppDriver.WaitForElement(window, "IdentifierEntry", DefaultWait)
                ?? throw new InvalidOperationException("Login screen's IdentifierEntry never appeared.");
            AutomationElement passwordEntry = AppDriver.WaitForElement(window, "PasswordEntry", DefaultWait)
                ?? throw new InvalidOperationException("Login screen's PasswordEntry never appeared.");
            AutomationElement loginButton = AppDriver.WaitForElement(window, "BtnLogIn", DefaultWait)
                ?? throw new InvalidOperationException("Login screen's BtnLogIn never appeared.");

            AppDriver.TypeText(identifier, template.UserCode);
            AppDriver.TypeText(passwordEntry, password);
            AppDriver.Click(loginButton);

            AutomationElement? landing = AppDriver.WaitForElement(window, "PendingWeightsCollectionView", DefaultWait);
            if (landing is null)
            {
                AutomationElement? loginError = AppDriver.FindByAutomationId(window, "LoginErrorLabel");
                string errorText = loginError is not null ? SafeName(loginError) : "(none)";
                string diag = app.WriteDiagnostics($"login-failed-{template.UserCode}.txt");
                throw new InvalidOperationException(
                    $"Login as {template.UserCode} did not land on PendingWeightsView. LoginError: {errorText}. Diagnostics: {diag}");
            }
        }

        private static string SafeName(AutomationElement e) { try { return e.Name ?? string.Empty; } catch { return "?"; } }
        private static bool SafeBool(Func<bool> get) { try { return get(); } catch { return false; } }

        public static bool IsVisible(AutomationElement? element) =>
            element is not null && SafeBool(() => !element.IsOffscreen);

        public static bool IsHiddenOrAbsent(AutomationElement? element) =>
            element is null || SafeBool(() => element.IsOffscreen);

        /// <summary>Types into a SearchBar and submits it (Enter), mirroring how a user searches
        /// on PartnerSelectView/ProductSelectView.</summary>
        public static void Search(AutomationElement searchBar, string term)
        {
            AppDriver.TypeText(searchBar, term);
            Keyboard.Type(VirtualKeyShort.ENTER);
        }

        /// <summary>Finds a native MAUI DisplayAlert button by its visible text (case-insensitive —
        /// the app's own DisplayAlert calls aren't consistent about it, e.g. "OK" vs "Ok") and
        /// clicks it. DisplayAlert renders as a platform dialog with no AutomationId hook
        /// available from XAML, so this is the one place these roleplays locate a control by
        /// name/text instead of AutomationId.</summary>
        public static bool TryClickNativeDialogButton(Window window, string buttonName)
        {
            AutomationElement? button = window
                .FindAllDescendants(cf => cf.ByControlType(ControlType.Button))
                .FirstOrDefault(b => string.Equals(SafeName(b), buttonName, StringComparison.OrdinalIgnoreCase));
            if (button is null)
                return false;
            AppDriver.Click(button);
            return true;
        }

        public static void ClickNativeDialogButton(Window window, string buttonName, TimeSpan? timeout = null)
        {
            DateTime deadline = DateTime.UtcNow + (timeout ?? DefaultWait);
            while (DateTime.UtcNow < deadline)
            {
                if (TryClickNativeDialogButton(window, buttonName))
                    return;
                Thread.Sleep(200);
            }
            throw new InvalidOperationException($"Native dialog button '{buttonName}' never appeared.");
        }

        /// <summary>
        /// Confirms the native MAUI <c>DisplayAlert("Confirmación", ..., accept, cancel)</c> shown
        /// by PartnerSelectView/ProductSelectView right after a row is selected. Both views wire
        /// their confirmation so that clicking the button labelled "Si" always proceeds with the
        /// selection (see the "OnConfirmClicked" accept/cancel argument order in
        /// ProductSelectView.xaml.cs, which is reversed from
        /// ResultsCollectionView_SelectionChanged's but yields the same "Si" = proceed result).
        /// </summary>
        public static void ConfirmSelectionDialog(Window window, TimeSpan? timeout = null) =>
            ClickNativeDialogButton(window, "Si", timeout);

        /// <summary>Finds the first descendant with <paramref name="automationId"/> (there may be
        /// several — e.g. one "BtnSeleccionar"/"selectedResult" per row) whose own subtree
        /// contains a descendant named with <paramref name="text"/>. Used to pick a specific
        /// PendingWeightsView row by vehicle plate when more than one row is present.</summary>
        public static AutomationElement? FindRowContaining(Window window, string automationId, string text)
        {
            AutomationElement[] candidates = window.FindAllDescendants(cf => cf.ByAutomationId(automationId));
            foreach (AutomationElement candidate in candidates)
            {
                try
                {
                    if (candidate.FindAllDescendants().Any(d => SafeName(d).Contains(text, StringComparison.OrdinalIgnoreCase)))
                        return candidate;
                }
                catch { }
            }
            return null;
        }

        /// <summary>
        /// Clicks <paramref name="trigger"/> and waits to see which of two outcomes happens first:
        /// a control identified by <paramref name="successAutomationId"/> appearing (the gate
        /// passed), or a native DisplayAlert button named <paramref name="blockedDialogButton"/>
        /// appearing (the gate rejected the click) — used for BtnNuevoProducto on DetailedWeightView,
        /// which isn't disabled when blocked; it shows an error DisplayAlert on click instead
        /// (confirmed with the project owner — see tasks.md 2.5's amendment).
        /// </summary>
        public static bool ClickAndRaceOutcome(
            Window window, AutomationElement trigger, string successAutomationId, string blockedDialogButton, TimeSpan? timeout = null)
        {
            AppDriver.Click(trigger);
            return RaceOutcome(window, successAutomationId, blockedDialogButton, timeout);
        }

        /// <summary>Waits to see which of two outcomes happens first: a control identified by
        /// <paramref name="successAutomationId"/> appearing, or a native DisplayAlert button
        /// named <paramref name="blockedDialogButton"/> appearing.</summary>
        public static bool RaceOutcome(
            Window window, string successAutomationId, string blockedDialogButton, TimeSpan? timeout = null)
        {
            DateTime deadline = DateTime.UtcNow + (timeout ?? DefaultWait);
            while (DateTime.UtcNow < deadline)
            {
                if (AppDriver.FindByAutomationId(window, successAutomationId) is not null)
                    return true;

                if (TryClickNativeDialogButton(window, blockedDialogButton))
                    return false;

                Thread.Sleep(200);
            }
            throw new InvalidOperationException(
                $"Neither '{successAutomationId}' nor the blocked dialog ('{blockedDialogButton}') appeared.");
        }
    }
}
