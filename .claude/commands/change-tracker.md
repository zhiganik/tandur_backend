When the user appends **TRACK CHANGE** to any request, complete the task first, then create a single standalone `.md` file in `docs/changes/` that documents only that specific change. Never append to a shared file — one task, one file.

## When this runs

Only when the user's message ends with (or contains) **TRACK CHANGE**. Do not run otherwise.

## Steps

**Step 1 — Finish the task first**

Complete whatever the user asked for. The change log is written after the work is done.

**Step 2 — Identify frontend-affecting changes**

Run:
```
git diff HEAD -- src/Api/Controllers src/Core/DTOs
```

A "frontend-affecting change" includes:
- New, removed, or renamed API route
- Changed HTTP method on an existing route
- Changed authorization requirement (role or policy)
- Added, removed, or renamed field in a request or response DTO
- Changed response status codes on an existing route

Skip: internal refactoring, test changes, EF migrations (unless a field name changes), DI/infrastructure.

**Step 3 — Choose a filename**

Use the pattern: `docs/changes/YYYY-MM-DD-<slug>.md`

The slug should be 3–6 words that describe the specific change, kebab-cased. Examples:
- `docs/changes/2026-05-19-add-category-sort.md`
- `docs/changes/2026-05-19-currency-moved-to-restaurant.md`
- `docs/changes/2026-05-19-user-search-filter.md`

Use today's date for YYYY-MM-DD.

**Step 4 — Write the file**

Follow the same format as `docs/api-changelog.md`. The file should be self-contained — a reader with no context should understand what changed.

```markdown
# <Short title>

**Date:** YYYY-MM-DD
**Auth required:** <token type and minimum role>

---

## Changes

### 1. `METHOD /route` — **what changed**

**Before:** ...

**After:** ...

```jsonc
// Show full DTO shape with // NEW, // CHANGED, // REMOVED comments
```

> ⚠️ Breaking — note if this requires a frontend code change

---

### 2. ...
```

**Step 5 — Update `docs/api-changelog.md`**

Open `docs/api-changelog.md`. Find the table under the matching date heading (e.g. `## 2026-05-19`). If the heading for today's date does not exist yet, insert it above the previous date's heading in the format:

```markdown
## YYYY-MM-DD

| File | Summary |
|------|---------|

---
```

Add a new row to the table for the file just created:

```markdown
| [<slug>](changes/<filename>.md) | <one-line summary, ⚠️ if breaking> |
```

- `<slug>` is the filename without the date prefix and `.md` extension (e.g. `add-category-sort`)
- Summary should be concise (under 80 chars). Append ` ⚠️` if the change is breaking.

**Step 6 — Confirm**

Tell the user the filename that was created and that `api-changelog.md` was updated. Nothing else needed.