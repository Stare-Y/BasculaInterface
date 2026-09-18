import { useCallback, useEffect, useRef, useState } from 'react'
import { Link } from 'react-router-dom'
import { apiClient, ApiError } from '../api/client'
import type { ClienteProveedorDto, WeightEntryDto } from '../types/api'
import './WeightsPage.css'

type ViewMode = 'pending' | 'all' | 'completed' | 'partner'

export default function WeightsPage() {
  const [view, setView] = useState<ViewMode>('pending')
  const [entries, setEntries] = useState<WeightEntryDto[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const [partnerQuery, setPartnerQuery] = useState('')
  const [partnerResults, setPartnerResults] = useState<ClienteProveedorDto[]>([])
  const [selectedPartner, setSelectedPartner] = useState<ClienteProveedorDto | null>(null)
  const debounceRef = useRef<ReturnType<typeof setTimeout> | null>(null)

  const loadEntries = useCallback(async (mode: ViewMode, partnerId?: number) => {
    setIsLoading(true)
    setError(null)
    try {
      let data: WeightEntryDto[]
      if (mode === 'pending') {
        data = await apiClient.get<WeightEntryDto[]>('/api/Weight/Pending')
      } else if (mode === 'completed') {
        data = await apiClient.get<WeightEntryDto[]>('/api/Weight/All/Completed')
      } else if (mode === 'partner' && partnerId) {
        data = await apiClient.get<WeightEntryDto[]>('/api/Weight/All/ByPartner', { partnerId })
      } else if (mode === 'partner') {
        data = []
      } else {
        data = await apiClient.get<WeightEntryDto[]>('/api/Weight/All')
      }
      setEntries(data)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'No se pudieron cargar los pesajes.')
    } finally {
      setIsLoading(false)
    }
  }, [])

  useEffect(() => {
    if (view !== 'partner') {
      loadEntries(view)
    }
  }, [view, loadEntries])

  function handlePartnerQueryChange(value: string) {
    setPartnerQuery(value)
    setSelectedPartner(null)
    if (debounceRef.current) clearTimeout(debounceRef.current)
    if (!value.trim()) {
      setPartnerResults([])
      return
    }
    debounceRef.current = setTimeout(async () => {
      try {
        const results = await apiClient.get<ClienteProveedorDto[]>('/api/ClienteProveedor/ByName', {
          name: value,
        })
        setPartnerResults(results)
      } catch {
        setPartnerResults([])
      }
    }, 300)
  }

  function selectPartner(partner: ClienteProveedorDto) {
    setSelectedPartner(partner)
    setPartnerResults([])
    setPartnerQuery(partner.razonSocial)
    setView('partner')
    loadEntries('partner', partner.id)
  }

  return (
    <div className="weights-page">
      <h1>Pesajes</h1>

      <div className="weights-tabs">
        <button className={view === 'pending' ? 'active' : ''} onClick={() => setView('pending')}>
          Activos
        </button>
        <button className={view === 'all' ? 'active' : ''} onClick={() => setView('all')}>
          Todos
        </button>
        <button className={view === 'completed' ? 'active' : ''} onClick={() => setView('completed')}>
          Concluidos
        </button>
      </div>

      <div className="weights-search">
        <input
          placeholder="Buscar por socio…"
          value={partnerQuery}
          onChange={(e) => handlePartnerQueryChange(e.target.value)}
        />
        {partnerResults.length > 0 && (
          <ul className="weights-search-results">
            {partnerResults.map((p) => (
              <li key={p.id} onClick={() => selectPartner(p)}>
                {p.razonSocial} {p.code ? `(${p.code})` : ''}
              </li>
            ))}
          </ul>
        )}
        {view === 'partner' && selectedPartner && (
          <p className="weights-search-active">
            Filtrando por: {selectedPartner.razonSocial}{' '}
            <button
              onClick={() => {
                setSelectedPartner(null)
                setPartnerQuery('')
                setView('pending')
              }}
            >
              Quitar filtro
            </button>
          </p>
        )}
      </div>

      {error && <p className="weights-error">{error}</p>}

      {isLoading ? (
        <p>Cargando…</p>
      ) : (
        <table className="weights-table">
          <thead>
            <tr>
              <th>ID</th>
              <th>Placa</th>
              <th>Tara</th>
              <th>Bruto</th>
              <th>Concluido</th>
            </tr>
          </thead>
          <tbody>
            {entries.map((entry) => (
              <tr key={entry.id}>
                <td>
                  <Link to={`/weights/${entry.id}`}>{entry.id}</Link>
                </td>
                <td>{entry.vehiclePlate}</td>
                <td>{entry.tareWeight}</td>
                <td>{entry.bruteWeight}</td>
                <td>{entry.concludeDate ? new Date(entry.concludeDate).toLocaleString() : '—'}</td>
              </tr>
            ))}
            {entries.length === 0 && (
              <tr>
                <td colSpan={5}>Sin resultados.</td>
              </tr>
            )}
          </tbody>
        </table>
      )}
    </div>
  )
}
