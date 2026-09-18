import { useEffect, useState } from 'react'
import { apiClient, ApiError } from '../api/client'
import type { WeightEntryDto } from '../types/api'
import './DashboardPage.css'

/** Dashboard tiles computed client-side from existing list responses (design.md Decision 5) —
 * there is no stats endpoint, so these are approximations bounded by the `top` page size
 * requested below, not accurate global counts. Revisit only if this proves insufficient. */
const PAGE_SIZE = 100

export default function DashboardPage() {
  const [pending, setPending] = useState<WeightEntryDto[] | null>(null)
  const [completed, setCompleted] = useState<WeightEntryDto[] | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    Promise.all([
      apiClient.get<WeightEntryDto[]>('/api/Weight/Pending', { top: PAGE_SIZE }),
      apiClient.get<WeightEntryDto[]>('/api/Weight/All/Completed', { top: PAGE_SIZE }),
    ])
      .then(([pendingData, completedData]) => {
        setPending(pendingData)
        setCompleted(completedData)
      })
      .catch((err) => setError(err instanceof ApiError ? err.message : 'No se pudo cargar el dashboard.'))
  }, [])

  const totalBrutePending = pending?.reduce((sum, e) => sum + e.bruteWeight, 0) ?? 0

  return (
    <div className="dashboard-page">
      <h1>Dashboard</h1>
      <p className="dashboard-note">
        Los valores mostrados son aproximados (calculados sobre los primeros {PAGE_SIZE} registros de cada
        lista), no totales globales — no existe un endpoint de estadísticas dedicado.
      </p>
      {error && <p className="dashboard-error">{error}</p>}
      <div className="dashboard-tiles">
        <div className="dashboard-tile">
          <span>Pesajes activos</span>
          <strong>{pending ? pending.length : '…'}</strong>
        </div>
        <div className="dashboard-tile">
          <span>Pesajes concluidos (recientes)</span>
          <strong>{completed ? completed.length : '…'}</strong>
        </div>
        <div className="dashboard-tile">
          <span>Bruto acumulado (activos)</span>
          <strong>{pending ? totalBrutePending.toFixed(2) : '…'}</strong>
        </div>
      </div>
    </div>
  )
}
