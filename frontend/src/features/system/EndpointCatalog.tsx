import { Badge } from '../../components/ui/Badge'
import { CopyButton } from '../../components/ui/CopyButton'
import { API_ENDPOINTS, type ApiEndpoint } from './endpoints'

interface EndpointCatalogProps {
  endpoints?: readonly ApiEndpoint[]
}

export function EndpointCatalog({
  endpoints = API_ENDPOINTS,
}: EndpointCatalogProps) {
  return (
    <ul className="divide-y divide-slate-100">
      {endpoints.map((endpoint) => (
        <li
          key={endpoint.path}
          className="flex flex-col gap-3 px-5 py-4 sm:flex-row sm:items-center sm:justify-between"
        >
          <div className="flex min-w-0 items-start gap-3">
            <span className="mt-0.5 inline-flex h-5 shrink-0 items-center rounded-md bg-emerald-50 px-1.5 font-mono text-[10px] font-semibold text-emerald-700 ring-1 ring-inset ring-emerald-200">
              {endpoint.method}
            </span>
            <div className="min-w-0">
              <p className="truncate font-mono text-xs font-medium text-slate-900">
                {endpoint.path}
              </p>
              <p className="mt-0.5 text-xs text-slate-500">
                {endpoint.description}
              </p>
            </div>
          </div>

          <div className="flex shrink-0 items-center gap-2">
            <Badge tone={endpoint.group === 'Salud' ? 'info' : 'neutral'}>
              {endpoint.group}
            </Badge>
            <CopyButton value={endpoint.path} label="Ruta" />
          </div>
        </li>
      ))}
    </ul>
  )
}
