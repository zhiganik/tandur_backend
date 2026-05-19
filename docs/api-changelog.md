# API Changelog

Changes are listed newest-first. Each entry describes what changed, the before/after shapes, and the required token/role.

---

## 2026-05-19 — Currency on Restaurant, pagination removed, MenuItem isActive, user search

---

### 1. `RestaurantDto` — **`currency` field added** ⚠️ Breaking on create/update

**Currency is now a property of the restaurant, not of individual menu items.**

Every restaurant has a single currency that applies to all its menu items.

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

**`POST /api/admin/restaurants`** — `currency` is now **required**:

```jsonc
// CreateRestaurantRequest — currency added (required)
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

**`PUT /api/admin/restaurants/{id}`** — same, `currency` now required.

Validation error if `currency` is missing or not a valid ISO 4217 code → **400 Bad Request**.

---

### 2. `MenuItemDto` — **`currency` field removed** ⚠️ Breaking

`currency` is no longer present on menu items. Read it from the parent restaurant instead.

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

**`POST /api/admin/menu/items`** and **`PUT /api/admin/menu/items/{id}`** — `currency` field no longer accepted (ignored if sent, safe to remove from request body).

---

### 3. `PATCH /api/admin/menu/items/{id}` and `PUT /api/admin/menu/items/{id}` — **`isActive` now writable**

Both endpoints now accept `isActive` to toggle the soft-delete state of a menu item without going through DELETE.

```jsonc
// PatchMenuItemRequest — isActive added (optional)
{
  "isAvailable": false,
  "isActive": false,        // NEW — omit to leave unchanged
  "price": null,
  "categoryId": null,
  "sortOrder": null
}

// UpdateMenuItemRequest — isActive added (optional, defaults to true)
{
  "name": "string",
  "description": "string",
  "shortDescription": "string",
  "price": 1500.00,
  "categoryId": "uuid",
  "sortOrder": 0,
  "isAvailable": true,
  "isActive": true           // NEW — defaults to true if omitted
}
```

---

### 4. List endpoints — **pagination removed** ⚠️ Breaking

The following endpoints previously returned a `PagedResult<T>` wrapper. They now return a plain array. Remove `?page=` and `?limit=` query params — they are no longer accepted.

#### Restaurants

**`GET /api/restaurants`** and **`GET /api/admin/restaurants`**

```jsonc
// Before
{
  "data": [ ...RestaurantDto ],
  "total": 10,
  "page": 1,
  "limit": 20,
  "totalPages": 1
}

// After — plain array
[ ...RestaurantDto ]
```

#### Categories

**`GET /api/restaurants/{id}/categories`** and **`GET /api/admin/restaurants/{id}/categories`**

```jsonc
// Before
{
  "data": [ ...CategoryDto ],
  "total": 5,
  "page": 1,
  "limit": 20,
  "totalPages": 1
}

// After — plain array
[ ...CategoryDto ]
```

#### Menu (`MenuDto.items`)

**`GET /api/restaurants/{id}/menu`** and **`GET /api/admin/restaurants/{id}/menu`**

```jsonc
// Before — items was a PagedResult
{
  "categories": [ ...CategoryDto ],
  "items": {
    "data": [ ...MenuItemDto ],
    "total": 12,
    "page": 1,
    "limit": 20,
    "totalPages": 1
  }
}

// After — items is a plain array
{
  "categories": [ ...CategoryDto ],
  "items": [ ...MenuItemDto ]
}
```

**Users (`GET /api/admin/users`) keeps its `PagedResult` wrapper** — no change there.

---

### 5. `GET /api/admin/users` — **search, filter, and sort added**

All parameters are optional and can be combined freely.

| Param | Type | Description |
|-------|------|-------------|
| `search` | `string` | Substring match on email or phone number; exact match on user ID |
| `role` | `string` (repeatable) | Filter by role — `User`, `Admin`, or `SuperAdmin`. Repeat for multiple: `?role=Admin&role=SuperAdmin` |
| `restaurantId` | `uuid` | Return only users assigned to this restaurant |
| `sort` | `asc` \| `desc` | Sort by registration date. Default: `desc` (newest first) |
| `page` | `int` | Page number, default `1` |
| `limit` | `int` | Page size, 1–100, default `20` |

```
GET /api/admin/users?search=alice&role=Admin&sort=asc&page=1&limit=20
Authorization: Bearer <superadmin-token>
```

Response shape is unchanged (`PagedResult<UserDto>`).

---

## 2026-05-19 — Remove PUT /api/admin/users/me; restrict user deletion to SuperAdmin

### 1. `PUT /api/admin/users/me` — **removed**

**Before:** `Admin` and `SuperAdmin` could call this to update their own `firstName`, `lastName`, and `phoneNumber`. The phone was saved as confirmed without OTP verification.

**After:** endpoint is gone. Use the existing endpoints on `GET /api/me` instead:
- Update name: `PATCH /api/me` with `{ "firstName": "...", "lastName": "..." }`
- Change phone: `POST /api/me/phone` → `PATCH /api/me/phone` (OTP-verified)

---

### 2. `DELETE /api/admin/users/{id}` — **access restricted to SuperAdmin**

**Before:** accessible by `Admin` and `SuperAdmin`.

**After:** `SuperAdmin` only. `Admin` tokens now get **403**.

Route and behaviour are otherwise unchanged.

---

## 2026-05-19 — RBAC Overhaul: SuperAdmin vs Admin split + Restaurant assignments

### Roles quick reference

| Role | Issued by | What they can do |
|------|-----------|-----------------|
| `User` | Self-registration via OTP | Mobile app features only |
| `Admin` | Created by SuperAdmin via CLI | Read restaurant list + update basic restaurant fields |
| `SuperAdmin` | Created by CLI with `--super` flag | Everything |

Bearer token is the same JWT for all roles — the role is encoded in the token claims.

---

### 1. `GET /api/admin/users` — **access restricted to SuperAdmin**

**Before:** accessible by `Admin` and `SuperAdmin`. Email/phone were masked (`j***@example.com`, `+79***567`).

**After:** `SuperAdmin` only. Returns full unmasked PII and a `restaurants` array per user. `Admin` tokens now get **403**.

Response shape change — `UserDto` gains a new field:

```jsonc
// UserDto — new field added
{
  "id": "string",
  "firstName": "string",
  "lastName": "string",
  "email": "string",        // unmasked now (SuperAdmin only sees this endpoint)
  "phone": "string",        // unmasked now
  "emailConfirmed": true,
  "phoneNumberConfirmed": true,
  "roles": ["Admin"],
  "createdAt": "2026-05-19T00:00:00Z",
  "restaurants": [          // NEW — empty [] for regular Users
    { "id": "uuid", "name": "string" }
  ]
}
```

`restaurants` content by role:
- `SuperAdmin` user entry → all restaurants in the app
- `Admin` user entry → only restaurants assigned to that admin
- `User` user entry → `[]`

---

### 2. `GET /api/admin/users/{id}` — **new endpoint**

**Before:** did not exist.

**After:** `SuperAdmin` only. Returns a single `UserDto` (same shape as above, unmasked, with `restaurants`).

```
GET /api/admin/users/{id}
Authorization: Bearer <superadmin-token>

