# Auto-assign sortOrder on category and menu item creation

**Date:** 2026-05-19
**Auth required:** Admin or SuperAdmin bearer token

---

## Changes

### 1. `POST /api/admin/restaurants/{restaurantId}/categories` — **`sortOrder` removed from request** ⚠️ Breaking

**Before:** caller had to supply `sortOrder` manually.

**After:** `sortOrder` is no longer accepted. The backend assigns `max(existing) + 1` automatically, so the new category always lands at the end of the list. First category in an empty restaurant gets `sortOrder = 1`.

```jsonc
// CreateCategoryRequest — before
{
  "name": "string",
  "sortOrder": 3,    // REMOVED — no longer accepted
  "isVisible": true
}

// CreateCategoryRequest — after
{
  "name": "string",
  "isVisible": true
}
```

To reorder categories after creation, use `PUT /api/admin/categories/{id}` or `PATCH /api/admin/categories/{id}` which still accept `sortOrder`.

---

### 2. `POST /api/admin/menu/items` — **`sortOrder` removed from request** ⚠️ Breaking

**Before:** caller had to supply `sortOrder` manually.

**After:** `sortOrder` is no longer accepted. The backend assigns `max(existing in restaurant) + 1` automatically.

```jsonc
// CreateMenuItemRequest — before
{
  "restaurantId": "uuid",
  "categoryId": "uuid",
  "name": "string",
  "description": "string",
  "shortDescription": "string",
  "price": 1500.00,
  "isAvailable": true,
  "sortOrder": 5    // REMOVED — no longer accepted
}

// CreateMenuItemRequest — after
{
  "restaurantId": "uuid",
  "categoryId": "uuid",
  "name": "string",
  "description": "string",
  "shortDescription": "string",
  "price": 1500.00,
  "isAvailable": true
}
```

To reorder items after creation, use `PUT /api/admin/menu/items/{id}` or `PATCH /api/admin/menu/items/{id}` which still accept `sortOrder`.
