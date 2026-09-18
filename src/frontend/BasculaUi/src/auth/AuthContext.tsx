import { createContext, useCallback, useContext, useEffect, useMemo, useRef, useState } from 'react'
import type { ReactNode } from 'react'
import { apiClient, setActivityListener, setAuthToken } from '../api/client'
import type { LoginResponse, UserDto } from '../types/api'
import { PORTAL_ROLES, normalizeUserDto } from '../types/api'

const SESSION_STORAGE_KEY = 'basculaui.session'

interface Session {
  token: string
  user: UserDto
}

interface AuthContextValue {
  user: UserDto | null
  isReady: boolean
  insufficientPermissions: boolean
  login: (identifier: string, password: string) => Promise<void>
  logout: () => void
  clearInsufficientPermissions: () => void
}

const AuthContext = createContext<AuthContextValue | null>(null)

function readStoredSession(): Session | null {
  try {
    const raw = sessionStorage.getItem(SESSION_STORAGE_KEY)
    return raw ? (JSON.parse(raw) as Session) : null
  } catch {
    return null
  }
}

function writeStoredSession(session: Session | null) {
  try {
    if (session) {
      sessionStorage.setItem(SESSION_STORAGE_KEY, JSON.stringify(session))
    } else {
      sessionStorage.removeItem(SESSION_STORAGE_KEY)
    }
  } catch {
    // sessionStorage unavailable (private browsing, etc.) — session just won't survive a refresh.
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<UserDto | null>(null)
  const [isReady, setIsReady] = useState(false)
  const [insufficientPermissions, setInsufficientPermissions] = useState(false)
  const logoutTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null)

  const clearSession = useCallback(() => {
    setAuthToken(null)
    writeStoredSession(null)
    setUser(null)
    if (logoutTimerRef.current) {
      clearTimeout(logoutTimerRef.current)
      logoutTimerRef.current = null
    }
  }, [])

  const scheduleInactivityLogout = useCallback(
    (minutes: number) => {
      if (logoutTimerRef.current) {
        clearTimeout(logoutTimerRef.current)
      }
      logoutTimerRef.current = setTimeout(() => {
        clearSession()
      }, minutes * 60 * 1000)
    },
    [clearSession],
  )

  const resetInactivityTimer = useCallback(() => {
    if (user) {
      scheduleInactivityLogout(user.inactivityTimeoutMinutes)
    }
  }, [user, scheduleInactivityLogout])

  // Restore a session on refresh (sessionStorage, not localStorage — see design.md Decision 4).
  useEffect(() => {
    const stored = readStoredSession()
    if (stored && PORTAL_ROLES.includes(stored.user.role)) {
      setAuthToken(stored.token)
      setUser(stored.user)
    } else if (stored) {
      writeStoredSession(null)
    }
    setIsReady(true)
  }, [])

  // Any click/keypress resets the inactivity timer, mirroring the MAUI terminal's watcher.
  useEffect(() => {
    if (!user) return
    resetInactivityTimer()
    const handler = () => resetInactivityTimer()
    window.addEventListener('click', handler)
    window.addEventListener('keydown', handler)
    return () => {
      window.removeEventListener('click', handler)
      window.removeEventListener('keydown', handler)
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [user])

  // A successful API response also resets the timer (design.md Decision 4).
  useEffect(() => {
    setActivityListener(user ? resetInactivityTimer : null)
    return () => setActivityListener(null)
  }, [user, resetInactivityTimer])

  const login = useCallback(async (identifier: string, password: string) => {
    const response = await apiClient.post<LoginResponse>('/api/Auth/Login', { identifier, password })
    const user = normalizeUserDto(response.user)
    if (!PORTAL_ROLES.includes(user.role)) {
      // Role gate (admin-portal spec Requirement 1): discard the session immediately, never enter
      // the portal shell — the JWT itself isn't role-restricted server-side, so this is a
      // client-side-only gate.
      setInsufficientPermissions(true)
      return
    }
    setAuthToken(response.token)
    writeStoredSession({ token: response.token, user })
    setUser(user)
  }, [])

  const logout = useCallback(() => {
    clearSession()
  }, [clearSession])

  const clearInsufficientPermissions = useCallback(() => setInsufficientPermissions(false), [])

  const value = useMemo<AuthContextValue>(
    () => ({ user, isReady, insufficientPermissions, login, logout, clearInsufficientPermissions }),
    [user, isReady, insufficientPermissions, login, logout, clearInsufficientPermissions],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used within an AuthProvider')
  return ctx
}
