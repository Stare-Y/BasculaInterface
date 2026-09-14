# Role & Permission Model — Spec

## Purpose

Defines the six fixed roles introduced by issue #134 and the `rename-customer-service-role` change, the two ABAC permission flags with role-based defaults and per-user tri-state overrides, `Sudo`'s unconditional bypass, and the deliberate absence of any account seeding.

## Requirements

### Requirement: Six fixed roles
The system SHALL define exactly six roles: `Operator`, `Dispatching Operator`, `Supervisor`, `Admin`, `Sudo`, and `Customer Service`. `Customer Service` SHALL be stored at the same underlying ordinal previously used for `Purchasing Operator` — a rename only, appended after `Sudo`, never inserted earlier, so every existing stored `Role` value is unaffected.

#### Scenario: A user is assigned exactly one role
- **WHEN** a user is created or edited
- **THEN** it is assigned exactly one of the six defined roles

#### Scenario: Existing stored roles are unaffected by the rename
- **WHEN** a `User` row whose stored `Role` value previously resolved to `Purchasing Operator` is read after this change
- **THEN** it resolves to `Customer Service`, with identical role-derived behavior (same `TerminalMode` default, same permission defaults, same inactivity timeout default)

### Requirement: Role-based default permissions
Each role SHALL have a default value for two permission flags, `CanSelfAuthorizeGate` and `CanCaptureWeightManually`: `Operator`, `Dispatching Operator`, `Admin`, and `Customer Service` default both to `false`; `Supervisor` defaults both to `true`; `Sudo` bypasses both checks unconditionally (see the separate bypass requirement).

#### Scenario: A Supervisor has gate access by default
- **WHEN** a `Supervisor` user with no explicit override is evaluated for `CanSelfAuthorizeGate`
- **THEN** the effective value is `true`

#### Scenario: An Operator lacks gate access by default
- **WHEN** an `Operator` user with no explicit override is evaluated for `CanSelfAuthorizeGate`
- **THEN** the effective value is `false`

#### Scenario: A Customer Service user lacks gate access by default
- **WHEN** a `Customer Service` user with no explicit override is evaluated for `CanSelfAuthorizeGate`
- **THEN** the effective value is `false`

### Requirement: Per-user permission overrides regardless of role
Each of the two permission flags SHALL support a per-user tri-state override (inherit role default / forced true / forced false), independent of the user's role.

#### Scenario: An Operator is individually granted gate access
- **WHEN** an `Operator` user has `CanSelfAuthorizeGateOverride = true`
- **THEN** the effective value of `CanSelfAuthorizeGate` for that user is `true`, regardless of the `Operator` role default

#### Scenario: A Supervisor has gate access individually revoked
- **WHEN** a `Supervisor` user has `CanSelfAuthorizeGateOverride = false`
- **THEN** the effective value of `CanSelfAuthorizeGate` for that user is `false`, regardless of the `Supervisor` role default

### Requirement: Sudo bypasses all authorization checks unconditionally
A user with `Role == Sudo` SHALL be treated as authorized for every permission check and every authenticated endpoint, without evaluating role defaults or per-user overrides.

#### Scenario: Sudo passes the self-authorize gate regardless of flags
- **WHEN** a `Sudo` user presents their credential at the self-authorize gate
- **THEN** the gate check succeeds, without evaluating `CanSelfAuthorizeGate`

### Requirement: No account is seeded by this change
The system SHALL NOT seed any user account (including `Sudo`) via migration or application startup. The first account is provisioned by the project owner directly against the database.

#### Scenario: A fresh deployment has zero users
- **WHEN** the `Users` table is queried immediately after this change's migration is applied
- **THEN** it contains zero rows
