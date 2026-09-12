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

        public AuthHeaderHandler(ISessionService sessionService)
        {
            _sessionService = sessionService;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_sessionService.IsAuthenticated)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _sessionService.Token);
            }

            return base.SendAsync(request, cancellationToken);
        }
    }
}
