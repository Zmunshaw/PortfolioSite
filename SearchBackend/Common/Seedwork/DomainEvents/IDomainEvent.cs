namespace SearchBackend.Common.Seedwork.DomainEvents;

public interface IDomainEvent
{
    DateTime OccurredOn { get; }
}
