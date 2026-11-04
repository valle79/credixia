import { Link } from 'react-router-dom'
import {
  ActivityIcon,
  ArrowRightIcon,
  DatabaseIcon,
  ServerIcon,
  SparkIcon,
} from '../components/icons'
import { Badge } from '../components/ui/Badge'
import { buttonStyles } from '../components/ui/buttonStyles'
import { Card, CardBody, CardHeader } from '../components/ui/Card'
import { DetailRow } from '../components/ui/DetailRow'
import { MetricCard, type MetricTone } from '../components/ui/MetricCard'
import { PageHeader } from '../components/ui/PageHeader'
import { Skeleton } from '../components/ui/Skeleton'
import { StatusPill, type ServiceState } from '../components/ui/StatusPill'
import { EndpointCatalog } from '../features/system/EndpointCatalog'
import { LatencyCard } from '../features/system/LatencyCard'
import { RefreshButton } from '../features/system/RefreshButton'
import { ServiceStatusRow } from '../features/system/ServiceStatusRow'
import { useHealth, usePing } from '../features/system/hooks'
import {
  healthServiceState,
  queryServiceState,
  serviceStateLabel,
} from '../features/system/status'
import { useLatencyHistory } from '../features/system/useLatencyHistory'
import {
  formatDateTime,
  formatLatency,
  formatRelative,
} from '../lib/format'
import { useNow } from '../lib/useNow'

const toneByState: Record<ServiceState, MetricTone> = {
  loading: 'neutral',
  online: 'success',
  degraded: 'warning',
  offline: 'danger',
}

function readinessLabel(
  isPending: boolean,
  isError: boolean,
  unavailable: boolean,
): string {
  if (isPending) {
    return 'Verificando'
  }

  if (isError) {
    return 'Sin respuesta'
  }

  return unavailable ? 'No disponible' : 'Operativa'
}

