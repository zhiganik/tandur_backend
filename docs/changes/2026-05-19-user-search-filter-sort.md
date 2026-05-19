# User list — search, filter by role/restaurant, sort by date

**Date:** 2026-05-19
**Auth required:** SuperAdmin bearer token

---

## Changes

### 1. `GET /api/admin/users` — **query params added**

All parameters are optional and stackable.

| Param | Type | Description |
|-------|------|-------------|
| `search` | `string` | Substring match on email or phone; exact match on user ID |
| `role` | `string` (repeatable) | `User`, `Admin`, or `SuperAdmin`. Repeat for OR: `?role=Admin&role=SuperAdmin` |
| `restaurantId` | `uuid` | Only users assigned to this restaurant |
| `sort` | `asc` \| `desc` | Sort by registration date. Default: `desc` |
| `page` | `int` | Default `1` |
| `limit` | `int` | 1–100, default `20` |

```
GET /api/admin/users?search=alice&role=Admin&sort=asc&page=1&limit=20
Authorization: Bearer <superadmin-token>
```

Response shape is unchanged (`PagedResult<UserDto>`).
