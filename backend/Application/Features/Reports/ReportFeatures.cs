using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Reports;

public record DashboardStatisticsDto(
    int TotalDocuments,
    int PendingApprovalDocuments,
    int ApprovedDocuments,
    int TotalIntakeRecords,
    int PendingIntakeRecords,
    int TotalDepartments,
    int TotalUsers,
    int TotalCategories);

public record GetDashboardStatisticsQuery : IRequest<Result<DashboardStatisticsDto>>;

public class GetDashboardStatisticsQueryHandler : IRequestHandler<GetDashboardStatisticsQuery, Result<DashboardStatisticsDto>>
{
    private readonly IApplicationDbContext _context;

    public GetDashboardStatisticsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<DashboardStatisticsDto>> Handle(GetDashboardStatisticsQuery request, CancellationToken cancellationToken)
    {
        var totalDocs = await _context.Documents.CountAsync(d => !d.IsDeleted, cancellationToken);
        var pendingDocs = await _context.Documents.CountAsync(d => !d.IsDeleted && d.Status == DocumentStatus.PendingApproval, cancellationToken);
        var approvedDocs = await _context.Documents.CountAsync(d => !d.IsDeleted && d.Status == DocumentStatus.Approved, cancellationToken);

        var totalIntakes = await _context.IntakeRecords.CountAsync(i => !i.IsDeleted, cancellationToken);
        var pendingIntakes = await _context.IntakeRecords.CountAsync(i => !i.IsDeleted && i.Status == IntakeStatus.Received, cancellationToken);

        var totalDepartments = await _context.Departments.CountAsync(d => !d.IsDeleted, cancellationToken);
        var totalUsers = await _context.Users.CountAsync(u => !u.IsDeleted, cancellationToken);
        var totalCategories = await _context.Categories.CountAsync(c => !c.IsDeleted, cancellationToken);

        var stats = new DashboardStatisticsDto(
            totalDocs,
            pendingDocs,
            approvedDocs,
            totalIntakes,
            pendingIntakes,
            totalDepartments,
            totalUsers,
            totalCategories);

        return Result<DashboardStatisticsDto>.Success(stats);
    }
}
public record DocumentSummaryReportDto(
    DateTime? FromDate,
    DateTime? ToDate,
    int TotalDocuments,
    int ApprovedDocuments,
    int PendingApprovalDocuments);

public record GetDocumentSummaryReportQuery(DateTime? FromDate, DateTime? ToDate)
    : IRequest<Result<DocumentSummaryReportDto>>;

public class GetDocumentSummaryReportQueryHandler
    : IRequestHandler<GetDocumentSummaryReportQuery, Result<DocumentSummaryReportDto>>
{
    private readonly IApplicationDbContext _context;

    public GetDocumentSummaryReportQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<DocumentSummaryReportDto>> Handle(
        GetDocumentSummaryReportQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.Documents.Where(d => !d.IsDeleted);

        if (request.FromDate.HasValue)
            query = query.Where(d => d.CreatedAt >= request.FromDate.Value);

        if (request.ToDate.HasValue)
            query = query.Where(d => d.CreatedAt <= request.ToDate.Value);

        var total = await query.CountAsync(cancellationToken);
        var approved = await query.CountAsync(d => d.Status == DocumentStatus.Approved, cancellationToken);
        var pending = await query.CountAsync(d => d.Status == DocumentStatus.PendingApproval, cancellationToken);

        return Result<DocumentSummaryReportDto>.Success(
            new DocumentSummaryReportDto(request.FromDate, request.ToDate, total, approved, pending));
    }
}

public record IntakeTimelineItemDto(
    DateTime Date,
    int Count);

public record IntakeTimelineReportDto(
    DateTime? FromDate,
    DateTime? ToDate,
    IReadOnlyList<IntakeTimelineItemDto> Items);

public record GetIntakeTimelineReportQuery(DateTime? FromDate, DateTime? ToDate)
    : IRequest<Result<IntakeTimelineReportDto>>;

public class GetIntakeTimelineReportQueryHandler
    : IRequestHandler<GetIntakeTimelineReportQuery, Result<IntakeTimelineReportDto>>
{
    private readonly IApplicationDbContext _context;

    public GetIntakeTimelineReportQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<IntakeTimelineReportDto>> Handle(
        GetIntakeTimelineReportQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.IntakeRecords.Where(i => !i.IsDeleted);

        if (request.FromDate.HasValue)
            query = query.Where(i => i.CreatedAt >= request.FromDate.Value);

        if (request.ToDate.HasValue)
            query = query.Where(i => i.CreatedAt <= request.ToDate.Value);

        var items = await query
            .GroupBy(i => i.CreatedAt.Date)
            .OrderBy(g => g.Key)
            .Select(g => new IntakeTimelineItemDto(g.Key, g.Count()))
            .ToListAsync(cancellationToken);

        return Result<IntakeTimelineReportDto>.Success(
            new IntakeTimelineReportDto(request.FromDate, request.ToDate, items));
    }
}