using Core.Domain.Entities.ContpaqiSQL;
using Core.Domain.Interfaces;

namespace BasculaTerminalTest.TestDoubles
{
    /// <summary>
    /// The ContpaqiSQL side mirrors an external ERP on SQL Server, and
    /// <see cref="Infrastructure.Data.ContpaqiSQLContext"/> touches that database from its own
    /// constructor. Integration tests here never assert on ERP data, so the three repos backed by
    /// it are swapped for these empty fakes — which keeps <c>ContpaqiSQLContext</c> from ever
    /// being constructed. Add real seed data here if a future flow needs a product or partner.
    /// </summary>
    internal sealed class FakeProductRepo : IProductRepo
    {
        public Task<IEnumerable<Producto>> SearchByNameAsync(string name, int page, int sizePage)
            => Task.FromResult(Enumerable.Empty<Producto>());

        public Task<Producto> GetByIdAsync(int id)
            => throw new KeyNotFoundException($"FakeProductRepo has no product {id}.");

        public Task<IEnumerable<Producto>> GetByMultipleIdsAsync(int[] ids)
            => Task.FromResult(Enumerable.Empty<Producto>());
    }

    internal sealed class FakeClienteProveedorRepo : IClienteProveedorRepo
    {
        public Task<IEnumerable<ClienteProveedor>> SearchByName(string name, int page = 1, int sizePage = 50, CancellationToken cancellationToken = default)
            => Task.FromResult(Enumerable.Empty<ClienteProveedor>());

        public Task<ClienteProveedor> GetById(int id, CancellationToken cancellationToken = default)
            => throw new KeyNotFoundException($"FakeClienteProveedorRepo has no partner {id}.");

        public Task<IEnumerable<ClienteProveedor>> GetByMultipleIds(int[] ids, CancellationToken cancellationToken = default)
            => Task.FromResult(Enumerable.Empty<ClienteProveedor>());

        public Task<IEnumerable<ClienteProveedor>> SearchByCode(string code, int page = 1, int sizePage = 50, CancellationToken cancellationToken = default)
            => Task.FromResult(Enumerable.Empty<ClienteProveedor>());
    }

    internal sealed class FakeDocumentRepo : IDocumentRepo
    {
        public Task<double> GetClientDebt(int clientId, CancellationToken cancellationToken = default)
            => Task.FromResult(0.0);
    }
}
