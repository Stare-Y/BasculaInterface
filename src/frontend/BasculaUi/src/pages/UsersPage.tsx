import { useCallback, useEffect, useState } from 'react'
import { apiClient, ApiError } from '../api/client'
import type { CreateUserRequest, Role, UpdateUserRequest, UserDto } from '../types/api'
import { normalizeUserDto, roleToOrdinal, terminalModeToOrdinal } from '../types/api'
import './UsersPage.css'

const ROLES: Role[] = ['Operator', 'DispatchingOperator', 'Supervisor', 'Admin', 'Sudo', 'CustomerService']

const emptyCreateForm: CreateUserRequest = {
  username: '',
  userCode: '',
  password: '',
  role: 'Operator',
  name: '',
  lastName: '',
}

export default function UsersPage() {
  const [users, setUsers] = useState<UserDto[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [showCreate, setShowCreate] = useState(false)
  const [createForm, setCreateForm] = useState<CreateUserRequest>(emptyCreateForm)
  const [editingUser, setEditingUser] = useState<UserDto | null>(null)

  const loadUsers = useCallback(async () => {
    setIsLoading(true)
    setError(null)
    try {
      const data = await apiClient.get<UserDto[]>('/api/Users')
      setUsers(data.map(normalizeUserDto))
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'No se pudieron cargar los usuarios.')
    } finally {
      setIsLoading(false)
    }
  }, [])

  useEffect(() => {
    loadUsers()
  }, [loadUsers])

  async function handleCreate() {
    setError(null)
    try {
      // Backend has no JsonStringEnumConverter registered — Role must go over the wire as its
      // numeric ordinal, not the string the <select> works with (see types/api.ts).
      await apiClient.post<UserDto>('/api/Users', {
        ...createForm,
        role: roleToOrdinal(createForm.role),
        terminalModeOverride: terminalModeToOrdinal(createForm.terminalModeOverride),
      })
      setCreateForm(emptyCreateForm)
      setShowCreate(false)
      await loadUsers()
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'No se pudo crear el usuario.')
    }
  }

  async function handleUpdate(id: number, request: UpdateUserRequest) {
    setError(null)
    try {
      await apiClient.put<UserDto>(`/api/Users/${id}`, {
        ...request,
        role: request.role != null ? roleToOrdinal(request.role) : request.role,
        terminalModeOverride: terminalModeToOrdinal(request.terminalModeOverride),
      })
      setEditingUser(null)
      await loadUsers()
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'No se pudo actualizar el usuario.')
    }
  }

  async function handleDisable(user: UserDto) {
    if (!window.confirm(`¿Deshabilitar a ${user.username}? Esta acción no se puede deshacer desde el portal.`)) {
      return
    }
    setError(null)
    try {
      await apiClient.patch(`/api/Users/${user.id}/Disable`)
      await loadUsers()
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'No se pudo deshabilitar el usuario.')
    }
  }

  return (
    <div className="users-page">
      <div className="users-header">
        <h1>Usuarios</h1>
        <button onClick={() => setShowCreate((v) => !v)}>{showCreate ? 'Cancelar' : 'Nuevo usuario'}</button>
      </div>

      {error && <p className="users-error">{error}</p>}

      {showCreate && (
        <div className="users-form-card">
          <h2>Crear usuario</h2>
          <div className="users-form-grid">
            <label>
              Usuario
              <input
                value={createForm.username}
                onChange={(e) => setCreateForm({ ...createForm, username: e.target.value })}
              />
            </label>
            <label>
              Código
              <input
                value={createForm.userCode}
                onChange={(e) => setCreateForm({ ...createForm, userCode: e.target.value })}
              />
            </label>
            <label>
              Contraseña
              <input
                type="password"
                value={createForm.password}
                onChange={(e) => setCreateForm({ ...createForm, password: e.target.value })}
              />
            </label>
            <label>
              Rol
              <select
                value={createForm.role}
                onChange={(e) => setCreateForm({ ...createForm, role: e.target.value as Role })}
              >
                {ROLES.map((role) => (
                  <option key={role} value={role}>
                    {role}
                  </option>
                ))}
              </select>
            </label>
            <label>
              Nombre
              <input value={createForm.name} onChange={(e) => setCreateForm({ ...createForm, name: e.target.value })} />
            </label>
            <label>
              Apellido
              <input
                value={createForm.lastName}
                onChange={(e) => setCreateForm({ ...createForm, lastName: e.target.value })}
              />
            </label>
          </div>
          <button onClick={handleCreate}>Guardar</button>
        </div>
      )}

      {editingUser && (
        <EditUserForm user={editingUser} onCancel={() => setEditingUser(null)} onSave={handleUpdate} />
      )}

      {isLoading ? (
        <p>Cargando…</p>
      ) : (
        <table className="users-table">
          <thead>
            <tr>
              <th>Usuario</th>
              <th>Código</th>
              <th>Nombre</th>
              <th>Rol</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {users.map((user) => (
              <tr key={user.id}>
                <td>{user.username}</td>
                <td>{user.userCode}</td>
                <td>
                  {user.name} {user.lastName}
                </td>
                <td>{user.role}</td>
                <td className="users-actions">
                  <button onClick={() => setEditingUser(user)}>Editar</button>
                  <button onClick={() => handleDisable(user)}>Deshabilitar</button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  )
}

function EditUserForm({
  user,
  onCancel,
  onSave,
}: {
  user: UserDto
  onCancel: () => void
  onSave: (id: number, request: UpdateUserRequest) => void
}) {
  const [username, setUsername] = useState(user.username)
  const [userCode, setUserCode] = useState(user.userCode)
  const [role, setRole] = useState<Role>(user.role)
  const [name, setName] = useState(user.name ?? '')
  const [lastName, setLastName] = useState(user.lastName ?? '')
  const [newPassword, setNewPassword] = useState('')
  const [canSelfAuthorizeGateOverride, setCanSelfAuthorizeGateOverride] = useState(
    user.canSelfAuthorizeGateOverride,
  )
  const [canCaptureWeightManuallyOverride, setCanCaptureWeightManuallyOverride] = useState(
    user.canCaptureWeightManuallyOverride,
  )

  function submit() {
    onSave(user.id, {
      username,
      userCode,
      newPassword: newPassword || null,
      role,
      canSelfAuthorizeGateOverride,
      resetCanSelfAuthorizeGateOverride: canSelfAuthorizeGateOverride === null,
      canCaptureWeightManuallyOverride,
      resetCanCaptureWeightManuallyOverride: canCaptureWeightManuallyOverride === null,
      name,
      lastName,
      resetTerminalModeOverride: false,
    })
  }

  return (
    <div className="users-form-card">
      <h2>Editar usuario — {user.username}</h2>
      <div className="users-form-grid">
        <label>
          Usuario
          <input value={username} onChange={(e) => setUsername(e.target.value)} />
        </label>
        <label>
          Código
          <input value={userCode} onChange={(e) => setUserCode(e.target.value)} />
        </label>
        <label>
          Nueva contraseña (opcional)
          <input type="password" value={newPassword} onChange={(e) => setNewPassword(e.target.value)} />
        </label>
        <label>
          Rol
          <select value={role} onChange={(e) => setRole(e.target.value as Role)}>
            {ROLES.map((r) => (
              <option key={r} value={r}>
                {r}
              </option>
            ))}
          </select>
        </label>
        <label>
          Nombre
          <input value={name} onChange={(e) => setName(e.target.value)} />
        </label>
        <label>
          Apellido
          <input value={lastName} onChange={(e) => setLastName(e.target.value)} />
        </label>
        <label>
          Autorizar reja (override)
          <select
            value={canSelfAuthorizeGateOverride === null ? 'default' : String(canSelfAuthorizeGateOverride)}
            onChange={(e) =>
              setCanSelfAuthorizeGateOverride(e.target.value === 'default' ? null : e.target.value === 'true')
            }
          >
            <option value="default">Usar valor del rol</option>
            <option value="true">Sí</option>
            <option value="false">No</option>
          </select>
        </label>
        <label>
          Captura manual de peso (override)
          <select
            value={canCaptureWeightManuallyOverride === null ? 'default' : String(canCaptureWeightManuallyOverride)}
            onChange={(e) =>
              setCanCaptureWeightManuallyOverride(e.target.value === 'default' ? null : e.target.value === 'true')
            }
          >
            <option value="default">Usar valor del rol</option>
            <option value="true">Sí</option>
            <option value="false">No</option>
          </select>
        </label>
      </div>
      <div className="users-form-actions">
        <button onClick={submit}>Guardar cambios</button>
        <button onClick={onCancel}>Cancelar</button>
      </div>
    </div>
  )
}
