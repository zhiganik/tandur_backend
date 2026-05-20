namespace Core.DTOs.Orders;

public record OrderStatsDto(
    int     TotalToday,
    decimal RevenueToday,
    int     PendingCount,
    int     PaidCount,
    int     CancelledCount,
    int     RefundedCount
);
