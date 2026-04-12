using System.Linq.Expressions;
using SearchBackend.Common.Seedwork.Aggregates;

namespace SearchBackend.Common.Seedwork.Interfaces;

public interface IRepository<T> where T : AggregateRoot<int>
{
    Task<T?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    Task AddAsync(T entity, CancellationToken ct = default);
    void Update(T entity);
    void Remove(T entity);
}
