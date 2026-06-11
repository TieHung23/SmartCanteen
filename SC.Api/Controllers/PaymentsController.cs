using System.Text;
using System.Text.Json;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SC.Application.MediatR.Payment.HandleSepayIpn;
using SC.Application.MediatR.Payment.GetPaymentById;
using SC.Application.MediatR.Payment.TopUpWallet;
using SC.Contract.Services.Payment;

namespace SC.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/[controller]")]
[Authorize]
public class PaymentsController(
    IMediator mediator,
    ISePayWebhookVerifier webhookVerifier) : ControllerBase
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
    public async Task<IActionResult> HandleSepayIpn()
    {
        Request.EnableBuffering();
        Request.Body.Position = 0;

        using var reader = new StreamReader(
            Request.Body,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false,
            leaveOpen: true);
        var rawBody = await reader.ReadToEndAsync();
        Request.Body.Position = 0;

        var verification = webhookVerifier.Verify(
            rawBody,
            Request.Headers["X-SePay-Signature"].ToString(),
            Request.Headers["X-SePay-Timestamp"].ToString());

        if (!verification.IsValid)
        {
            return Unauthorized(new
            {
                success = false,
                message = "Unauthorized webhook request."
            });
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(rawBody);
        }
        catch (JsonException)
        {
            return BadRequest(new
            {
                success = false,
                message = "SePay IPN payload must be valid JSON."
            });
        }

        using (document)
        {
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "SePay IPN payload must be a JSON object."
                });
            }

            var data = ToDictionary(document.RootElement);
            var userAgent = Request.Headers["User-Agent"].ToString();
            var isTestMode = userAgent.StartsWith("SePay-Testmode-Webhook/", StringComparison.OrdinalIgnoreCase);
            var hasPaymentCode = data.TryGetValue("code", out var code)
                    && code.StartsWith("SC-", StringComparison.OrdinalIgnoreCase)
                || data.TryGetValue("content", out var content)
                    && content.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                        .Any(part => part.StartsWith("SC-", StringComparison.OrdinalIgnoreCase));

            if (isTestMode && !hasPaymentCode)
            {
                return Ok(new
                {
                    success = true
                });
            }

            var result = await mediator.Send(new HandleSepayIpnCommand
            {
                Data = data
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
