using System.Diagnostics;
using System.Text;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Capturing;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
using FlaUI.UIA3;

namespace BasculaBotTests
{
    /// <summary>
    /// Launches the published BasculaInterface Windows app, captures its console output, and
    /// gives tests a handle on its window(s) plus a full diagnostic dump. One driver per test.
    ///
    /// The app path comes from <c>BASCULA_APP_EXE</c>, set by <c>scripts/vm/run-bot-suite.ps1</c>.
    /// </summary>
    public sealed class AppDriver : IDisposable
    {
        public Application Application { get; }
        public UIA3Automation Automation { get; }

        private readonly Process _process;
        private readonly StringBuilder _stdout = new();
        private readonly StringBuilder _stderr = new();

        private AppDriver(Process process, Application application, UIA3Automation automation)
        {
            _process = process;
            Application = application;
            Automation = automation;
        }

        public static AppDriver Launch()
        {
            string exe = Environment.GetEnvironmentVariable("BASCULA_APP_EXE")
                ?? throw new InvalidOperationException(
                    "BASCULA_APP_EXE is not set. Run these tests through scripts/vm/run-bot-suite.ps1.");
            if (!File.Exists(exe))
                throw new FileNotFoundException($"BASCULA_APP_EXE points at a file that doesn't exist: {exe}");

            var process = new Process
            {
                StartInfo = new ProcessStartInfo(exe)
                {
                    WorkingDirectory = Path.GetDirectoryName(exe),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                },
                EnableRaisingEvents = true,
            };

            process.Start();
            var driver = new AppDriver(process, Application.Attach(process.Id), new UIA3Automation());

            process.OutputDataReceived += (_, e) => { if (e.Data != null) lock (driver._stdout) driver._stdout.AppendLine(e.Data); };
            process.ErrorDataReceived += (_, e) => { if (e.Data != null) lock (driver._stderr) driver._stderr.AppendLine(e.Data); };
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            return driver;
        }

        public string ArtifactsDir =>
            Environment.GetEnvironmentVariable("BASCULA_BOT_ARTIFACTS") ?? Directory.GetCurrentDirectory();

        public string Stdout() { lock (_stdout) return _stdout.ToString(); }
        public string Stderr() { lock (_stderr) return _stderr.ToString(); }

        public bool HasExited { get { try { _process.Refresh(); return _process.HasExited; } catch { return true; } } }

        public int ExitCode { get { try { _process.Refresh(); return _process.HasExited ? _process.ExitCode : int.MinValue; } catch { return int.MinValue; } } }

        /// <summary>All top-level windows owned by the app's process, per the UIA desktop tree.</summary>
        public Window[] ProcessWindows()
        {
            try
            {
                return Automation.GetDesktop()
                    .FindAllChildren(cf => cf.ByControlType(ControlType.Window))
                    .Where(w => SafeInt(() => w.Properties.ProcessId.ValueOrDefault) == _process.Id)
                    .Select(w => w.AsWindow())
                    .ToArray();
            }
            catch { return Array.Empty<Window>(); }
        }

        /// <summary>
        /// The app's main window once it has real content. Returns null if the app exits or never
        /// produces a window with content — the caller writes diagnostics either way.
        /// </summary>
        public Window? TryGetMainWindow(TimeSpan? timeout = null)
        {
            DateTime deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(40));
            Window? lastSeen = null;

            while (DateTime.UtcNow < deadline)
            {
                if (HasExited)
                    return lastSeen;

                Window? main = null;
                try { main = Application.GetMainWindow(Automation, TimeSpan.FromSeconds(2)); } catch { }
                foreach (Window w in Prepend(main, ProcessWindows()))
                {
                    lastSeen = w;
                    if (SafeInt(() => w.FindAllDescendants().Length) >= 3)
                        return w;
                }

                Thread.Sleep(500);
            }

            return lastSeen;
        }

        private static IEnumerable<Window> Prepend(Window? first, Window[] rest)
        {
            if (first is not null) yield return first;
            foreach (Window w in rest) yield return w;
        }

