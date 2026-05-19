# Remove PUT /api/admin/users/me; restrict user deletion to SuperAdmin

**Date:** 2026-05-19
**Auth required:** SuperAdmin bearer token

---

## Changes

### 1. `PUT /api/admin/users/me` — **removed** ⚠️ Breaking

**Before:** `Admin` and `SuperAdmin` could update their own `firstName`, `lastName`, and `phoneNumber` via this endpoint. Phone was saved as confirmed without OTP verification.

**After:** endpoint is gone.

Use the standard `/me` endpoints instead:
- Update name → `PATCH /api/me` with `{ "firstName": "...", "lastName": "..." }`
- Change phone → `POST /api/me/phone` then `PATCH /api/me/phone` (OTP-verified)

---

### 2. `DELETE /api/admin/users/{id}` — **restricted to SuperAdmin**

**Before:** accessible by `Admin` and `SuperAdmin`.

**After:** `SuperAdmin` only. `Admin` tokens now get **403 Forbidden**.

Route and behaviour are otherwise unchanged.
