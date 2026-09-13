# Design: Add Name/LastName to User (retro-compatible)

## Context

`User` (`Core.Domain/Entities/Identity/User.cs`) currently has no person-facing name field — `Username`/`UserCode` are login credentials, `Role` is authorization, and `InactivityTimeoutMinutes` (just added by `fix-session-inactivity-timeout`) is a client behavior setting. The project owner wants `Name` and `LastName` added, required going forward through the CRUD API, but production already has real `User` rows created before this change exists. Those rows must not break, and inserting them was never routed through any validation this change adds — so the required-ness has to live at the API/service layer, not the database column itself.

This directly mirrors the retro-compatibility shape already established one change ago for `InactivityTimeoutMinutes`: a column that must tolerate rows written before the column had meaning, paired with logic that enforces a stronger rule for everything written through the app from now on. The mechanism differs because the two fields have different requirements once "safe for old rows" is satisfied: `InactivityTimeoutMinutes` needed a concrete numeric fallback everywhere (a null timeout can't be plugged into `TimeSpan.FromMinutes`), so it became a non-nullable column with a SQL-level default. `Name`/`LastName` have no such caller — nothing computes with them, multiplies by them, or crashes on `null` — so the simpler, more standard EF Core pattern applies directly: nullable columns, with "required" enforced purely as application-level validation on the write path.

## Goals / Non-Goals

**Goals:**
- Add `Name`/`LastName` to `User` without touching or invalidating a single existing row.
- Make the create-user API reject a missing/blank `Name` or `LastName`, matching the existing `Username`/`UserCode`/`Password` validation style exactly (same exception types, same `UserService`-level placement, same controller-level HTTP mapping already in place).
- Make the update-user API follow the codebase's existing partial-update convention: omit it, nothing changes; supply it, it's validated and applied.

**Non-Goals:** backfilling any existing row; a uniqueness constraint on either field; any `BasculaInterface` (MAUI) or `BasculaUi` (React) UI work; using either field in login, the self-authorize gate, or any permission decision.

## Decisions

### Decision 1: Nullable columns, no database-level default

**Chosen:** `public string? Name { get; set; }` and `public string? LastName { get; set; }` — plain nullable reference-type properties, no `HasDefaultValue`, no `required` keyword. The EF Core migration is a pure `AddColumn<string>(..., nullable: true)` for each, with no data migration step. Existing rows automatically read as `NULL` for both — that's a correct, valid state (not an error state) for a value that simply hasn't been backfilled yet.

**Why not mirror `InactivityTimeoutMinutes`'s non-nullable-plus-DB-default pattern:** that pattern exists specifically because a `null` int has no sane value to flow into `TimeSpan.FromMinutes(...)` on the client — the column *had* to always resolve to *some* concrete number, even for a row nobody touched. `Name`/`LastName` are consumed nowhere except display; there is no downstream calculation that breaks on `null`. Forcing a database-level non-null default (e.g. an empty string) here would just replace one falsy sentinel (`null`) with another (`""`) for no benefit, while implying to any code that reads the column that a "default" name exists — it doesn't, the person's name is simply unknown until entered.

**Rejected — non-nullable with an empty-string DB default:** would compile and "work," but every legacy row would read as `""` for Name/LastName, which is a worse signal than `null` for "this hasn't been filled in" and offers no numeric-style advantage the way `InactivityTimeoutMinutes`'s `5` does.

### Decision 2: "Required through the API" is enforced in `UserService`, not the column

**Chosen:** `UserService.CreateAsync` gets two more guard clauses, inserted alongside the existing three, in the same style:
```csharp
if (string.IsNullOrWhiteSpace(request.Name))
    throw new ArgumentException("El nombre es requerido.");
if (string.IsNullOrWhiteSpace(request.LastName))
    throw new ArgumentException("El apellido es requerido.");
```
`CreateUserRequest` gains `Name`/`LastName` as required (non-optional) positional parameters — placed before the existing trailing `int? InactivityTimeoutMinutes = null` (C# requires optional parameters last). This means every call site constructing a `CreateUserRequest` must supply both at the language level; `UserService` additionally rejects a blank/whitespace-only value at runtime, exactly like `Username` already does, since C#'s non-nullable `string` doesn't stop `""` or a JSON `null` coming over the wire from `ModelState`-bypassing code.

`UpdateAsync` is gated on `!= null` rather than `!string.IsNullOrWhiteSpace(...)` like `Username`/`UserCode` — deliberately different, because this field's spec requires telling "omitted" apart from "explicitly cleared to blank," which `Username`/`UserCode`'s pattern can't do (both look identical to a plain non-null-vs-null-agnostic check): `if (request.Name != null) { if (string.IsNullOrWhiteSpace(request.Name)) throw ...; user.Name = request.Name; }`. A JSON body that omits the field entirely deserializes it as `null` (skip, leaves any existing value — including a pre-existing legacy `NULL` — untouched); a body that explicitly sends `""`/whitespace is caught and rejected with the same `ArgumentException`→`400` mapping as create. This is stricter than `Username`/`UserCode`'s own update behavior (which silently no-ops on blank instead of rejecting it) — intentional, since the owner's request was specifically that these fields be *required*, not merely defaulted-when-present.

**Rejected — enforce via `[Required]` data annotations on the DTO:** the codebase doesn't use data-annotation validation anywhere else in this controller/service pair (every existing rule is a manual guard clause in `UserService`, checked in `UsersController`'s catch blocks) — introducing a second, inconsistent validation mechanism for just these two fields would be a bigger, less-obviously-correct change than matching the file's own established pattern.

**Rejected — make the column itself non-nullable now and backfill legacy rows with a placeholder:** rejected per the proposal's explicit Non-goal — backfilling is the project owner's call, not something this change should force via a synthetic placeholder value that would then need to be told apart from a real name later.

### Decision 3: `UserDto` exposes both as nullable, unconditionally

**Chosen:** `UserDto.Name`/`LastName` are plain `string?`, copied straight from the entity in the `UserDto(User entity)` constructor — no coalescing to `""` or any other placeholder. A `GetAll`/`GetById` caller sees `null` for a legacy row exactly because that's the true state; hiding it behind an empty string would make "never entered" indistinguishable from "entered as blank" (which validation already prevents from happening through the API in the first place, but a raw SQL update could still produce).

## Open Questions

None — the required/nullable split mirrors a pattern the project owner already approved for `InactivityTimeoutMinutes`, and this change's proposal.md's Non-goals were derived directly from the owner's own framing of the request (required-through-the-API, retro-compatible-at-the-database).
