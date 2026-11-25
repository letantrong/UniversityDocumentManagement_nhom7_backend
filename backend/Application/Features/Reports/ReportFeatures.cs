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

public record DocumentSummaryReportDto(
    int TotalDocuments,
    int PendingApprovalDocuments,
    int ApprovedDocuments,
    int RejectedDocuments);

public record IntakeTimelineItemDto(
    DateTime Date,
    int Count);

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

        var totalDocuments = await query.CountAsync(cancellationToken);
        var pendingApprovalDocuments = await query.CountAsync(
            d => d.Status == DocumentStatus.PendingApproval, cancellationToken);
        var approvedDocuments = await query.CountAsync(
            d => d.Status == DocumentStatus.Approved, cancellationToken);
        var rejectedDocuments = await query.CountAsync(
            d => d.Status == DocumentStatus.Rejected, cancellationToken);

        var summary = new DocumentSummaryReportDto(
            totalDocuments,
            pendingApprovalDocuments,
            approvedDocuments,
            rejectedDocuments);

        return Result<DocumentSummaryReportDto>.Success(summary);
    }
}

public record GetIntakeTimelineReportQuery(DateTime? FromDate, DateTime? ToDate)
    : IRequest<Result<List<IntakeTimelineItemDto>>>;

public class GetIntakeTimelineReportQueryHandler
    : IRequestHandler<GetIntakeTimelineReportQuery, Result<List<IntakeTimelineItemDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetIntakeTimelineReportQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<IntakeTimelineItemDto>>> Handle(
        GetIntakeTimelineReportQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.IntakeRecords.Where(i => !i.IsDeleted);

        if (request.FromDate.HasValue)
            query = query.Where(i => i.CreatedAt >= request.FromDate.Value);

        if (request.ToDate.HasValue)
            query = query.Where(i => i.CreatedAt <= request.ToDate.Value);

        var timeline = await query
            .GroupBy(i => i.CreatedAt.Date)
            .Select(g => new IntakeTimelineItemDto(g.Key, g.Count()))
            .OrderBy(x => x.Date)
            .ToListAsync(cancellationToken);

        return Result<List<IntakeTimelineItemDto>>.Success(timeline);
    }
}