# RBAC overhaul — SuperAdmin vs Admin split and restaurant assignments

**Date:** 2026-05-19
**Auth required:** SuperAdmin bearer token for all new/restricted endpoints

---

## Roles quick reference

| Role | Issued by | Access |
|------|-----------|--------|
| `User` | Self-registration via OTP | Mobile app only |
| `Admin` | Created by SuperAdmin via CLI | Read restaurants + update basic fields |
| `SuperAdmin` | CLI with `--super` flag | Everything |

Bearer token is the same JWT for all roles — role is encoded in the claims.

---

## Changes

### 1. `GET /api/admin/users` — **restricted to SuperAdmin**

**Before:** `Admin` and `SuperAdmin`. Email/phone were masked.

**After:** `SuperAdmin` only. Returns full unmasked PII + `restaurants` array per user. `Admin` → **403**.

```jsonc
// UserDto — new field added
{
  "id": "string",
  "firstName": "string",
  "lastName": "string",
  "email": "string",           // unmasked
  "phone": "string",           // unmasked
  "emailConfirmed": true,
  "phoneNumberConfirmed": true,
  "roles": ["Admin"],
  "createdAt": "2026-05-19T00:00:00Z",
  "restaurants": [             // NEW — [] for regular Users
    { "id": "uuid", "name": "string" }
  ]
}
```

`restaurants` per role: SuperAdmin entry → all restaurants · Admin entry → assigned restaurants only · User entry → `[]`.

---

### 2. `GET /api/admin/users/{id}` — **new endpoint**

**Before:** did not exist.

**After:** SuperAdmin only. Returns a single `UserDto` (same shape, unmasked, with `restaurants`).

```
GET /api/admin/users/{id}
Authorization: Bearer <superadmin-token>

200 → UserDto
404 → user not found
403 → not SuperAdmin
```

---

### 3. `POST /api/admin/restaurants` — **restricted to SuperAdmin**

**Before:** Admin and SuperAdmin.

**After:** SuperAdmin only. Admin → **403**. Request/response shape unchanged.

---

### 4. `PATCH /api/admin/restaurants/{id}` — **restricted to SuperAdmin**

**Before:** Admin and SuperAdmin.

**After:** SuperAdmin only. Admin → **403**. Request/response shape unchanged.

---

### 5. `DELETE /api/admin/restaurants/{id}` — **restricted to SuperAdmin**

**Before:** Admin and SuperAdmin.

**After:** SuperAdmin only. Admin → **403**.

---

### 6. `POST /api/admin/users/{adminId}/restaurants/{restaurantId}` — **new endpoint**

Assigns a restaurant to an Admin. SuperAdmin only.

```
204  → assigned (idempotent)
400  → target is not Admin  { "message": "Target user must have the Admin role." }
400  → target is SuperAdmin { "message": "SuperAdmin already has access to all restaurants." }
404  → user or restaurant not found
403  → not SuperAdmin
```

---

### 7. `DELETE /api/admin/users/{adminId}/restaurants/{restaurantId}` — **new endpoint**

Removes a restaurant assignment from an Admin. SuperAdmin only.

```
204  → unassigned
400  → target is SuperAdmin { "message": "SuperAdmin already has access to all restaurants." }
404  → assignment not found
403  → not SuperAdmin
```

---

### 8. `POST /api/admin/users/{id}/password/reset` — **new endpoint**

SuperAdmin triggers a password-reset email to a target Admin.

```
200 → { "message": "Password reset link sent to admin's email." }
400 → target is not Admin
404 → user not found
403 → not SuperAdmin
```

Admin completes reset via:
```
PATCH /api/me/password
Body: { "token": "<from email>", "newPassword": "string" }
```

---

### 9. `GET /api/me` — **`restaurants` field added**

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
  "restaurants": [    // NEW — always present, may be []
    { "id": "uuid", "name": "string" }
  ]
}
```
