using Portfolio.Common.Seedwork.DomainEvents;

namespace Portfolio.Application.DomainEvents;

public interface IDomainEventDispatcher
{
    Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken ct = default);
}