export function DashboardPage() {
  const ping = usePing()
  const live = useHealth('live')
  const ready = useHealth('ready')
  const samples = useLatencyHistory()
  const now = useNow(10_000)

  const apiState = queryServiceState(ping.isPending, ping.isError)
  const liveState = queryServiceState(live.isPending, live.isError)
  const readyState = healthServiceState(ready.isPending, ready.isError, ready.data)
  const readyUnavailable = ready.data?.state !== 'healthy'

  const lastSync =
    ping.dataUpdatedAt > 0
      ? formatRelative(ping.dataUpdatedAt, now)
      : 'Sin datos'

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Operación"
        title="Panel general"
        description="Estado operativo de la API, la base de datos y los servicios que ya expone el backend."
        actions={
          <>
            <RefreshButton />
            <Link to="/sistema" className={buttonStyles('primary', 'sm')}>
              Diagnóstico
              <ArrowRightIcon className="size-3.5" />
            </Link>
          </>
        }
      />

      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <MetricCard
          label="API de operaciones"
          value={
            ping.isPending ? (
              <Skeleton className="h-6 w-24" />
            ) : (
              serviceStateLabel(apiState)
            )
          }
          hint={
            ping.data
              ? `${ping.data.service} · ${formatLatency(ping.data.latencyMs)}`
              : ping.isError
                ? 'No se pudo contactar la API'
                : 'Consultando /api/v1/ping'
          }
          icon={<ActivityIcon className="size-4.5" />}
          tone={toneByState[apiState]}
          badge={<StatusPill state={apiState} />}
        />

        <MetricCard
          label="Base de datos"
          value={
            ready.isPending ? (
              <Skeleton className="h-6 w-24" />
            ) : (
              readinessLabel(
                ready.isPending,
                ready.isError,
                readyUnavailable,
              )
            )
          }
          hint={
            ready.data
              ? `Readiness · ${formatLatency(ready.data.latencyMs)}`
              : ready.isError
                ? 'El chequeo de disponibilidad falló'
                : 'Consultando /health/ready'
          }
          icon={<DatabaseIcon className="size-4.5" />}
          tone={toneByState[readyState]}
          badge={<StatusPill state={readyState} />}
        />

        <MetricCard
          label="Proceso HTTP"
          value={
            live.isPending ? (
              <Skeleton className="h-6 w-24" />
            ) : (
              serviceStateLabel(liveState)
            )
          }
          hint={
            live.data
              ? `Liveness · ${formatLatency(live.data.latencyMs)}`
              : live.isError
                ? 'Sin respuesta de /health/live'
                : 'Consultando /health/live'
          }
          icon={<ServerIcon className="size-4.5" />}
          tone={toneByState[liveState]}
          badge={<StatusPill state={liveState} />}
        />

        <MetricCard
          label="Versión desplegada"
          value={ping.data ? `v${ping.data.version}` : '—'}
          hint={
            ping.data
              ? `${ping.data.environment} · ${formatDateTime(ping.data.utc)}`
              : 'Sin información del servicio'
          }
          icon={<SparkIcon className="size-4.5" />}
          badge={<Badge tone="neutral">API v1</Badge>}
        />
      </div>

      <div className="grid gap-4 lg:grid-cols-3">
        <Card className="lg:col-span-2">
          <CardHeader
            title="Salud de servicios"
            description="Chequeos en vivo del backend cada 20 segundos."
            action={<Badge tone="neutral">Automático</Badge>}
          />
          <CardBody className="divide-y divide-slate-100 py-0">
            <ServiceStatusRow
              name="API de operaciones"
              description="Proceso HTTP principal mediante /api/v1/ping."
              state={apiState}
              latencyMs={ping.data?.latencyMs}
              httpStatus={ping.data?.httpStatus}
              checkedAt={ping.data?.receivedAt}
              error={ping.error?.message}
            />
            <ServiceStatusRow
              name="Liveness"
              description="Confirma que el proceso sigue vivo."
              state={liveState}
              latencyMs={live.data?.latencyMs}
              httpStatus={live.data?.httpStatus}
              checkedAt={live.data?.receivedAt}
              error={live.error?.message}
            />
            <ServiceStatusRow
              name="Readiness"
              description="Verifica PostgreSQL y dependencias críticas."
              state={readyState}
              latencyMs={ready.data?.latencyMs}
              httpStatus={ready.data?.httpStatus}
              checkedAt={ready.data?.receivedAt}
              error={ready.error?.message}
            />
          </CardBody>
        </Card>

        <Card>
          <CardHeader
            title="Conexión"
            description="Datos reportados por el backend."
            action={
              <Badge tone={ping.isSuccess ? 'success' : 'neutral'}>
                {ping.isSuccess ? 'En vivo' : 'Sin datos'}
              </Badge>
            }
          />
          <CardBody className="space-y-3">
            <DetailRow label="Servicio" value={ping.data?.service ?? '—'} mono />
            <DetailRow label="Entorno" value={ping.data?.environment ?? '—'} />
            <DetailRow
              label="Versión"
              value={ping.data ? `v${ping.data.version}` : '—'}
            />
            <DetailRow
              label="Hora del servidor"
              value={ping.data ? formatDateTime(ping.data.utc) : '—'}
            />
            <DetailRow label="Última sincronización" value={lastSync} />
            <DetailRow
              label="Correlación"
              value={ping.data?.correlationId ?? '—'}
              mono
            />
          </CardBody>
        </Card>
      </div>

      <div className="grid gap-4 lg:grid-cols-3">
        <LatencyCard samples={samples} />

        <Card className="lg:col-span-2">
          <CardHeader
            title="Superficie disponible"
            description="Endpoints públicos que el frontend consume hoy."
            action={
              <Link
                to="/api"
                className="text-xs font-medium text-brand-700 hover:text-brand-800"
              >
                Ver API
              </Link>
            }
          />
          <EndpointCatalog />
        </Card>
      </div>

      <Card className="overflow-hidden border-slate-800 bg-slate-950">
        <div className="flex flex-col gap-4 p-5 sm:flex-row sm:items-center sm:justify-between sm:p-6">
          <div className="max-w-2xl">
            <p className="text-[10px] font-semibold tracking-[0.16em] text-brand-300 uppercase">
              Siguiente fase
            </p>
            <p className="mt-1.5 text-sm font-semibold text-white">
              Identidad, tenants y control de acceso
            </p>
            <p className="mt-1 text-xs leading-relaxed text-slate-400">
              El backend actual entrega la base técnica. Los módulos de clientes,
              préstamos y cobranza se incorporan sobre esta superficie.
            </p>
          </div>
          <Link
            to="/hoja-de-ruta"
            className="inline-flex h-10 shrink-0 items-center gap-2 rounded-xl bg-white px-4 text-sm font-medium text-slate-950 transition hover:bg-slate-100"
          >
            Ver hoja de ruta
            <ArrowRightIcon className="size-4" />
          </Link>
        </div>
      </Card>
    </div>
  )
}
