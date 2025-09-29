using Core.Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repos
{
    public class DocumentRepo : IDocumentRepo
    {
        private readonly ContpaqiSQLContext _dbContext;
        public DocumentRepo(ContpaqiSQLContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }
        public async Task<double> GetClientDebt(int clientId, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Documentos
                .AsNoTracking()
                .Where(d => d.CIDCLIENTEPROVEEDOR == clientId && d.CIDDOCUMENTODE == 4)
                .SumAsync(d => d.CPENDIENTE, cancellationToken);
        }
    }
}