200 OK  → UserDto
404     → user not found
403     → not SuperAdmin
```

---

### 3. `POST /api/admin/restaurants` — **access restricted to SuperAdmin**

**Before:** accessible by `Admin` and `SuperAdmin`.

**After:** `SuperAdmin` only. `Admin` tokens now get **403**.

Route, request body, and response shape are unchanged.

---

### 4. `PUT /api/admin/restaurants/{id}` — **unchanged**

Still accessible by both `Admin` and `SuperAdmin`. Updates: `name`, `address`, `latitude`, `longitude`, `openTime`, `closeTime`.

---

### 5. `PATCH /api/admin/restaurants/{id}` — **access restricted to SuperAdmin**

**Before:** accessible by `Admin` and `SuperAdmin`. Used to toggle `isActive`.

**After:** `SuperAdmin` only. `Admin` tokens now get **403**.

Route, request body, and response shape are unchanged.

```jsonc
// PatchRestaurantRequest — unchanged
{ "isActive": true }
```

---

### 6. `DELETE /api/admin/restaurants/{id}` — **access restricted to SuperAdmin**

**Before:** accessible by `Admin` and `SuperAdmin`.

**After:** `SuperAdmin` only. `Admin` tokens now get **403**.

---

### 7. `POST /api/admin/users/{adminId}/restaurants/{restaurantId}` — **new endpoint**

Assigns a restaurant to an Admin. SuperAdmin only.

```
POST /api/admin/users/{adminId}/restaurants/{restaurantId}
Authorization: Bearer <superadmin-token>

204  → assigned successfully (or was already assigned — idempotent)
400  → target user is not an Admin role
      { "message": "Target user must have the Admin role." }
400  → target user is a SuperAdmin
      { "message": "SuperAdmin already has access to all restaurants." }
404  → user or restaurant not found
403  → not SuperAdmin
```

---

### 8. `DELETE /api/admin/users/{adminId}/restaurants/{restaurantId}` — **new endpoint**

Removes a restaurant assignment from an Admin. SuperAdmin only.

```
DELETE /api/admin/users/{adminId}/restaurants/{restaurantId}
Authorization: Bearer <superadmin-token>

204  → unassigned successfully
400  → target user is a SuperAdmin
      { "message": "SuperAdmin already has access to all restaurants." }
404  → assignment not found
403  → not SuperAdmin
```

---

### 9. `POST /api/admin/users/{id}/password/reset` — **new endpoint**

SuperAdmin triggers a password reset email for a target Admin. The Admin receives an email with a reset token and completes the flow themselves via `PATCH /api/me/password`.

```
POST /api/admin/users/{id}/password/reset
Authorization: Bearer <superadmin-token>

200 OK  → { "message": "Password reset link sent to admin's email." }
400     → target user is not an Admin
          { "message": "Target user must have the Admin role." }
404     → user not found
403     → not SuperAdmin
```

The target Admin then resets via the existing flow:
```
PATCH /api/me/password
Authorization: Bearer <admin-token>
Body: { "token": "<from email>", "newPassword": "string" }
```

---

### 10. `GET /api/me` — **response shape changed**

**Before:** `MeDto` had no restaurant information.

**After:** `MeDto` gains a `restaurants` field.

```jsonc
// MeDto — new field added
{
  "id": "string",
  "firstName": "string",
  "lastName": "string",
  "email": "string",
  "emailConfirmed": true,
  "phone": "string",
  "phoneNumberConfirmed": true,
  "dateOfBirth": null,
  "roles": ["Admin"],
  "createdAt": "2026-05-19T00:00:00Z",
  "restaurants": [          // NEW — always present, may be []
    { "id": "uuid", "name": "string" }
  ]
}
```

`restaurants` content by role:
- `SuperAdmin` → all restaurants in the app
- `Admin` → only assigned restaurants
- `User` → `[]`

---

### 11. New DB table: `AdminRestaurantAssignments`

Backend change only — no API surface impact. Many-to-many join table between users and restaurants. Applied automatically on API startup.
