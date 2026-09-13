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
            _timer.Stop();
            _timer.Start();
        }

        public void Stop() => _timer.Stop();

        /// <summary>Call on any user input or successful API response to reset the countdown.</summary>
        public void RegisterActivity()
        {
            if (_timer.Interval > 0)
            {
                _timer.Stop();
                _timer.Start();
            }
        }

        public void Dispose() => _timer.Dispose();
    }
}
