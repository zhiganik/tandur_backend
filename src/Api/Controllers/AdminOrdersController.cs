using System.Security.Claims;
using Core.Domain.Constants;
using Core.Domain.Enums;
using Core.DTOs.Common;
using Core.DTOs.Orders;
using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Api.Controllers;

[ApiController]
[Route("api/admin/orders")]
[Authorize(Policy = TandurPolicies.AdminPanel)]
[Tags("Admin › Orders")]
[Produces("application/json")]
public class AdminOrdersController(IOrderService orderService, ILogger<AdminOrdersController> logger)
    : ControllerBase
{
    private string ActorId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet]
    [SwaggerOperation(Summary = "List all orders — search by orderId/userId/restaurantId, filter by status/total, sort by date")]
    [ProducesResponseType<PagedResult<OrderDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetOrders([FromQuery] OrderQuery query)
    {
        var result = await orderService.GetPagedAsync(query);
        return Ok(result);
    }

    [HttpGet("stats")]
    [SwaggerOperation(Summary = "Today's order totals and all-time counts by status")]
    [ProducesResponseType<OrderStatsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetStats()
    {
        var stats = await orderService.GetStatsAsync();
        return Ok(stats);
    }

    [HttpGet("{id:guid}")]
    [SwaggerOperation(Summary = "Get any order by ID")]
    [ProducesResponseType<OrderDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrder(Guid id)
    {
        var order = await orderService.GetByIdAsync(id);
        return order is null ? NotFound() : Ok(order);
    }

    [HttpPost("{id:guid}/refund")]
    [SwaggerOperation(Summary = "Issue a Stripe refund for a paid order")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Refund(Guid id, [FromBody] RefundRequest request)
    {
        var order = await orderService.GetByIdAsync(id);
        if (order is null) return NotFound();
        if (order.Status != nameof(OrderStatus.Paid))
            return BadRequest(new { message = "Only paid orders can be refunded." });

        await orderService.RefundAsync(id, request.Reason);
        logger.LogInformation("Order {OrderId} refunded by {ActorId}", id, ActorId);
        return Ok(new { message = "Refund issued." });
    }

    [HttpPost("{id:guid}/cancel")]
    [SwaggerOperation(Summary = "Cancel an abandoned pending order")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var cancelled = await orderService.CancelAsync(id);
        if (!cancelled)
            return BadRequest(new { message = "Order not found or not in PendingPayment status." });

        logger.LogInformation("Order {OrderId} cancelled by {ActorId}", id, ActorId);
        return NoContent();
    }
}
