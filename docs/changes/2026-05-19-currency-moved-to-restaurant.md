# Currency moved from MenuItem to Restaurant

**Date:** 2026-05-19
**Auth required:** SuperAdmin bearer token (create/update restaurant)

---

## Changes

### 1. `RestaurantDto` — **`currency` field added** ⚠️ Breaking on create/update

Currency is now a property of the restaurant. Every menu item in a restaurant shares one currency.

```jsonc
// RestaurantDto — new field
{
  "id": "uuid",
  "name": "string",
  "address": "string",
  "latitude": 0.0,
  "longitude": 0.0,
  "currency": "KZT",       // NEW — ISO 4217 code, e.g. KZT / USD / EUR
  "timeZone": "string",
  "openTime": "09:00:00",
  "closeTime": "22:00:00",
  "isActive": true,
  "isOpenNow": true,
  "distanceKm": null
}
```

**`POST /api/admin/restaurants`** and **`PUT /api/admin/restaurants/{id}`** — `currency` is now **required**:

```jsonc
// CreateRestaurantRequest / UpdateRestaurantRequest — currency added (required)
{
  "name": "string",
  "address": "string",
  "latitude": 43.25,
  "longitude": 76.95,
  "currency": "KZT",       // REQUIRED — must be a valid ISO 4217 code
  "timeZone": "Asia/Almaty",
  "openTime": "09:00:00",
  "closeTime": "22:00:00"
}
```

Missing or invalid ISO 4217 code → **400 Bad Request**.

---

### 2. `MenuItemDto` — **`currency` field removed** ⚠️ Breaking

`currency` is gone from menu item responses. Read it from the parent restaurant.

```jsonc
// MenuItemDto — currency removed
{
  "id": "uuid",
  "restaurantId": "uuid",
  "categoryId": "uuid",
  "name": "string",
  "description": "string",
  "shortDescription": "string",
  "price": 1500.00,
  // "currency" — REMOVED
  "imageUrl": null,
  "isAvailable": true,
  "isActive": true,
  "sortOrder": 0
}
```

**`POST /api/admin/menu/items`** and **`PUT /api/admin/menu/items/{id}`** — `currency` no longer accepted in the request body.
