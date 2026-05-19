# API Changelog

Changes are listed newest-first. Each entry describes what changed, the before/after shapes, and the required token/role.

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