        /// <summary>Human-readable dump of the process, its console output, every window it owns,
        /// and its control tree. Always safe to call.</summary>
        public string WriteDiagnostics(string fileName = "diagnostics.txt")
        {
            Directory.CreateDirectory(ArtifactsDir);
            string path = Path.Combine(ArtifactsDir, fileName);
            var sb = new StringBuilder();

            sb.AppendLine($"BasculaInterface bot diagnostics - {DateTime.Now:u}");
            sb.AppendLine($"exe: {Environment.GetEnvironmentVariable("BASCULA_APP_EXE")}");
            sb.AppendLine($"pid: {_process.Id}  HasExited: {HasExited}  ExitCode: {ExitCode}");
            sb.AppendLine();
            sb.AppendLine("--- stdout ---");
            sb.AppendLine(Trim(Stdout()));
            sb.AppendLine("--- stderr ---");
            sb.AppendLine(Trim(Stderr()));
            sb.AppendLine();

            Window[] windows = ProcessWindows();
            sb.AppendLine($"{windows.Length} top-level window(s) for this process");
            sb.AppendLine(new string('=', 70));

            int i = 0;
            foreach (Window w in windows)
            {
                sb.AppendLine($"--- window #{i} ---");
                sb.AppendLine($"name='{Safe(() => w.Name)}' class='{Safe(() => w.ClassName)}' " +
                              $"rect={Safe(() => w.BoundingRectangle.ToString())} offscreen={Safe(() => w.IsOffscreen.ToString())}");
                try
                {
                    AutomationElement[] d = w.FindAllDescendants();
                    sb.AppendLine($"{d.Length} descendants:");
                    foreach (AutomationElement e in d.Take(500))
                        sb.AppendLine($"  {Safe(() => e.ControlType.ToString()),-16} name='{Safe(() => e.Name)}' id='{Safe(() => e.AutomationId)}'");
                }
                catch (Exception ex) { sb.AppendLine($"  descendant walk failed: {ex.Message}"); }

                try { Capture.Element(w).ToFile(Path.Combine(ArtifactsDir, $"window-{i}.png")); } catch { }
                sb.AppendLine();
                i++;
            }

            File.WriteAllText(path, sb.ToString());
            return path;
        }

        public string Screenshot(string name)
        {
            Directory.CreateDirectory(ArtifactsDir);
            string path = Path.Combine(ArtifactsDir, $"{name}-{DateTime.Now:HHmmss}.png");
            try { Capture.Screen().ToFile(path); } catch { }
            return path;
        }

        private static string Trim(string s) => string.IsNullOrWhiteSpace(s) ? "(empty)" : s.Trim();
        private static string Safe(Func<string> get) { try { return get() ?? ""; } catch { return "?"; } }
        private static int SafeInt(Func<int> get) { try { return get(); } catch { return 0; } }

        /// <summary>Finds a descendant of <paramref name="window"/> by its <c>AutomationId</c>, or
        /// null if not (yet) present. Roleplays should prefer <see cref="WaitForElement"/> when the
        /// element may not exist the instant this is called (e.g. right after navigation).</summary>
        public static AutomationElement? FindByAutomationId(Window window, string id) =>
            window.FindFirstDescendant(cf => cf.ByAutomationId(id));

        /// <summary>Polls for a descendant of <paramref name="window"/> with the given
        /// <c>AutomationId</c> until it appears or <paramref name="timeout"/> elapses.</summary>
        public static AutomationElement? WaitForElement(Window window, string id, TimeSpan? timeout = null)
        {
            DateTime deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(10));
            while (DateTime.UtcNow < deadline)
            {
                AutomationElement? element = FindByAutomationId(window, id);
                if (element is not null)
                    return element;
                Thread.Sleep(200);
            }
            return FindByAutomationId(window, id);
        }

        /// <summary>Invokes/clicks an element the way a real user would, preferring the
        /// <c>Invoke</c> UIA pattern (works for Button/Border-with-tap-gesture alike) and falling
        /// back to a mouse click on its center point.</summary>
        public static void Click(AutomationElement element)
        {
            if (element.Patterns.Invoke.IsSupported)
            {
                element.Patterns.Invoke.Pattern.Invoke();
                return;
            }
            element.Click();
        }

        /// <summary>Clears any existing text and sets <paramref name="text"/> into an element
        /// (Entry/SearchBar) by focusing it first. Prefers setting the whole string in one atomic
        /// UIA Value-pattern call over simulating keystrokes: several bound Entries in this app
        /// validate/reformat on every TextChanged (e.g. WeightingScreen's manual-weight EntryLabel
        /// reverts the whole field on an invalid intermediate value), which character-by-character
        /// typing can trip over mid-string and corrupt.</summary>
        public static void TypeText(AutomationElement element, string text)
        {
            element.Focus();
            if (element.Patterns.Value.IsSupported)
            {
                element.Patterns.Value.Pattern.SetValue(text);
                return;
            }

            using (Keyboard.Pressing(VirtualKeyShort.CONTROL))
                Keyboard.Type(VirtualKeyShort.KEY_A);
            Keyboard.Type(VirtualKeyShort.DELETE);
            Keyboard.Type(text);
        }

        public void Dispose()
        {
            try { if (!HasExited) _process.Kill(entireProcessTree: true); } catch { }
            try { Application.Dispose(); } catch { }
            try { Automation.Dispose(); } catch { }
            try { _process.Dispose(); } catch { }
        }
    }
}
