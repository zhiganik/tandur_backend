using Core.Interfaces;
using Infrastructure.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Stripe;
using Swashbuckle.AspNetCore.Annotations;

namespace Api.Controllers;

[ApiController]
[Route("api/webhooks")]
[Tags("Webhooks")]
[Produces("application/json")]
public class StripeWebhookController(
    IOrderService                    orderService,
    IOptions<StripeOptions>          stripeOptions,
    ILogger<StripeWebhookController> logger) : ControllerBase
{
    [HttpPost("stripe")]
    [AllowAnonymous]
    [SwaggerOperation(Summary = "Stripe webhook — handles payment_intent.succeeded and payment_intent.payment_failed")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Stripe()
    {
        var json      = await new StreamReader(Request.Body).ReadToEndAsync();
        var signature = Request.Headers["Stripe-Signature"].ToString();

        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(
                json, signature, stripeOptions.Value.WebhookSecret);
        }
        catch (StripeException ex)
        {
            logger.LogWarning("Stripe webhook signature invalid: {Message}", ex.Message);
            return BadRequest();
        }

        if (stripeEvent.Type == EventTypes.PaymentIntentSucceeded)
        {
            var intent  = (PaymentIntent)stripeEvent.Data.Object;
            var orderId = Guid.Parse(intent.Metadata["orderId"]);
            await orderService.MarkAsPaidAsync(orderId, intent.Id);
        }

        if (stripeEvent.Type == EventTypes.PaymentIntentPaymentFailed)
        {
            var intent  = (PaymentIntent)stripeEvent.Data.Object;
            var orderId = Guid.Parse(intent.Metadata["orderId"]);
            await orderService.MarkAsFailedAsync(orderId);
        }

        return Ok();
    }
}
