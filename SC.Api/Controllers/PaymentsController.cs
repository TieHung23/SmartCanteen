using System.Text.Json;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SC.Application.MediatR.Payment.HandleSepayIpn;
using SC.Application.MediatR.Payment.GetPaymentById;
using SC.Application.MediatR.Payment.TopUpWallet;

namespace SC.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/[controller]")]
[Authorize]
public class PaymentsController(IMediator mediator) : ControllerBase
{
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetPaymentById([FromRoute] Guid id)
    {
        var result = await mediator.Send(new GetPaymentByIdQuery(id));

        if (result.IsFailure)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    [HttpPost("top-up")]
    public async Task<IActionResult> TopUpWallet([FromBody] TopUpWalletCommand command)
    {
        var result = await mediator.Send(command);

        if (result.IsFailure)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpPost("sepay/ipn")]
    [AllowAnonymous]
    public async Task<IActionResult> HandleSepayIpn([FromBody] JsonElement request)
    {
        if (request.ValueKind != JsonValueKind.Object)
        {
            return BadRequest(new
            {
                success = false,
                message = "SePay IPN payload must be a JSON object."
            });
        }

        var result = await mediator.Send(new HandleSepayIpnCommand
        {
            Data = ToDictionary(request)
        });

        if (result.IsFailure)
        {
            return BadRequest(new
            {
                success = false,
                message = result.Message
            });
        }

        return Ok(new
        {
            success = true
        });
    }

    private static Dictionary<string, string> ToDictionary(JsonElement element)
    {
        var data = new Dictionary<string, string>();

        foreach (var property in element.EnumerateObject())
        {
            data[property.Name] = property.Value.ValueKind == JsonValueKind.String
                ? property.Value.GetString() ?? string.Empty
                : property.Value.ToString();
        }

        return data;
    }
}
