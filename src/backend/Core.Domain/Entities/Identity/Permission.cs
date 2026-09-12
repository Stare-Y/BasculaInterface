namespace Core.Domain.Entities.Identity
{
    /// <summary>The two ABAC-overridable permission flags (design.md Decision 4).</summary>
    public enum Permission
    {
        CanSelfAuthorizeGate,
        CanCaptureWeightManually,
    }
}
