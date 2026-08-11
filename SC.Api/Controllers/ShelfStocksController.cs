using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SC.Application.MediatR.ShelfStockAdmin.CreateShelfStock;
using SC.Application.MediatR.ShelfStockAdmin.GetShelfStocks;
using SC.Application.MediatR.ShelfStockAdmin.GetShelfStockDetail;
using SC.Application.MediatR.ShelfStockAdmin.RefillShelfStock;

namespace SC.Api.Controllers;

/// <summary>
/// Tồn kho trên kệ theo session. Staff xem + xác nhận refill (BR-152);
/// Manager khởi tạo dòng tồn.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/manager/shelf-stocks")]
[Authorize(Roles = "Manager,Staff")]
public class ShelfStocksController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetBySession([FromQuery] Guid sessionId, CancellationToken ct)
    {
        var result = await mediator.Send(new GetShelfStocksQuery(sessionId), ct);
        return result.IsFailure
            ? StatusCode(result.Error?.HttpStatusCode ?? 400, result)
            : Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetShelfStockDetailQuery(id), ct);
        return result.IsFailure
            ? StatusCode(result.Error?.HttpStatusCode ?? 400, result)
            : Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> Create([FromBody] CreateShelfStockCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return result.IsFailure
            ? StatusCode(result.Error?.HttpStatusCode ?? 400, result)
            : Ok(result);
    }

    /// <summary>Staff xác nhận đã nạp thêm hộp vào lane (quantity cộng dồn).</summary>
    [HttpPatch("{id:guid}/refill")]
    public async Task<IActionResult> Refill(
        [FromRoute] Guid id, [FromBody] RefillShelfStockCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command with { Id = id }, ct);
        return result.IsFailure
            ? StatusCode(result.Error?.HttpStatusCode ?? 400, result)
            : Ok(result);
    }
}
