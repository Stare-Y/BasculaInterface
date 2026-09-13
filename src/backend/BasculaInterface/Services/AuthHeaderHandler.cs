using System.Net.Http.Headers;

namespace BasculaInterface.Services
{
    /// <summary>
    /// Attaches the current session's JWT to every outgoing API request (issue #134 / design.md
    /// Decision 7) — registered once on the app's <c>HttpClient</c> in <c>MauiProgram.cs</c>, so
    /// existing call sites through <c>IApiService</c> don't need to change individually.
    /// </summary>
    public class AuthHeaderHandler : DelegatingHandler
    {
        private readonly ISessionService _sessionService;
        private readonly InactivityWatcherService _inactivityWatcher;

        public AuthHeaderHandler(ISessionService sessionService, InactivityWatcherService inactivityWatcher)
        {
            _sessionService = sessionService;
            _inactivityWatcher = inactivityWatcher;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_sessionService.IsAuthenticated)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _sessionService.Token);
            }

            HttpResponseMessage response = await base.SendAsync(request, cancellationToken);

            // fix-session-inactivity-timeout design.md Decision 1: a successful API response counts
            // as activity too, not just direct user input.
            if (response.IsSuccessStatusCode)
            {
                _inactivityWatcher.RegisterActivity();
            }

            return response;
        }
    }
}
