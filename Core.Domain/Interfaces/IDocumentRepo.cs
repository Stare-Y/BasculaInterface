namespace Core.Domain.Interfaces
{
    public interface IDocumentRepo
    {
        Task<double> GetClientDebt(int clientId, CancellationToken cancellationToken = default);
    }
}
