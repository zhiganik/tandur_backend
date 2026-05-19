# Changelog

All notable changes to this project will be documented in this file.

## [Unreleased]

### Added
- `Restaurant.Currency` — required ISO 4217 currency code field (e.g. `KZT`, `USD`, `EUR`) on the restaurant entity. All menu items for a restaurant share a single currency.
- FluentValidation rule on `CreateRestaurantRequest` and `UpdateRestaurantRequest` that validates the currency code against ISO 4217 using `System.Globalization.RegionInfo`.

### Changed
- **Currency moved from `MenuItem` to `Restaurant`** — `MenuItem.Currency` column removed; currency is now inherited from the parent restaurant. `CreateMenuItemRequest` and `UpdateMenuItemRequest` no longer accept a `currency` field.
- **Pagination removed from Restaurants, Categories, and Menu endpoints** — list endpoints now return full arrays (`IReadOnlyList<T>`) instead of `PagedResult<T>`. Pagination is retained only on the Users admin endpoint (`GET /api/admin/users`).
  - `GET /api/restaurants` — returns `RestaurantDto[]`
  - `GET /api/admin/restaurants` — returns `RestaurantDto[]`
  - `GET /api/restaurants/{id}/categories` — returns `CategoryDto[]`
  - `GET /api/admin/restaurants/{id}/categories` — returns `CategoryDto[]`
  - `GET /api/restaurants/{id}/menu` — returns `MenuDto` with `items` as a flat array
  - `GET /api/admin/menu/items` (menu list) — returns `MenuDto` with `items` as a flat array
- `MenuDto.items` type changed from `PagedResult<MenuItemDto>` to `MenuItemDto[]`.

### Removed
- Paged repository methods: `GetPagedActiveAsync`, `GetPagedAllAsync`, `CountActiveAsync`, `CountAllAsync` on restaurants; `GetPagedAvailableAsync`, `GetPagedAllAsync`, `CountAvailableAsync`, `CountAllAsync` on menu items; `GetPagedVisibleAsync`, `GetPagedAllAsync`, `CountVisibleAsync`, `CountAllAsync` on categories.

### Database
- Migration `MoveCurrencyToRestaurant`: adds `Currency` column (`varchar(3) NOT NULL`) to `Restaurants` table; drops `Currency` column from `MenuItems` table.

## [1.0.0] — 2026-05-19

- Initial release: restaurant management, menu items with categories, OTP auth (mobile), admin panel with RBAC (Admin / SuperAdmin), image uploads (S3), soft delete.
