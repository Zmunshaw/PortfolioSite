using Mapster;
using MediatR;
using Portfolio.Application.DomainEvents;
using Portfolio.Application.Notifications;
using Portfolio.Common.Seedwork.DomainEvents;

namespace Portfolio.Infrastructure.DomainEvents;

public class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IMediator _mediator;

    public DomainEventDispatcher(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken ct = default)
    {
        foreach (var domainEvent in domainEvents)
        {
            var notificationType = DomainEventMappingConfig.GetNotificationType(domainEvent.GetType());
            if (notificationType is null)
                continue;

            var notification = domainEvent.Adapt(domainEvent.GetType(), notificationType);
            if (notification is INotification mediatRNotification)
                await _mediator.Publish(mediatRNotification, ct);
        }
    }
}
