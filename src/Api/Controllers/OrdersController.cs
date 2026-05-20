using System.Security.Claims;
using Core.DTOs.Common;
using Core.DTOs.Orders;
using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Api.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
[Tags("Orders")]
[Produces("application/json")]
public class OrdersController(IOrderService orderService, ILogger<OrdersController> logger)
    : ControllerBase
{
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpPost("checkout")]
    [SwaggerOperation(Summary = "Create an order and receive a Stripe PaymentIntent client_secret")]
    [ProducesResponseType<CheckoutResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Checkout([FromBody] CheckoutRequest request)
    {
        var (result, error) = await orderService.CheckoutAsync(UserId, request);
        if (error is not null) return BadRequest(new { message = error });

        logger.LogInformation("Checkout initiated — Order {OrderId}", result!.OrderId);
        return Ok(result);
    }

    [HttpGet]
    [SwaggerOperation(Summary = "Get own order history")]
    [ProducesResponseType<PagedResult<OrderDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyOrders([FromQuery] MyOrderQuery query)
    {
        var result = await orderService.GetMyOrdersAsync(UserId, query);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [SwaggerOperation(Summary = "Get a specific order (own orders only)")]
    [ProducesResponseType<OrderDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrder(Guid id)
    {
        var order = await orderService.GetByIdAsync(id, UserId);
        return order is null ? NotFound() : Ok(order);
    }
}
