import { Navigate, Outlet } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'

/** Route guard (tasks.md 3.4): unauthenticated access to any portal route redirects to login. */
export default function ProtectedRoute() {
  const { user, isReady } = useAuth()

  if (!isReady) {
    return null
  }

  if (!user) {
    return <Navigate to="/login" replace />
  }

  return <Outlet />
}
