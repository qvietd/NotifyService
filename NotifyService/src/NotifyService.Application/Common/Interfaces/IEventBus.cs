namespace NotifyService.Application.Common.Interfaces;

public interface IEventBus
{
    Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : class;
}

public interface IDomainEventHandler<in TEvent>
{
    Task Handle(TEvent domainEvent, CancellationToken cancellationToken = default);
}