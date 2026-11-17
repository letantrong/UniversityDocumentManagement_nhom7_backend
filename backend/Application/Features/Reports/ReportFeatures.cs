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
