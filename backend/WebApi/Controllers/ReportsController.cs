using Application.Features.Reports;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Manager")]
public class ReportsController : ControllerBase
{
    private readonly ISender _sender;

    public ReportsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetDashboardStatisticsQuery(), cancellationToken);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    [HttpGet("documents-summary")]
    public async Task<IActionResult> GetDocumentSummary(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new GetDocumentSummaryReportQuery(fromDate, toDate), cancellationToken);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    [HttpGet("intake-timeline")]
    public async Task<IActionResult> GetIntakeTimeline(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new GetIntakeTimelineReportQuery(fromDate, toDate), cancellationToken);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }
}