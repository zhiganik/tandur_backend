Track all frontend-affecting changes made in the current session and append them to `docs/api-changelog.md`.

A "frontend-affecting change" is any of the following:
- New, removed, or renamed API route (any controller)
- Changed HTTP method on an existing route
- Changed authorization requirement (policy, role, or auth scheme)
- Added, removed, or renamed field in a request DTO
- Added, removed, or renamed field in a response DTO or record (including `MeDto`, `UserDto`, `RestaurantDto`, etc.)
- Changed response status code meaning or new status codes on an existing route
- New or removed endpoint entirely

Changes that are NOT frontend-affecting and should be skipped:
- Internal refactoring (service layer, repository, DI wiring)
- Test changes
- EF migrations (unless a field name changes)
- Infrastructure configuration

## Steps

**Step 1 — Identify changes**

Read git diff for all `src/Api/Controllers/*.cs` and `src/Core/DTOs/**/*.cs` files to find what changed since the last commit (or the beginning of the session if nothing is committed yet):

```
git diff HEAD -- src/Api/Controllers src/Core/DTOs
```

If nothing is committed yet, compare to the remote:
```
git diff origin/main -- src/Api/Controllers src/Core/DTOs
```

**Step 2 — Build the entry**

For each frontend-affecting change, write a clear entry with:
- The route and HTTP method
- **Before:** what the old behaviour/shape was (use `did not exist` for new endpoints)
- **After:** what the new behaviour/shape is
- Required token/role
- Full JSON shape for any changed request or response DTO (with comments for new/changed fields marked `// NEW` or `// CHANGED`)
- Status codes if they changed

Group related changes under a single dated heading. Use today's date: **$CURRENT_DATE** (resolve this to the actual date at invocation time).

**Step 3 — Prepend to the changelog**

Open `docs/api-changelog.md`. Insert the new entry **after the first two header lines** (the `# API Changelog` title and the description paragraph) but **before** the first existing `---` separator, so the file stays newest-first.

The entry format:

```markdown
---

## YYYY-MM-DD — <short title describing what changed>

### 1. `METHOD /route` — **description**

**Before:** ...

**After:** ...

...
```

**Step 4 — Confirm**

Tell the user which entries were added and show them the first 30 lines of the updated file so they can verify placement.