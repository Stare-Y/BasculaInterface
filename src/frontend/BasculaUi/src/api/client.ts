import type { GenericResponse } from '../types/api'

// Same origin as the API by default — the portal is served from BasculaTerminalApi's own
// wwwroot (design.md Decision 1), so a relative base URL needs no configuration in production.
// VITE_API_BASE_URL lets `npm run dev` point at a separately-running API instance.
const BASE_URL = import.meta.env.VITE_API_BASE_URL ?? ''

export class ApiError extends Error {
  status: number
  constructor(status: number, message: string) {
    super(message)
    this.status = status
  }
}

let authToken: string | null = null

/** Called by AuthContext on login/logout/restore so every request picks up the current token. */
export function setAuthToken(token: string | null) {
  authToken = token
}

let activityListener: (() => void) | null = null

/** AuthContext registers this so a successful API response resets the inactivity timer, same as
 * the MAUI client's "any interaction or successful API response resets the timer" semantics
 * (design.md Decision 4). */
export function setActivityListener(listener: (() => void) | null) {
  activityListener = listener
}

interface RequestOptions {
  method?: 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE'
  body?: unknown
  query?: Record<string, string | number | boolean | undefined | null>
}

function buildUrl(path: string, query?: RequestOptions['query']): string {
  const url = new URL(BASE_URL + path, window.location.origin)
  if (query) {
    for (const [key, value] of Object.entries(query)) {
      if (value !== undefined && value !== null) {
        url.searchParams.set(key, String(value))
      }
    }
  }
  return url.toString()
}

async function request<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const headers: Record<string, string> = {}
  if (authToken) {
    headers['Authorization'] = `Bearer ${authToken}`
  }
  if (options.body !== undefined) {
    headers['Content-Type'] = 'application/json'
  }

  const response = await fetch(buildUrl(path, options.query), {
    method: options.method ?? 'GET',
    headers,
    body: options.body !== undefined ? JSON.stringify(options.body) : undefined,
  })

  const text = await response.text()
  const parsed: unknown = text ? JSON.parse(text) : null

  if (!response.ok) {
    const message =
      parsed && typeof parsed === 'object' && 'message' in parsed
        ? String((parsed as GenericResponse<unknown>).message)
        : response.statusText
    throw new ApiError(response.status, message || `Request failed with status ${response.status}`)
  }

  activityListener?.()
  return parsed as T
}

export const apiClient = {
  get: <T>(path: string, query?: RequestOptions['query']) => request<T>(path, { method: 'GET', query }),
  post: <T>(path: string, body?: unknown, query?: RequestOptions['query']) =>
    request<T>(path, { method: 'POST', body, query }),
  put: <T>(path: string, body?: unknown, query?: RequestOptions['query']) =>
    request<T>(path, { method: 'PUT', body, query }),
  patch: <T>(path: string, body?: unknown, query?: RequestOptions['query']) =>
    request<T>(path, { method: 'PATCH', body, query }),
}
