namespace Portfolio.Common.Seedwork.Interfaces;

public interface IUnitOfWork : IDisposable, IAsyncDisposable
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
