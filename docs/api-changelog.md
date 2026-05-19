# API Changelog

Each change is documented in its own file under `docs/changes/`. Listed newest-first.

---

## 2026-05-19

| File | Summary |
|------|---------|
| [restaurant-schedule-and-closure](changes/2026-05-19-restaurant-schedule-and-closure.md) | Restaurant schedule & closure — replaces openTime/closeTime with 2-layer schedule system ⚠️ |
| [auto-sortorder-on-create](changes/2026-05-19-auto-sortorder-on-create.md) | `sortOrder` auto-assigned on category and menu item creation |
| [user-search-filter-sort](changes/2026-05-19-user-search-filter-sort.md) | User list — search, filter by role/restaurant, sort by date |
| [pagination-removed-list-endpoints](changes/2026-05-19-pagination-removed-list-endpoints.md) | Pagination removed from restaurant, category, and menu list endpoints ⚠️ |
| [menu-item-isactive-writable](changes/2026-05-19-menu-item-isactive-writable.md) | `isActive` now writable via PATCH and PUT on menu items |
| [currency-moved-to-restaurant](changes/2026-05-19-currency-moved-to-restaurant.md) | Currency moved from MenuItem to Restaurant ⚠️ |
| [remove-admin-users-me-restrict-delete](changes/2026-05-19-remove-admin-users-me-restrict-delete.md) | Remove `PUT /api/admin/users/me`; restrict user deletion to SuperAdmin ⚠️ |
| [rbac-superadmin-admin-split](changes/2026-05-19-rbac-superadmin-admin-split.md) | RBAC overhaul — SuperAdmin vs Admin split + restaurant assignments |

---

> ⚠️ = breaking change requiring frontend code update
