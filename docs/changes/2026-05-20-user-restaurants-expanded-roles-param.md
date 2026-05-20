# User restaurants expanded; roles query param renamed

**Date:** 2026-05-20
**Auth required:** Bearer — `Admin` or `SuperAdmin`

---

## Changes

### 1. `GET /api/admin/users` — **`restaurants` object expanded, `role` param renamed `roles`**

#### 1a. Query parameter renamed

**Before:** `?role=Admin&role=User`

**After:** `?roles=Admin&roles=User`

> ⚠️ Breaking — any client sending `?role=` will receive unfiltered results.

#### 1b. `restaurants` object shape

Each item in the `restaurants` array previously returned only `id` and `name`. It now returns the full restaurant object (same shape as `GET /api/admin/restaurants`). `isOpenNow` is always `false` in this context (schedules are not loaded for user list queries).

**Before:**
```jsonc
{
  "id": "...",
  "firstName": "John",
  "restaurants": [
    { "id": "uuid", "name": "My Cafe" }   // only id + name
  ]
}
```

**After:**
```jsonc
{
  "id": "...",
  "firstName": "John",
  "restaurants": [
    {
      "id": "uuid",
      "name": "My Cafe",         // unchanged
      "address": "...",          // NEW
      "latitude": 41.2,          // NEW
      "longitude": 69.2,         // NEW
      "currency": "UZS",         // NEW
      "timeZone": "Asia/Tashkent", // NEW
      "isActive": true,          // NEW
      "isOpenNow": false,        // NEW — always false in this context
      "distanceKm": null         // NEW — always null in this context
    }
  ]
}
```

> ⚠️ Breaking — clients doing strict schema validation will reject the extra fields.

---

### 2. `GET /api/admin/users/{id}` — **`restaurants` object expanded**

Same shape change as §1b above. No route or auth change.

---

### 3. `GET /api/me` — **`restaurants` object expanded**

Same shape change as §1b above. Applies to all roles; regular `User` accounts still receive an empty array.
