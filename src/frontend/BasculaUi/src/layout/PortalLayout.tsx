import { NavLink, Outlet } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'
import { USER_MANAGEMENT_ROLES } from '../types/api'
import './PortalLayout.css'

export default function PortalLayout() {
  const { user, logout } = useAuth()
  const canManageUsers = user ? USER_MANAGEMENT_ROLES.includes(user.role) : false

  return (
    <div className="portal-shell">
      <aside className="portal-sidebar">
        <div className="portal-brand">Báscula Admin</div>
        <nav className="portal-nav">
          <NavLink to="/dashboard" className={navClass}>
            Dashboard
          </NavLink>
          {canManageUsers && (
            <NavLink to="/users" className={navClass}>
              Usuarios
            </NavLink>
          )}
          <NavLink to="/weights" className={navClass}>
            Pesajes
          </NavLink>
        </nav>
        <div className="portal-user">
          <div>
            <strong>{user?.name ?? user?.username}</strong>
            <div className="portal-role">{user?.role}</div>
          </div>
          <button onClick={logout}>Cerrar sesión</button>
        </div>
      </aside>
      <main className="portal-content">
        <Outlet />
      </main>
    </div>
  )
}

function navClass({ isActive }: { isActive: boolean }) {
  return isActive ? 'portal-nav-link active' : 'portal-nav-link'
}
