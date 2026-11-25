using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Notifications;

public record NotificationDto(
    Guid Id,
    string Title,
    string Message,
    NotificationType Type,
    string TypeName,
    string? ReferenceId,
    bool IsRead,
    DateTime? ReadAt,
    DateTime CreatedAt);

public record GetMyNotificationsQuery(bool? OnlyUnread = null) : IRequest<Result<List<NotificationDto>>>;

public class GetMyNotificationsQueryHandler : IRequestHandler<GetMyNotificationsQuery, Result<List<NotificationDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetMyNotificationsQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<List<NotificationDto>>> Handle(GetMyNotificationsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
            return Result<List<NotificationDto>>.Success(new List<NotificationDto>());

        var query = _context.Notifications
            .Where(n => n.UserId == userId.Value && !n.IsDeleted);

        if (request.OnlyUnread == true)
            query = query.Where(n => !n.IsRead);

        var notifications = await query
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .Select(n => new NotificationDto(
                n.Id,
                n.Title,
                n.Message,
                n.Type,
                n.Type.ToString(),
                n.ReferenceId,
                n.IsRead,
                n.ReadAt,
                n.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result<List<NotificationDto>>.Success(notifications);
    }
}

public record MarkNotificationAsReadCommand(Guid NotificationId) : IRequest<Result>;

public class MarkNotificationAsReadCommandHandler : IRequestHandler<MarkNotificationAsReadCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeService _dateTimeService;

    public MarkNotificationAsReadCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeService dateTimeService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeService = dateTimeService;
    }

    public async Task<Result> Handle(MarkNotificationAsReadCommand request, CancellationToken cancellationToken)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == request.NotificationId && n.UserId == _currentUserService.UserId, cancellationToken);

        if (notification == null)
            return Result.Failure("Notification not found.");

        notification.IsRead = true;
        notification.ReadAt = _dateTimeService.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success("Notification marked as read.");
    }
}
public record CreateNotificationCommand(
    Guid UserId,
    string Title,
    string Message,
    NotificationType Type = NotificationType.System,
    string? ReferenceId = null) : IRequest<Result<Guid>>;

public class CreateNotificationCommandHandler : IRequestHandler<CreateNotificationCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeService _dateTimeService;
    private readonly INotificationService _notificationService;

    public CreateNotificationCommandHandler(
        IApplicationDbContext context,
        IDateTimeService dateTimeService,
        INotificationService notificationService)
    {
        _context = context;
        _dateTimeService = dateTimeService;
        _notificationService = notificationService;
    }

    public async Task<Result<Guid>> Handle(CreateNotificationCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Message))
            return Result<Guid>.Failure("Title and message are required.");

        var notification = new Domain.Entities.Notification
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            Title = request.Title.Trim(),
            Message = request.Message.Trim(),
            Type = request.Type,
            ReferenceId = request.ReferenceId,
            IsRead = false,
            CreatedAt = _dateTimeService.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync(cancellationToken);

        await _notificationService.SendToUserAsync(
            notification.UserId,
            notification.Title,
            notification.Message,
            notification.ReferenceId,
            cancellationToken);

        return Result<Guid>.Success(notification.Id);
    }
}