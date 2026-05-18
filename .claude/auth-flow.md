# Tandur Auth Flow — Frontend Reference

Base URL: `https://your-api/api`

All request bodies are JSON. All responses are JSON.  
`{ accessToken, refreshToken, expiresAt }` = `TokenResponse`.

---

## Mobile Client (React Native)

### Registration and login flow (same steps for both)

Steps 1–4 are identical for new and returning users. Step 5 determines which path to take.

```
1. POST /auth/session/phone          { phoneNumber }
   → 200 { message, retryAfterSeconds: 60 }

2. POST /auth/session/phone/verify   { phoneNumber, code }
   → 200 { sessionToken }           ← store this, expires in 15 min

3. POST /auth/session/email          { sessionToken, email }
   → 200 { message, retryAfterSeconds: 60 }

4. POST /auth/session/email/verify   { sessionToken, email, code }
   → 200 { sessionToken }           ← upgraded token (both phone + email confirmed)

5. POST /auth/login                  { sessionToken }
   → 200 TokenResponse              ← existing user, store tokens and done
   → 404                            ← no account found, proceed to step 6

6. POST /auth/register               { sessionToken, fullName }   ← only if step 5 returned 404
   → 200 TokenResponse              ← new user created, store tokens
```

**How to branch after step 5:**
```js
const loginRes = await fetch('/api/auth/login', { body: { sessionToken } });
if (loginRes.ok) {
  // existing user — store tokens, navigate to home
} else if (loginRes.status === 404) {
  // new user — show name input screen, then call /auth/register
}
```

### Token refresh

```
POST /auth/refresh  { refreshToken }
→ 200 TokenResponse
```

Call this when you receive a `401` on any authenticated request, or proactively when `expiresAt` is near.

### Account deletion (requires OTP)

```
1. POST /me/delete   (no body, uses registered phone)   Bearer token required
   → 200 { message, retryAfterSeconds: 60 }

2. DELETE /me        { code }                           Bearer token required
   → 204
```

---

## Profile Management (`/me/*`)

All endpoints require `Authorization: Bearer <accessToken>`.

| Method | Route | Body | Purpose |
|--------|-------|------|---------|
| `GET` | `/me` | — | Get own profile (unmasked) |
| `PATCH` | `/me` | `{ firstName, lastName, dateOfBirth? }` | Update name and/or birthday (`dateOfBirth: null` clears it) |
| `POST` | `/me/phone` | `{ newPhone }` | Send OTP to new phone |
| `PATCH` | `/me/phone` | `{ newPhone, code }` | Verify OTP + update phone |
| `POST` | `/me/email` | `{ newEmail }` | Send OTP to new email |
| `PATCH` | `/me/email` | `{ newEmail, code }` | Verify OTP + update email |
| `POST` | `/me/delete` | — | Send deletion OTP to registered phone |
| `DELETE` | `/me` | `{ code }` | Verify OTP + delete account |
| `POST` | `/me/password` | — | Send password reset link to email (**admin only**) |
| `PATCH` | `/me/password` | `{ token, newPassword }` | Reset password with token (**admin only**) |

`MeDto` response shape:
```json
{
  "id": "...",
  "firstName": "John",
  "lastName": "Doe",
  "email": "john@example.com",
  "emailConfirmed": true,
  "phone": "+79001234567",
  "phoneNumberConfirmed": true,
  "dateOfBirth": null,
  "roles": ["User"],
  "createdAt": "2025-01-01T00:00:00Z"
}
```

---

## Admin Panel (Web)

### First-time login flow (new admin created via CLI)

```
1. POST /admin/auth/login           { email, password }
   → 200 { requiresPasswordChange: true, token }   ← scoped JWT, scope=change_password

2. POST /admin/auth/change-password { newPassword }   Bearer: scoped token
   → 200 TokenResponse                               ← full JWT pair, store it

   After this, admin must set up their phone via POST /me/phone (send OTP)
   and PATCH /me/phone (verify), then set their name via PATCH /me/name.
```

### Regular admin login

```
POST /admin/auth/login   { email, password }
→ 200 TokenResponse
```

### Token refresh

```
POST /auth/refresh   { refreshToken }
→ 200 TokenResponse
```

Same endpoint as mobile. The server detects the client type from the stored refresh token and applies the correct expiry (web = 2 days, mobile = 30 days).

### Logout

```
POST /admin/auth/logout   { refreshToken }   Bearer token required (AdminPanel policy)
→ 204
```

Revokes the current refresh token (current device only). Access token stays valid until it naturally expires.

### Password change (voluntarily, while logged in)

```
1. POST /me/password   (no body)           Bearer token required (AdminPanel policy)
   → 200 { message }
   A password reset token is sent to the admin's registered email.
   ⚠ In development the token is logged to Seq — not emailed.

2. PATCH /me/password  { token, newPassword }   Bearer token required (AdminPanel policy)
   → 204   All existing sessions revoked
```

The token from step 1 is a one-time Identity reset token. In production it will arrive as a link in an email; for now pick it up from Seq logs at http://localhost:5341.

### Admin mobile app access

Once an admin has set up their phone via `/me/phone` (POST + PATCH), they can log into the mobile app using the standard OTP flow:

```
POST /auth/session/phone → POST /auth/session/phone/verify
→ POST /auth/session/email → POST /auth/session/email/verify
→ POST /auth/login
```

The returned JWT contains both `Admin` and `User` roles — grants access to both mobile app and admin panel with separate refresh token TTLs.

---

## OTP expiry windows

| OTP type | Expires |
|----------|---------|
| Phone (registration / login) | 5 minutes |
| Email (registration / login) | 10 minutes |
| Session token | 15 minutes |
| Profile change (phone / email / delete) | 10 minutes |

## Resend / rate limiting

There is no separate resend endpoint — call the same send endpoint again.

Every OTP send endpoint enforces:
- **60-second cooldown** between sends per phone/email
- **5 sends maximum per hour** per phone/email

**Success response:**
```json
200 OK
{ "message": "OTP sent.", "retryAfterSeconds": 60 }
```
Use `retryAfterSeconds` to show a countdown timer and disable the resend button.

**Rate limited response:**
```json
429 Too Many Requests
{ "message": "Please wait before requesting a new code.", "retryAfterSeconds": 42 }
```

---

## Error shapes

```json
{ "message": "..." }          // single-message errors (401, 404, 400 OTP, 429)
{ "errors": ["...", "..."] }  // validation / identity errors (400)
```

## Conflict responses (409)

Returned when a phone or email is already registered to another account:
```json
409 Conflict
{ "message": "This phone number is already registered. Please log in." }
```
Occurs on: `POST /auth/register`, `POST /me/phone`, `PATCH /me/phone`, `POST /me/email`, `PATCH /me/email`.

## Scoped token guard

If an admin has a `scope=change_password` JWT (issued at first login), all `/me/*` endpoints return `403 Forbidden`. The admin must complete `POST /admin/auth/change-password` first to receive a full token.
