## Why

`User` (issue #134's custom entity — no ASP.NET Core Identity) has no display name at all today: `Username` and `UserCode` are login/identification credentials, not a person's actual name. The project owner wants a `Name`/`LastName` on every user going forward, enforced by the user-management API — but the owner already has real production rows created before this change existed, and those rows must keep working exactly as they do today; nothing about this change may retroactively invalidate or break them.

## What Changes

- **`User` gains `Name` and `LastName`** (`string?`, nullable at the database level) — nullable specifically so the migration is a no-op for every existing row: they get `NULL` in both new columns and continue to load, log in, and pass the self-authorize gate exactly as before.
- **The user-management API enforces both as required going forward**: `UserService.CreateAsync` rejects a blank/missing `Name` or `LastName` with the same `ArgumentException`-driven `400 Bad Request` pattern already used for `Username`/`UserCode`/`Password`. Every *new* user created through the API from now on is guaranteed to have both.
- **`UpdateAsync` follows the existing partial-update convention**: omitting `Name`/`LastName` from an update request leaves the stored value untouched (so a pre-existing `NULL` row can be left alone indefinitely, or backfilled whenever convenient); supplying either rejects a blank value with the same `400`.
- **`UserDto`/`GetAll`/`GetById` expose both as nullable strings** — a legacy row simply reports `null` until backfilled; no API consumer is misled into thinking a value exists when it doesn't.

## Non-goals

- Backfilling `Name`/`LastName` on any existing row — that's a data/business decision for the project owner to make later (bulk update, ask each user, etc.), not something this change automates or forces.
- Any change to `BasculaUi` (React dashboard) — display/edit-form updates there are a separate, follow-up concern.
- Making `Name`/`LastName` part of login, the self-authorize gate, or any auth/permission decision — purely descriptive fields.
- Any uniqueness constraint on `Name`/`LastName` (unlike `Username`/`UserCode`) — people can share a name.

## Impact

**API/Domain:** `User` entity gains two nullable columns; `UserDto`/`CreateUserRequest`/`UpdateUserRequest`; `UserService.CreateAsync`/`UpdateAsync` validation; a new EF Core migration (nullable `AddColumn`, no default needed, no data migration).
**Affected terminals:** none directly — `BasculaInterface` (MAUI) never displays or edits `Name`/`LastName` today and this change doesn't add that; `BasculaUi` (React admin dashboard) is where user management actually happens and is out of scope per Non-goals above.
**Existing data:** zero impact — every existing row keeps loading and authenticating exactly as before, with `Name`/`LastName` simply reading as `null` until an explicit update supplies them.
