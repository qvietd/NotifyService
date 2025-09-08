using MediatR;
using NotifyService.Infrastructure.Repositories;

namespace NotifyService.Application.Features.Notify.Queries;

public record GetNotifyQuery(
    string userId,
    int page = 1,
    int pageSize = 10) : IRequest<GetNotifyResponse>;

public record GetNotifyResponse(List<NotificationDto> notifies);

public class GetNotifyQueryHandler : IRequestHandler<GetNotifyQuery, GetNotifyResponse>
{
    private readonly INotificationRepository _repository;

    public GetNotifyQueryHandler(INotificationRepository repository)
    {
        _repository = repository;
    }

    public async Task<GetNotifyResponse> Handle(GetNotifyQuery request, CancellationToken cancellationToken)
    {
        var notifies = await _repository.GetUserNotificationsAsync(request.userId, request.page, request.pageSize, cancellationToken);
        var result = notifies.Select(t => new NotificationDto()
        {
            Id = t.Id,
            Content = t.Content,
            CreatedAt = t.CreatedAt,
            IsRead = t.IsRead,
            Link = t.Link,
            RecipientId = t.UserId,
            SenderAvatar = t.SenderAvatar,
            SenderId = t.SenderId,
            Type = t.Type,
        }).ToList();

        return new GetNotifyResponse(result);
    }
}