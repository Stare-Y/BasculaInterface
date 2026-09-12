using System.Security.Claims;
using Core.Application.Services;

namespace BasculaTerminalApi.Service
{
    /// <summary>
    /// Lives here (not in the shared <c>Infrastructure</c> project) deliberately: it's the only
    /// consumer of <see cref="IHttpContextAccessor"/> in the codebase, and <c>Infrastructure</c> is
    /// referenced by both this API (an <c>Sdk.Web</c> project, which has the ASP.NET Core shared
    /// framework natively) and the MAUI client <c>BasculaInterface</c> (which must not). Putting
    /// this class in <c>Infrastructure</c> previously required a <c>FrameworkReference</c> to
    /// <c>Microsoft.AspNetCore.App</c> there, which leaked into the MAUI client and broke its
    /// Release/win-x64 ReadyToRun (crossgen) publish with a missing-method error on
    /// <c>ITlsHandshakeFeature.HostName</c> — a reference-assembly mismatch crossgen hit only
    /// because the MAUI app now transitively carried an ASP.NET Core framework reference it never
    /// needed.
    /// </summary>
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public int? UserId
        {
            get
            {
                string? sub = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
                return int.TryParse(sub, out int id) ? id : null;
            }
        }
    }
}
