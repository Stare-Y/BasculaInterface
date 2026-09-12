namespace Core.Application.Services
{
    /// <summary>Resolves the acting user's id from the current request's JWT claims, for services
    /// that need to attribute a mutating action (e.g. the audit log).</summary>
    public interface ICurrentUserService
    {
        /// <summary>The authenticated user's id, or null if there is no authenticated request in
        /// scope (e.g. a background/test context).</summary>
        int? UserId { get; }
    }
}
