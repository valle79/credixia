import {
  ExternalLinkIcon,
  ShieldIcon,
  TerminalIcon,
} from '../components/icons'
import { Badge } from '../components/ui/Badge'
import { Card, CardBody, CardHeader } from '../components/ui/Card'
import { CopyButton } from '../components/ui/CopyButton'
import { DetailRow } from '../components/ui/DetailRow'
import { PageHeader } from '../components/ui/PageHeader'
import { ApiConsole } from '../features/system/ApiConsole'
import { EndpointCatalog } from '../features/system/EndpointCatalog'
import { API_ENDPOINTS, OPENAPI_PATHS } from '../features/system/endpoints'
import { API_BASE_URL, resolveApiUrl } from '../lib/api'

function absoluteUrl(path: string): string {
  const resolved = resolveApiUrl(path)

  if (resolved.startsWith('http')) {
    return resolved
  }

  if (typeof window === 'undefined') {
    return resolved
  }

  return `${window.location.origin}${resolved}`
}

function DocumentationLink({
  href,
  title,
  description,
}: {
  href: string
  title: string
  description: string
}) {
  return (
    <a
      href={href}
      target="_blank"
      rel="noreferrer"
      className="group flex items-center justify-between gap-4 rounded-xl border border-slate-200 px-4 py-3 transition hover:border-brand-300 hover:bg-brand-50/40"
    >
      <div className="min-w-0">
        <p className="text-sm font-medium text-slate-900">{title}</p>
        <p className="mt-0.5 truncate font-mono text-[11px] text-slate-500">
          {description}
        </p>
      </div>
      <ExternalLinkIcon className="size-4 shrink-0 text-slate-400 transition group-hover:text-brand-600" />
    </a>
  )
}

export function ApiPage() {
  const origin = API_BASE_URL || (typeof window === 'undefined' ? '—' : window.location.origin)
  const pingUrl = absoluteUrl('/api/v1/ping')

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Recursos"
        title="API y recursos"
        description="Superficie pública del backend actual, documentación OpenAPI y consola para ejecutar solicitudes reales."
        actions={<Badge tone="info">Fase 1</Badge>}
      />

      <div className="grid gap-4 lg:grid-cols-2">
        <Card>
          <CardHeader
            title="Base de la API"
            description="El frontend usa el proxy del navegador por defecto."
            action={<CopyButton value={pingUrl} label="URL" />}
          />
          <CardBody className="space-y-3">
            <DetailRow label="Origen" value={origin} mono />
            <DetailRow label="Ping" value={pingUrl} mono />
            <DetailRow
              label="Modo"
              value={API_BASE_URL ? 'URL absoluta' : 'Proxy del navegador'}
            />
            <DetailRow label="API version" value="v1" mono />
          </CardBody>
        </Card>

        <Card>
          <CardHeader
            title="Documentación"
            description="Disponible cuando la API expone OpenAPI."
            action={<TerminalIcon className="size-4 text-slate-400" />}
          />
          <CardBody className="space-y-2.5">
            <DocumentationLink
              href={OPENAPI_PATHS.reference}
              title="Referencia Scalar"
              description={OPENAPI_PATHS.reference}
            />
            <DocumentationLink
              href={OPENAPI_PATHS.document}
              title="Documento OpenAPI"
              description={OPENAPI_PATHS.document}
            />
            <p className="pt-1 text-[11px] leading-relaxed text-slate-400">
              En producción la documentación se desactiva con
              {' '}
              <code className="rounded bg-slate-100 px-1 py-0.5 font-mono text-[10px] text-slate-600">
                Api__ExposeOpenApi=false
              </code>
              .
            </p>
          </CardBody>
        </Card>
      </div>

      <Card>
        <CardHeader
          title="Endpoints públicos"
          description="Todos los endpoints actuales son anónimos y de solo lectura."
          action={
            <Badge tone="neutral">{`${API_ENDPOINTS.length} rutas`}</Badge>
          }
        />
        <EndpointCatalog />
      </Card>

      <div className="grid gap-4 lg:grid-cols-2">
        <ApiConsole />

        <Card>
          <CardHeader
            title="Convenciones"
            description="Contratos transversales de la API."
          />
          <CardBody>
            <ul className="space-y-4 text-xs leading-relaxed text-slate-600">
              <li className="flex gap-3">
                <span className="mt-0.5 grid size-6 shrink-0 place-items-center rounded-lg bg-brand-50 text-brand-700">
                  <ShieldIcon className="size-3.5" />
                </span>
                <div>
                  <p className="font-medium text-slate-900">ProblemDetails</p>
                  <p className="mt-0.5 text-slate-500">
                    Los errores usan el formato estándar con
                    {' '}
                    <code className="font-mono text-[11px] text-slate-700">
                      title
                    </code>
                    ,{' '}
                    <code className="font-mono text-[11px] text-slate-700">
                      detail
                    </code>
                    {' '}y{' '}
                    <code className="font-mono text-[11px] text-slate-700">
                      code
                    </code>
                    .
                  </p>
                </div>
              </li>
              <li className="flex gap-3">
                <span className="mt-0.5 grid size-6 shrink-0 place-items-center rounded-lg bg-sky-50 text-sky-700">
                  <TerminalIcon className="size-3.5" />
                </span>
                <div>
                  <p className="font-medium text-slate-900">Correlación</p>
                  <p className="mt-0.5 text-slate-500">
                    Cada solicitud envía
                    {' '}
                    <code className="font-mono text-[11px] text-slate-700">
                      X-Correlation-ID
                    </code>
                    {' '}y la API lo devuelve en la respuesta.
                  </p>
                </div>
              </li>
              <li className="flex gap-3">
                <span className="mt-0.5 grid size-6 shrink-0 place-items-center rounded-lg bg-amber-50 text-amber-700">
                  <ShieldIcon className="size-3.5" />
                </span>
                <div>
                  <p className="font-medium text-slate-900">Autenticación</p>
                  <p className="mt-0.5 text-slate-500">
                    La sesión con access token y refresh rotativo se habilita en
                    la fase 2. Hoy no se requiere token.
                  </p>
                </div>
              </li>
            </ul>
          </CardBody>
        </Card>
      </div>
    </div>
  )
}
