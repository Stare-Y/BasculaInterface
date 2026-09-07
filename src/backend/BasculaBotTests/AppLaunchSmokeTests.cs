using FlaUI.Core.AutomationElements;

namespace BasculaBotTests
{
    /// <summary>
    /// Step 1 on the VM: prove FlaUI can launch the published MAUI app and read its visual tree.
    /// Every run writes <c>diagnostics.txt</c> (process state, console output, per-window control
    /// trees) + <c>window-*.png</c> to the artifacts folder - that's what we use to figure out
    /// startup problems and, once it's up, to pick AutomationIds.
    /// </summary>
    public class AppLaunchSmokeTests : IDisposable
    {
        private readonly AppDriver _app = AppDriver.Launch();

        [Fact]
        public void App_starts_and_FlaUI_can_read_its_window()
        {
            Window? window = _app.TryGetMainWindow();
            _app.Screenshot("desktop");
            string diag = _app.WriteDiagnostics();

            if (_app.HasExited)
                Assert.Fail($"BasculaInterface exited during startup (exit code {_app.ExitCode}). " +
                            $"Full console output + window state in {diag}.");

            Assert.True(window is not null,
                $"App is still running but produced no readable window within the timeout. See {diag}.");

            AutomationElement[] descendants = SafeDescendants(window!);
            Assert.True(descendants.Length >= 3,
                $"App window is up but has {descendants.Length} descendants - the MAUI tree isn't visible " +
                $"to UI Automation. See {diag} and window-*.png.");

            bool loginFound = descendants.Any(e =>
                string.Equals(Name(e), "Login", StringComparison.OrdinalIgnoreCase));
            Assert.True(loginFound,
                $"Window readable ({descendants.Length} elements) but no 'Login' button by name. See {diag}.");
        }

        private static AutomationElement[] SafeDescendants(Window w)
        {
            try { return w.FindAllDescendants(); }
            catch { return Array.Empty<AutomationElement>(); }
        }

        private static string Name(AutomationElement e)
        {
            try { return e.Name ?? string.Empty; }
            catch { return string.Empty; }
        }

        public void Dispose() => _app.Dispose();
    }
}
