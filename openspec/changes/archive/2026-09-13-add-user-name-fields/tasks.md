## 1. Domain & database

- [x] 1.1 Add `Name`/`LastName` (`string?`, nullable) to `Core.Domain/Entities/Identity/User.cs`, doc-commented the same way `InactivityTimeoutMinutes` was: what they are, and a pointer to this change's design.md Decision 1 for why they're nullable despite being "required" at the API.
- [x] 1.2 Generate `Infrastructure/Migrations/<timestamp>_AddUserNameFields.cs` (`dotnet ef migrations add AddUserNameFields --project Infrastructure --startup-project BasculaTerminalApi --context WeightDBContext`, same throwaway-connection-string pattern used for the inactivity-timeout migration). Verify the generated migration is a plain nullable `AddColumn<string>` for each column, no default value, no data migration. — Verified: `20260913092429_AddUserNameFields.cs` is exactly that, both columns.

## 2. Backend — DTOs & validation

- [x] 2.1 `CreateUserRequest` gains `Name`/`LastName` as required (non-optional) positional parameters, placed before the existing trailing `int? InactivityTimeoutMinutes = null` (C# requires optional parameters last in a positional record).
- [x] 2.2 `UpdateUserRequest` gains `Name`/`LastName` as `string?` (optional, partial-update convention already used by `Username`/`UserCode`).
- [x] 2.3 `UserService.CreateAsync`: add `ArgumentException`-throwing guard clauses for blank/whitespace `Name`/`LastName`, matching the existing `Username`/`UserCode`/`Password` guards exactly in style and placement; set both on the created `User`.
- [x] 2.4 `UserService.UpdateAsync`: gated on `!= null` (not `!IsNullOrWhiteSpace`, unlike `Username`/`UserCode`) so an explicitly-blank value is distinguishable from an omitted one and can be rejected — see design.md Decision 2's revised wording. Omitted → unchanged; blank-but-present → `400`; non-blank → applied.
- [x] 2.5 `UserDto` gains `Name`/`LastName` (`string?`), copied as-is (no coalescing) in the `UserDto(User entity)` constructor.

## 3. Existing call sites & tests

- [x] 3.1 Updated the two existing test call sites that construct a `CreateUserRequest` positionally (`UserServiceInactivityTimeoutTests.MakeCreateRequest`, `UsersControllerInactivityTimeoutHttpTests`, both call sites in the latter) to supply `Name`/`LastName`.
- [x] 3.2 Added `UserServiceNameFieldsTests.cs`: create rejects `null`/`""`/whitespace-only `Name` (3 cases), same for `LastName` (3 cases), create with both present succeeds and stores them, update without either field leaves a pre-existing `null` (simulated directly on an in-memory entity) untouched, update with a blank `Name`/`LastName` is rejected and leaves the stored value unchanged (2 cases each), update with non-blank values applies them. 10 tests total.
- [x] 3.3 Confirmed no other production code path reads `User.Name`/`LastName` — grepped the whole backend; the only `.Name`/`.LastName` hits outside the files this change touches are unrelated (`DeviceInfo.Name`, `ClaimTypes.Name`).

## 4. Verification

- [x] 4.1 Full non-integration, non-Live test suite passes: 123/123 (110 pre-existing + 13 new: 10 from `UserServiceNameFieldsTests` + the 3 already-existing tests that needed their call sites updated). The one other failure seen when running without excluding `Live` (`BasculaTerminalTest.Live.BasculaClient.WebSocketTesting`, connection refused on `localhost:5284`) is pre-existing and unrelated — it requires an actual running API process, not something this or any prior change in this suite provisions.
- [x] 4.2 Confirmed via the generated migration file (`20260913092429_AddUserNameFields.cs`): both columns are `AddColumn<string>(..., nullable: true)`, no default, no data migration step — an existing row needs no backfill to remain valid.
- [ ] 4.3 Docker-dependent HTTP-level integration test for this validation was **not added** — the existing `UsersControllerInactivityTimeoutHttpTests` call sites were updated to keep compiling (3.1), but no new integration test asserts the 400-on-blank-Name/LastName behavior over real HTTP. Same sandbox limitation as every other integration test in this suite (`Docker.DotNet` ping failure) means it couldn't be verified here even if added. Left for a future pass on a machine with Docker/Podman, alongside the other already-outstanding Docker-unverified tests (5.2 in `fix-session-inactivity-timeout`, 8.3-8.5 in the parent change).
