# Stripe payments — Orders feature

**Date:** 2026-05-20
**Auth required:** varies per endpoint — see table below

---

## New endpoints

### Client endpoints — `Bearer User JWT`

#### `POST /api/orders/checkout`

Creates an order and a Stripe PaymentIntent. The client passes this directly to the Stripe SDK to present the payment sheet.

**Request:**
```jsonc
{
  "restaurantId": "uuid",
  "items": [
    { "menuItemId": "uuid", "quantity": 2 },
    { "menuItemId": "uuid", "quantity": 1 }
  ]
}
```

**Response `200`:**
```jsonc
{
  "orderId": "uuid",
  "clientSecret": "pi_xxx_secret_yyy"  // pass to Stripe SDK
}
```

**Validation errors `400`:** empty items list, quantity outside 1–50, item unavailable, item from wrong restaurant, zero total.

---

#### `GET /api/orders`

Own order history, sorted newest-first.

**Query params:**
| Param | Type | Notes |
|-------|------|-------|
| `page` | int | default 1 |
| `limit` | int | default 20 |
| `status` | string | `PendingPayment` \| `Paid` \| `Cancelled` \| `Refunded` |

**Response `200`:** `PagedResult<OrderDto>` — see shape below.

---

#### `GET /api/orders/{id}`

Single order detail. Returns `404` if the order belongs to a different user.

---

### Admin endpoints — `Bearer Admin JWT`

#### `GET /api/admin/orders`

All orders across all users. Supports exact search and range filters.

**Query params:**
| Param | Type | Notes |
|-------|------|-------|
| `page` | int | default 1 |
| `limit` | int | default 20, max 100 |
| `orderId` | uuid | exact match |
| `userId` | string | exact match |
| `restaurantId` | uuid | exact match |
| `status` | string | `PendingPayment` \| `Paid` \| `Cancelled` \| `Refunded` |
| `minTotal` | decimal | inclusive |
| `maxTotal` | decimal | inclusive |
| `sort` | string | `asc` \| `desc` (default) |

**Response `200`:** `PagedResult<OrderDto>`

---

#### `GET /api/admin/orders/stats`

Dashboard widget data. Two queries: today's activity + all-time counts by status.

**Response `200`:**
```jsonc
{
  "totalToday": 12,
  "revenueToday": 340.50,
  "pendingCount": 3,
  "paidCount": 145,
  "cancelledCount": 8,
  "refundedCount": 2
}
```

---

#### `GET /api/admin/orders/{id}`

Any order by ID, no ownership check.

---

#### `POST /api/admin/orders/{id}/refund`

Issues a full Stripe refund. Only valid for `Paid` orders.

**Request:**
```jsonc
{ "reason": "requested_by_customer" }  // optional
```

**Response `200`:** `{ "message": "Refund issued." }`
**Response `400`:** order not in `Paid` status.

---

#### `POST /api/admin/orders/{id}/cancel`

Cancels an abandoned `PendingPayment` order. No Stripe call — the PaymentIntent expires on Stripe's side.

**Response `204`**
**Response `400`:** order not found or not in `PendingPayment` status.

---

### Webhook — `No auth (signature-verified)`

#### `POST /api/webhooks/stripe`

Stripe calls this automatically. Do not call manually.

Handles: `payment_intent.succeeded` → order `Paid`, `payment_intent.payment_failed` → order `Cancelled`.

---

## Shared DTO shapes

### `OrderDto`
```jsonc
{
  "id": "uuid",
  "restaurantId": "uuid",
  "currency": "UZS",
  "total": 125000.00,
  "status": "Paid",            // PendingPayment | Paid | Cancelled | Refunded
  "createdAt": "2026-05-20T10:00:00Z",
  "items": [
    {
      "menuItemId": "uuid",
      "name": "Caesar Salad",   // snapshot at order time
      "unitPrice": 45000.00,    // snapshot at order time
      "quantity": 2,
      "lineTotal": 90000.00
    }
  ]
}
```

### `PagedResult<OrderDto>`
```jsonc
{
  "data": [ /* OrderDto[] */ ],
  "total": 42,
  "page": 1,
  "limit": 20,
  "totalPages": 3
}
```
