namespace BasculaInterface.Services
{
    /// <summary>
    /// Client-side-only inactivity auto-logout (issue #134 / design.md Decision 7) — no server-side
    /// session/last-activity tracking. A single timer resets on every call to
    /// <see cref="RegisterActivity"/>; when it elapses uninterrupted, <see cref="OnTimeout"/> fires
    /// once. The caller (App-level code, wired at startup) is responsible for hooking
    /// <see cref="RegisterActivity"/> into global input (e.g. a tap/pointer gesture recognizer at
    /// the Shell root) and every successful API response, and for reacting to
    /// <see cref="OnTimeout"/> by logging out and navigating to the login page.
    /// </summary>
    public class InactivityWatcherService : IDisposable
    {
        private readonly System.Timers.Timer _timer;

        // Tracks whether Start(timeout) has ever been called with a real, per-user timeout.
        // RegisterActivity() must not arm the timer before that: System.Timers.Timer defaults
        // Interval to 100ms when never explicitly set, and 100 still satisfies "> 0" — so a naive
        // `_timer.Interval > 0` guard (what this used to check) doesn't actually protect against an
        // unconfigured timer. Without this flag, any pointer press on the login screen (the global
        // PointerPressedEvent hook in MauiProgram.cs calls RegisterActivity() on every press,
        // including clicking into the login fields or the "Ingresar" button itself) armed a
        // 100-millisecond countdown before the very first login ever completed, which then fired
        // OnTimeout mid-login — logging the just-established session straight back out. Only the
        // first login in a running app could ever race this way, since Start() permanently
        // overwrites _timer.Interval with a real value (minutes, not milliseconds) the first time
        // it succeeds.
        private bool _hasStarted = false;

        public event Action? OnTimeout;

        /// <summary>Whether the native window currently holds OS focus/activation — updated from
        /// <c>MauiProgram.cs</c>'s native window hook (Windows-only). Defaults to true so nothing
        /// relying on it misbehaves before that hook has fired at least once.</summary>
        public bool IsWindowActive { get; private set; } = true;

        /// <summary>Fires when the window regains activation, after having lost it. Lets a caller
        /// defer an action (e.g. the logout navigation) that has been observed to leave the app
        /// stuck when performed while unfocused, until focus actually returns.</summary>
        public event Action? OnWindowActivated;

        public void NotifyWindowActivationChanged(bool isActive)
        {
            bool wasActive = IsWindowActive;
            IsWindowActive = isActive;

            if (isActive && !wasActive)
            {
                OnWindowActivated?.Invoke();
            }
        }

        public InactivityWatcherService()
        {
            _timer = new System.Timers.Timer { AutoReset = false };
            _timer.Elapsed += (_, _) => OnTimeout?.Invoke();
        }

        /// <summary>Starts (or restarts) the watch with the given timeout. Passing a new timeout
        /// (e.g. read fresh from server config) takes effect on the next call.</summary>
        public void Start(TimeSpan timeout)
        {
            _timer.Interval = timeout.TotalMilliseconds;
            _hasStarted = true;
            _timer.Stop();
            _timer.Start();
        }

        public void Stop() => _timer.Stop();

        /// <summary>Call on any user input or successful API response to reset the countdown. A
        /// no-op before the first real <see cref="Start"/> call — see the <see cref="_hasStarted"/>
        /// comment for why that guard exists.</summary>
        public void RegisterActivity()
        {
            if (_hasStarted)
            {
                _timer.Stop();
                _timer.Start();
            }
        }

        public void Dispose() => _timer.Dispose();
    }
}
