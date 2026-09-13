## ADDED Requirements

### Requirement: User has an optional-at-storage, required-at-creation name

`User` SHALL have `Name` and `LastName` fields. Both SHALL be nullable at the database level, so existing rows created before this requirement existed remain valid without modification. The user-creation endpoint SHALL require both to be present and non-blank; the update endpoint SHALL leave either field unchanged when omitted from the request, and reject a blank value when one is supplied.

#### Scenario: Creating a user without a Name is rejected
- **WHEN** an `Admin` or `Sudo` calls the create-user endpoint with a missing or blank `Name`
- **THEN** the server returns `400 Bad Request` and no user is created

#### Scenario: Creating a user without a LastName is rejected
- **WHEN** an `Admin` or `Sudo` calls the create-user endpoint with a missing or blank `LastName`
- **THEN** the server returns `400 Bad Request` and no user is created

#### Scenario: Creating a user with both fields present succeeds
- **WHEN** an `Admin` or `Sudo` calls the create-user endpoint with a non-blank `Name` and `LastName`, alongside the other required fields
- **THEN** a new `User` is created with those values stored

#### Scenario: A pre-existing row without a Name or LastName remains valid
- **WHEN** a `User` row created before this requirement existed (with `NULL` `Name`/`LastName`) is read, logged in with, or used at the self-authorize gate
- **THEN** the operation succeeds exactly as it did before, reporting `null` for `Name`/`LastName` in the API response

#### Scenario: Updating a user without supplying Name or LastName leaves them unchanged
- **WHEN** an `Admin` or `Sudo` calls the update-user endpoint without including `Name` or `LastName` in the request
- **THEN** the user's stored `Name`/`LastName` (including a pre-existing `null`) are left unchanged

#### Scenario: Updating a user with a blank Name or LastName is rejected
- **WHEN** an `Admin` or `Sudo` calls the update-user endpoint with `Name` or `LastName` present but blank/whitespace-only
- **THEN** the server returns `400 Bad Request` and the stored value is left unchanged
