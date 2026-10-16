using eAutoskola.Application.Payments;
using eAutoskola.Domain.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eAutoskola.API.Controllers;

[ApiController]
[Route("api/payments")]
[Authorize(Roles = Role.Student)]
public class PaymentController : ControllerBase
{
    private readonly PaymentService _service;

    public PaymentController(PaymentService service)
    {
        _service = service;
    }

    [HttpPost("installments/{installmentId:int}/create-order")]
    public async Task<ActionResult<CreateOrderResultDto>> CreateOrder(int installmentId, CancellationToken cancellationToken)
        => Ok(await _service.CreateOrderAsync(installmentId, cancellationToken));

    [HttpPost("{orderId}/capture")]
    public async Task<ActionResult<CapturePaymentResultDto>> Capture(string orderId, CancellationToken cancellationToken)
        => Ok(await _service.CaptureAsync(orderId, cancellationToken));
}
