using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SC.Application.MediatR.Reports.Manager.GetOrderIssuesReport;
using SC.Application.MediatR.Reports.Manager.GetRefundPoliciesReport;
using SC.Application.MediatR.Reports.Manager.GetReportSummary;
using SC.Application.MediatR.Reports.Manager.GetSessionReport;

namespace SC.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/manager/reports")]
[Authorize(Roles = "Manager")]
public sealed class ReportsManagerController(IMediator mediator) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(
        [FromQuery] GetReportSummaryQuery query,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(query, cancellationToken);
        return result.IsFailure
            ? StatusCode(result.Error?.HttpStatusCode ?? StatusCodes.Status400BadRequest, result)
            : Ok(result);
    }

    [HttpGet("sessions")]
    public async Task<IActionResult> GetSessions(
        [FromQuery] GetSessionReportQuery query,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(query, cancellationToken);
        return result.IsFailure
            ? StatusCode(result.Error?.HttpStatusCode ?? StatusCodes.Status400BadRequest, result)
            : Ok(result);
    }

    [HttpGet("order-issues")]
    public async Task<IActionResult> GetOrderIssues(
        [FromQuery] GetOrderIssuesReportQuery query,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(query, cancellationToken);
        return result.IsFailure
            ? StatusCode(result.Error?.HttpStatusCode ?? StatusCodes.Status400BadRequest, result)
            : Ok(result);
    }

    [HttpGet("refund-policies")]
    public async Task<IActionResult> GetRefundPolicies(
        [FromQuery] GetRefundPoliciesReportQuery query,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(query, cancellationToken);
        return result.IsFailure
            ? StatusCode(result.Error?.HttpStatusCode ?? StatusCodes.Status400BadRequest, result)
            : Ok(result);
    }
}
