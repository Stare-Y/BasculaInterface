// Mirrors src/backend/Core.Application/DTOs and Core.Domain enums. Field names/casing match the
// backend's System.Text.Json defaults (PascalCase, as emitted by the API).

export type Role =
  | 'Operator'
  | 'DispatchingOperator'
  | 'Supervisor'
  | 'Admin'
  | 'Sudo'
  | 'CustomerService'

export type TerminalMode = 'Main' | 'Secondary' | 'PedidosOnly' | 'OnlyFinished'

// Backend enums (Role, TerminalMode) are plain C# enums with no JsonStringEnumConverter
// registered anywhere in BasculaTerminalApi, so System.Text.Json's default kicks in: the wire
// value is the underlying ordinal (a number), not the name string — e.g. a Sudo user's role
// arrives as `4`, not `"Sudo"`. Order must stay in exact sync with Core.Domain's enum
// declarations (Role.cs, TerminalMode.cs) since that ordinal is the only thing on the wire.
const ROLE_BY_ORDINAL: Role[] = ['Operator', 'DispatchingOperator', 'Supervisor', 'Admin', 'Sudo', 'CustomerService']
const TERMINAL_MODE_BY_ORDINAL: TerminalMode[] = ['Main', 'Secondary', 'PedidosOnly', 'OnlyFinished']

function normalizeRole(value: Role | number): Role {
  return typeof value === 'number' ? ROLE_BY_ORDINAL[value] : value
}

function normalizeTerminalMode<T extends TerminalMode | number | null | undefined>(
  value: T,
): T extends number ? TerminalMode : T {
  return (typeof value === 'number' ? TERMINAL_MODE_BY_ORDINAL[value] : value) as T extends number
    ? TerminalMode
    : T
}

/** Every UserDto received from the API must be passed through this before its `role`/
 * `terminalMode*` fields are used — see the ordinal-vs-string note above. */
export function normalizeUserDto(dto: UserDto): UserDto {
  return {
    ...dto,
    role: normalizeRole(dto.role),
    terminalMode: normalizeTerminalMode(dto.terminalMode),
    terminalModeOverride: normalizeTerminalMode(dto.terminalModeOverride),
  }
}

/** The reverse of the above: every outgoing request body with a `role`/`terminalMode*` field
 * (CreateUserRequest, UpdateUserRequest) must run its enum fields through these before being
 * sent — the backend's default deserializer only accepts the numeric ordinal, same reason. */
export function roleToOrdinal(role: Role): number {
  return ROLE_BY_ORDINAL.indexOf(role)
}

export function terminalModeToOrdinal<T extends TerminalMode | null | undefined>(
  mode: T,
): T extends TerminalMode ? number : T {
  return (mode == null ? mode : TERMINAL_MODE_BY_ORDINAL.indexOf(mode)) as T extends TerminalMode ? number : T
}

/** Roles allowed to reach the portal past login (admin-portal spec, Requirement 1). */
export const PORTAL_ROLES: Role[] = ['Admin', 'Supervisor', 'Sudo']

/** Roles allowed to see/use the user-management screen (admin-portal spec, Requirement 2). */
export const USER_MANAGEMENT_ROLES: Role[] = ['Admin', 'Sudo']

export interface GenericResponse<T> {
  data: T | null
  message: string | null
}

export interface UserDto {
  id: number
  username: string
  userCode: string
  role: Role
  canSelfAuthorizeGateOverride: boolean | null
  canCaptureWeightManuallyOverride: boolean | null
  canSelfAuthorizeGate: boolean
  canCaptureWeightManually: boolean
  inactivityTimeoutMinutes: number
  name: string | null
  lastName: string | null
  terminalModeOverride: TerminalMode | null
  terminalMode: TerminalMode
}

export interface LoginRequest {
  identifier: string
  password: string
}

export interface LoginResponse {
  token: string
  user: UserDto
}

export interface CreateUserRequest {
  username: string
  userCode: string
  password: string
  role: Role
  name: string
  lastName: string
  inactivityTimeoutMinutes?: number | null
  terminalModeOverride?: TerminalMode | null
}

export interface UpdateUserRequest {
  username?: string | null
  userCode?: string | null
  newPassword?: string | null
  role?: Role | null
  canSelfAuthorizeGateOverride?: boolean | null
  resetCanSelfAuthorizeGateOverride: boolean
  canCaptureWeightManuallyOverride?: boolean | null
  resetCanCaptureWeightManuallyOverride: boolean
  inactivityTimeoutMinutes?: number | null
  name?: string | null
  lastName?: string | null
  terminalModeOverride?: TerminalMode | null
  resetTerminalModeOverride: boolean
}

export interface ExternalTargetBehaviorDto {
  id: number
  [key: string]: unknown
}

export interface WeightDetailDto {
  id: number
  fK_WeightEntryId: number
  tare: number
  weight: number
  costales: number | null
  fK_WeightedProductId: number | null
  weightedBy: string | null
  secondaryTare: number | null
  requiredAmount: number | null
  productPrice: number | null
  lastUpdated: string | null
  notes: string | null
  isLoaded: boolean
  fK_PedidoLineId: number | null
}

export interface WeightEntryDto {
  id: number
  partnerId: number | null
  conptaqiComercialFK: number | null
  contpaqiComercialFolio: string | null
  externalTargetBehaviorFK: number | null
  tareWeight: number
  bruteWeight: number
  isDischarge: boolean
  concludeDate: string | null
  createdAt: string | null
  vehiclePlate: string
  notes: string | null
  registeredBy: string | null
  externalTargetBehavior: ExternalTargetBehaviorDto | null
  weightDetails: WeightDetailDto[]
}

export interface AuditLogEntryDto {
  id: number
  userId: number
  timestamp: string
  action: string
  entityType: string
  entityId: number
}

export interface WeightEntryRadiographyDto {
  weightEntry: WeightEntryDto
  details: WeightDetailDto[]
  auditLog: AuditLogEntryDto[]
}

export interface ClienteProveedorDto {
  id: number
  code: string
  razonSocial: string
  rfc: string | null
  creditLimit: number
  debt: number
  availableCredit: number
  orderRequestAllowed: boolean
  ignoreCreditLimit: boolean
  isProvider: boolean
}
