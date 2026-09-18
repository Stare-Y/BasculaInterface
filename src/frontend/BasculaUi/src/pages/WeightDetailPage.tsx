import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { apiClient, ApiError } from '../api/client'
import type { AuditLogEntryDto, WeightEntryDto, WeightEntryRadiographyDto } from '../types/api'
import './WeightDetailPage.css'

export default function WeightDetailPage() {
  const { id } = useParams<{ id: string }>()
  const [entry, setEntry] = useState<WeightEntryDto | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  // Audit trail state is intentionally separate from the entry fetch above and starts empty —
  // no request to /Radiography is made until loadAuditTrail() runs, triggered only by the
  // explicit button below (admin-portal spec Requirement 4: "never automatically").
  const [auditLog, setAuditLog] = useState<AuditLogEntryDto[] | null>(null)
  const [isAuditLoading, setIsAuditLoading] = useState(false)
  const [auditError, setAuditError] = useState<string | null>(null)

  useEffect(() => {
    if (!id) return
    setIsLoading(true)
    setError(null)
    apiClient
      .get<WeightEntryDto>('/api/Weight/ById', { id })
      .then(setEntry)
      .catch((err) => setError(err instanceof ApiError ? err.message : 'No se pudo cargar el pesaje.'))
      .finally(() => setIsLoading(false))
  }, [id])

  async function loadAuditTrail() {
    if (!id) return
    setIsAuditLoading(true)
    setAuditError(null)
    try {
      const radiography = await apiClient.get<WeightEntryRadiographyDto>(`/api/Weight/${id}/Radiography`)
      setAuditLog(radiography.auditLog)
    } catch (err) {
      setAuditError(err instanceof ApiError ? err.message : 'No se pudo cargar la auditoría.')
    } finally {
      setIsAuditLoading(false)
    }
  }

  if (isLoading) return <p>Cargando…</p>
  if (error) return <p className="detail-error">{error}</p>
  if (!entry) return null

  return (
    <div className="weight-detail-page">
      <Link to="/weights">← Volver a pesajes</Link>
      <h1>Pesaje #{entry.id}</h1>

      <div className="detail-summary">
        <div>
          <span>Placa</span>
          <strong>{entry.vehiclePlate}</strong>
        </div>
        <div>
          <span>Tara</span>
          <strong>{entry.tareWeight}</strong>
        </div>
        <div>
          <span>Bruto</span>
          <strong>{entry.bruteWeight}</strong>
        </div>
        <div>
          <span>Descarga</span>
          <strong>{entry.isDischarge ? 'Sí' : 'No'}</strong>
        </div>
        <div>
          <span>Concluido</span>
          <strong>{entry.concludeDate ? new Date(entry.concludeDate).toLocaleString() : '—'}</strong>
        </div>
      </div>

      <h2>Detalles</h2>
      <table className="detail-table">
        <thead>
          <tr>
            <th>ID</th>
            <th>Producto</th>
            <th>Peso</th>
            <th>Tara</th>
            <th>Cargado</th>
          </tr>
        </thead>
        <tbody>
          {entry.weightDetails.map((d) => (
            <tr key={d.id}>
              <td>{d.id}</td>
              <td>{d.fK_WeightedProductId ?? '—'}</td>
              <td>{d.weight}</td>
              <td>{d.tare}</td>
              <td>{d.isLoaded ? 'Sí' : 'No'}</td>
            </tr>
          ))}
        </tbody>
      </table>

      <h2>Auditoría</h2>
      {auditLog === null ? (
        <button onClick={loadAuditTrail} disabled={isAuditLoading}>
          {isAuditLoading ? 'Cargando…' : 'Cargar auditoría'}
        </button>
      ) : (
        <table className="detail-table">
          <thead>
            <tr>
              <th>Fecha</th>
              <th>Usuario</th>
              <th>Acción</th>
              <th>Entidad</th>
            </tr>
          </thead>
          <tbody>
            {auditLog.map((a) => (
              <tr key={a.id}>
                <td>{new Date(a.timestamp).toLocaleString()}</td>
                <td>{a.userId}</td>
                <td>{a.action}</td>
                <td>
                  {a.entityType} #{a.entityId}
                </td>
              </tr>
            ))}
            {auditLog.length === 0 && (
              <tr>
                <td colSpan={4}>Sin eventos registrados.</td>
              </tr>
            )}
          </tbody>
        </table>
      )}
      {auditError && <p className="detail-error">{auditError}</p>}
    </div>
  )
}
