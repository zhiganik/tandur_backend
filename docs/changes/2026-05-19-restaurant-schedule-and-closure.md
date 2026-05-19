# Restaurant Schedule & Closure

**Date:** 2026-05-19
**Auth required:** Admin or SuperAdmin bearer token (admin endpoints); any valid bearer token (mobile endpoint)

---

## Changes

### 1. `RestaurantDto` — **`openTime` and `closeTime` removed** ⚠️ Breaking

`openTime` and `closeTime` are gone from all restaurant responses. Use the schedule endpoints below to read and manage hours.

```jsonc
// RestaurantDto — before
{
  "id": "uuid",
  "name": "string",
  "currency": "KZT",
  "timeZone": "Asia/Almaty",
  "openTime": "09:00:00",    // REMOVED
  "closeTime": "22:00:00",   // REMOVED
  "isActive": true,
  "isOpenNow": true,
  "distanceKm": null
}

// RestaurantDto — after
{
  "id": "uuid",
  "name": "string",
  "currency": "KZT",
  "timeZone": "Asia/Almaty",
  "isActive": true,
  "isOpenNow": true,          // still present — now computed from the 2-layer schedule
  "distanceKm": null
}
```

---

### 2. `POST /api/admin/restaurants` and `PUT /api/admin/restaurants/{id}` — **`openTime`/`closeTime` removed from request** ⚠️ Breaking

```jsonc
// CreateRestaurantRequest / UpdateRestaurantRequest — before
{
  "name": "string",
  "address": "string",
  "latitude": 43.25,
  "longitude": 76.95,
  "currency": "KZT",
  "timeZone": "Asia/Almaty",
  "openTime": "09:00:00",    // REMOVED
  "closeTime": "22:00:00"    // REMOVED
}

// after — openTime/closeTime gone, schedule managed via /schedule endpoints
{
  "name": "string",
  "address": "string",
  "latitude": 43.25,
  "longitude": 76.95,
  "currency": "KZT",
  "timeZone": "Asia/Almaty"
}
```

A default schedule (Mon–Sat 09:00–22:00, Sunday off) is seeded automatically when a restaurant is created.

---

### 3. `GET /api/admin/restaurants/{id}/schedule` — **new endpoint**

**Before:** did not exist.

**After:** returns the full 7-day weekly schedule.

```
GET /api/admin/restaurants/{id}/schedule
Authorization: Bearer <admin-or-superadmin-token>

200 → ScheduleDayDto[]
403 → Admin does not manage this restaurant
```

```jsonc
// ScheduleDayDto
[
  {
    "dayOfWeek": 1,       // 0=Sunday … 6=Saturday (System.DayOfWeek)
    "isDayOff": false,
    "timeSlots": [
      { "from": "09:00:00", "to": "14:00:00" },
      { "from": "15:00:00", "to": "22:00:00" }
    ]
  },
  {
    "dayOfWeek": 0,       // Sunday
    "isDayOff": true,
    "timeSlots": []
  }
]
```

---

### 4. `PUT /api/admin/restaurants/{id}/schedule` — **new endpoint**

Replaces the full weekly schedule. All 7 days must be included.

```jsonc
// UpdateFullScheduleRequest
{
  "days": [
    {
      "dayOfWeek": 1,
      "isDayOff": false,
      "timeSlots": [
        { "from": "09:00:00", "to": "22:00:00" }
      ]
    },
    { "dayOfWeek": 0, "isDayOff": true, "timeSlots": [] }
    // ... all 7 days required
  ]
}
```

Validation: all 7 DayOfWeek values required; each slot's `from` must be before `to`; slots must not overlap.

---

### 5. `PATCH /api/admin/restaurants/{id}/schedule/{dayOfWeek}` — **new endpoint**

Updates a single day. `{dayOfWeek}` is an integer 0–6 (Sunday=0).

```jsonc
// UpdateScheduleDayRequest
{
  "isDayOff": false,
  "timeSlots": [
    { "from": "10:00:00", "to": "23:00:00" }
  ]
}
```

```
200 → ScheduleDayDto
404 → day not found for this restaurant
```

---

### 6. `GET /api/admin/restaurants/{id}/overrides` — **new endpoint**

Returns all planned and instant overrides for a restaurant.

```jsonc
// ScheduleOverrideDto
[
  {
    "id": "uuid",
    "date": "2025-12-25",
    "reason": "Christmas",
    "isInstant": false,
    "timeSlots": []          // empty = closed all day
  },
  {
    "id": "uuid",
    "date": "2025-12-31",
    "reason": "New Year's Eve short hours",
    "isInstant": false,
    "timeSlots": [
      { "from": "12:00:00", "to": "18:00:00" }
    ]
  }
]
```

`isInstant: true` means the override was created on the same day it applies (emergency closure).

---

### 7. `POST /api/admin/restaurants/{id}/overrides` — **new endpoint**

Creates a date override (planned day off or custom hours). `timeSlots` empty = closed all day.

```jsonc
// CreateOverrideRequest
{
  "date": "2025-12-25",
  "reason": "Christmas day off",   // required
  "timeSlots": []                  // empty = closed all day; add slots for custom hours
}
```

```
201 → ScheduleOverrideDto
400 → date in the past, missing reason, overlapping slots
```

---

### 8. `PUT /api/admin/restaurants/{id}/overrides/{overrideId}` — **new endpoint**

```jsonc
// UpdateOverrideRequest
{
  "reason": "Updated reason",
  "timeSlots": [{ "from": "14:00:00", "to": "20:00:00" }]
}
```

---

### 9. `DELETE /api/admin/restaurants/{id}/overrides/{overrideId}` — **new endpoint**

```
204 → deleted
404 → not found
```

---

### 10. `POST /api/admin/restaurants/{id}/close` — **new endpoint**

Emergency closure — creates an instant override for **today**. Empty `timeSlots` = closed all day; provide slots to reopen from a specific time.

```jsonc
// InstantCloseRequest
{
  "reason": "No water — emergency",   // required
  "timeSlots": []                     // empty = closed all day
}

// or: close only until 18:00, then reopen
{
  "reason": "Water being fixed",
  "timeSlots": [{ "from": "18:00:00", "to": "22:00:00" }]
}
```

```
200 → ScheduleOverrideDto (isInstant: true)
```

If an instant override for today already exists it is replaced.

---

### 11. `DELETE /api/admin/restaurants/{id}/close` — **new endpoint**

Removes today's instant closure override, falling back to the weekly schedule.

```
204 → reopened
404 → no instant closure active today
```

---

### 12. `GET /api/restaurants/{id}/schedule` — **new endpoint** (mobile)

**Auth:** any valid bearer token.

```jsonc
// RestaurantSchedulePublicDto
{
  "isOpenNow": true,
  "todaySlots": [
    { "from": "10:00:00", "to": "12:00:00" },
    { "from": "14:00:00", "to": "20:00:00" }
  ],
  "nextSlot": { "from": "14:00:00", "to": "20:00:00" },   // null if closed for the day
  "upcomingClosures": [
    { "date": "2025-12-25", "reason": "Christmas" }
  ]
}
```

`todaySlots` reflects the two-layer logic (override wins over weekly schedule). `upcomingClosures` lists future dates where the restaurant will be fully closed (empty `timeSlots` overrides).

---

## Two-layer open/close logic

`isOpenNow` (in `RestaurantDto` and the public schedule endpoint) is always computed as:

1. If there is an override for **today** → use its slots (empty = closed)
2. Otherwise → use the weekly schedule for today's day of week
