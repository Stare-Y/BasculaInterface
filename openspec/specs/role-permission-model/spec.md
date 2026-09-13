# Role & Permission Model — Spec

## Purpose

Defines the five fixed roles introduced by issue #134, the two ABAC permission flags with role-based defaults and per-user tri-state overrides, `Sudo`'s unconditional bypass, and the deliberate absence of any account seeding.

## Requirements

### Requirement: Five fixed roles
The system SHALL define exactly five roles: `Operator`, `Dispatching Operator`, `Supervisor`, `Admin`, and `Sudo`.

#### Scenario: A user is assigned exactly one role
- **WHEN** a user is created or edited
- **THEN** it is assigned exactly one of the five defined roles

### Requirement: Role-based default permissions
Each role SHALL have a default value for two permission flags, `CanSelfAuthorizeGate` and `CanCaptureWeightManually`: `Operator` and `Dispatching Operator` default both to `false`; `Supervisor` defaults both to `true`; `Admin` defaults both to `false`.

#### Scenario: A Supervisor has gate access by default
- **WHEN** a `Supervisor` user with no explicit override is evaluated for `CanSelfAuthorizeGate`
- **THEN** the effective value is `true`

#### Scenario: An Operator lacks gate access by default
- **WHEN** an `Operator` user with no explicit override is evaluated for `CanSelfAuthorizeGate`
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
