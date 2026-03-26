namespace Core.Application.DTOs
{
    /// <summary>
    /// Request DTO for validating partner credit availability
    /// </summary>
    public class CreditValidationRequest
    {
        public int PartnerId { get; set; }
        public double RequestedAmount { get; set; }
    }

    /// <summary>
    /// Response DTO containing credit validation results.
    /// Contains detailed credit information for administrative purposes.
    /// </summary>
    public class CreditValidationResponse
    {
        public bool IsValid { get; set; }
        public double AvailableCredit { get; set; }
        public double RequestedAmount { get; set; }
        public double PendingEntriesCost { get; set; }
        public double RemainingCredit { get; set; }
        public string? Message { get; set; }
    }
}
