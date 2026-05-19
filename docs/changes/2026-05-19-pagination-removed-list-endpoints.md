# Pagination removed from restaurant, category, and menu list endpoints

**Date:** 2026-05-19
**Auth required:** Bearer token (any role)

---

## Changes

All affected endpoints now return a **plain array** instead of a `PagedResult<T>` wrapper. Remove `?page=` and `?limit=` — they are no longer accepted.

> ⚠️ Breaking — unwrap `.data` and drop pagination params on all three resources below.

---

### 1. `GET /api/restaurants` and `GET /api/admin/restaurants`

```jsonc
// Before
{ "data": [...RestaurantDto], "total": 10, "page": 1, "limit": 20, "totalPages": 1 }

// After
[ ...RestaurantDto ]
```

---

### 2. `GET /api/restaurants/{id}/categories` and `GET /api/admin/restaurants/{id}/categories`

```jsonc
// Before
{ "data": [...CategoryDto], "total": 5, "page": 1, "limit": 20, "totalPages": 1 }

// After
[ ...CategoryDto ]
```

---

### 3. `GET /api/restaurants/{id}/menu` and `GET /api/admin/restaurants/{id}/menu` — `items` field

```jsonc
// Before — items was a PagedResult
{
  "categories": [...CategoryDto],
  "items": { "data": [...MenuItemDto], "total": 12, "page": 1, "limit": 20, "totalPages": 1 }
}

// After — items is a plain array
{
  "categories": [...CategoryDto],
  "items": [...MenuItemDto]
}
```

**`GET /api/admin/users` keeps its `PagedResult` wrapper — no change.**
