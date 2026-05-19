# MenuItem isActive now writable via PATCH and PUT

**Date:** 2026-05-19
**Auth required:** Admin or SuperAdmin bearer token

---

## Changes

### 1. `PATCH /api/admin/menu/items/{id}` — **`isActive` added**

`isActive` can now be toggled directly without going through DELETE (which also sets it to false).

```jsonc
// PatchMenuItemRequest — isActive added (optional, omit to leave unchanged)
{
  "isAvailable": false,
  "isActive": false,      // NEW
  "price": null,
  "categoryId": null,
  "sortOrder": null
}
```

---

### 2. `PUT /api/admin/menu/items/{id}` — **`isActive` added**

```jsonc
// UpdateMenuItemRequest — isActive added (optional, defaults to true)
{
  "name": "string",
  "description": "string",
  "shortDescription": "string",
  "price": 1500.00,
  "categoryId": "uuid",
  "sortOrder": 0,
  "isAvailable": true,
  "isActive": true         // NEW — defaults to true if omitted
}
```
