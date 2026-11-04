import { useState } from 'react'
import {
  ActivityIcon,
  DatabaseIcon,
  ServerIcon,
  TerminalIcon,
} from '../components/icons'
import { Badge } from '../components/ui/Badge'
import { Button } from '../components/ui/Button'
import { Card, CardBody, CardHeader } from '../components/ui/Card'
import { CopyButton } from '../components/ui/CopyButton'
import { DetailRow } from '../components/ui/DetailRow'
import { MetricCard, type MetricTone } from '../components/ui/MetricCard'
import { PageHeader } from '../components/ui/PageHeader'
import { Skeleton } from '../components/ui/Skeleton'
import { StatusPill, type ServiceState } from '../components/ui/StatusPill'
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
import type { HealthResult } from '../features/system/types'
import { cn } from '../lib/cn'
import { formatLatency, formatTime } from '../lib/format'
import { useNow } from '../lib/useNow'

const AUTO_REFRESH_MS = 10_000

const toneByState: Record<ServiceState, MetricTone> = {
  loading: 'neutral',
  online: 'success',
  degraded: 'warning',
  offline: 'danger',
}

function readinessLabel(health: HealthResult | undefined, isError: boolean): string {
  if (isError) {
    return 'Sin respuesta'
  }

  return health?.state === 'healthy' ? 'Operativa' : 'No disponible'
}

function RawBlock({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <div className="mb-2 flex items-center justify-between gap-3">
        <p className="font-mono text-[11px] font-medium text-slate-500">
          {label}
        </p>
        <CopyButton value={value} label="Copiar" />
      </div>
      <pre className="max-h-40 overflow-auto rounded-xl bg-slate-950 p-4 font-mono text-[11px] leading-relaxed text-slate-200">
        {value}
      </pre>
    </div>
  )
}

export function SystemPage() {
  const [autoRefresh, setAutoRefresh] = useState(true)
  const interval = autoRefresh ? AUTO_REFRESH_MS : false

  const ping = usePing(interval)
  const live = useHealth('live', interval)
  const ready = useHealth('ready', interval)
  const samples = useLatencyHistory()
  const now = useNow(5_000)

  const apiState = queryServiceState(ping.isPending, ping.isError)
  const liveState = queryServiceState(live.isPending, live.isError)
  const readyState = healthServiceState(ready.isPending, ready.isError, ready.data)

  const pingResponse = ping.data
    ? JSON.stringify(
        {
          status: ping.data.status,
          service: ping.data.service,
          environment: ping.data.environment,
          version: ping.data.version,
          utc: ping.data.utc,
        },
        null,
        2,
      )
    : 'Sin datos'

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Diagnóstico"
        title="Estado del sistema"
        description="Verificación directa de los servicios, la base de datos y la trazabilidad de cada solicitud."
        actions={
          <>
            <Button
              size="sm"
              variant={autoRefresh ? 'secondary' : 'ghost'}
              onClick={() => setAutoRefresh((value) => !value)}
              aria-pressed={autoRefresh}
            >
              <ActivityIcon
                className={cn(
                  'size-3.5',
                  autoRefresh ? 'text-brand-600' : 'text-slate-400',
                )}
              />
              Auto {autoRefresh ? 'activo' : 'pausado'}
            </Button>
            <RefreshButton />
          </>
        }
      />

      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
        <MetricCard
          label="API de operaciones"
          value={
            ping.isPending ? (
              <Skeleton className="h-6 w-24" />
            ) : (
              serviceStateLabel(apiState)
            )
          }
          hint={`Latencia ${formatLatency(ping.data?.latencyMs)}`}
          icon={<ActivityIcon className="size-4.5" />}
          tone={toneByState[apiState]}
          badge={<StatusPill state={apiState} />}
        />

        <MetricCard
          label="Liveness"
          value={
            live.isPending ? (
              <Skeleton className="h-6 w-24" />
            ) : (
              serviceStateLabel(liveState)
            )
          }
          hint={`Latencia ${formatLatency(live.data?.latencyMs)}`}
          icon={<ServerIcon className="size-4.5" />}
          tone={toneByState[liveState]}
          badge={<StatusPill state={liveState} />}
        />

        <MetricCard
          label="Readiness"
          value={
            ready.isPending ? (
              <Skeleton className="h-6 w-24" />
            ) : (
              readinessLabel(ready.data, ready.isError)
            )
          }
          hint={`Latencia ${formatLatency(ready.data?.latencyMs)}`}
          icon={<DatabaseIcon className="size-4.5" />}
          tone={toneByState[readyState]}
          badge={<StatusPill state={readyState} />}
        />
      </div>

      <div className="grid gap-4 lg:grid-cols-3">
        <Card className="lg:col-span-2">
          <CardHeader
            title="Servicios monitoreados"
            description={
              autoRefresh
                ? `Actualización automática cada ${AUTO_REFRESH_MS / 1_000} segundos.`
                : 'Actualización automática pausada.'
            }
            action={
              <Badge tone={autoRefresh ? 'success' : 'neutral'}>
                {autoRefresh ? 'En vivo' : 'Manual'}
              </Badge>
            }
          />
          <CardBody className="divide-y divide-slate-100 py-0">
            <ServiceStatusRow
              name="API de operaciones"
              description="/api/v1/ping"
              state={apiState}
              latencyMs={ping.data?.latencyMs}
              httpStatus={ping.data?.httpStatus}
              checkedAt={ping.data?.receivedAt}
              error={ping.error?.message}
            />
            <ServiceStatusRow
              name="Liveness"
              description="/health/live"
              state={liveState}
              latencyMs={live.data?.latencyMs}
              httpStatus={live.data?.httpStatus}
              checkedAt={live.data?.receivedAt}
              error={live.error?.message}
            />
            <ServiceStatusRow
              name="Readiness"
              description="/health/ready"
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
            title="Trazabilidad"
            description="Identificadores devueltos por el backend."
          />
          <CardBody className="space-y-3">
            <DetailRow
              label="Ping"
              value={ping.data?.correlationId ?? '—'}
              mono
            />
            <DetailRow
              label="Liveness"
              value={live.data?.correlationId ?? '—'}
              mono
            />
            <DetailRow
              label="Readiness"
              value={ready.data?.correlationId ?? '—'}
              mono
            />
            <div className="border-t border-slate-100 pt-3">
              <DetailRow
                label="Ping recibido"
                value={ping.data ? formatTime(ping.data.receivedAt) : '—'}
              />
              <div className="h-2" />
              <DetailRow
                label="Liveness recibido"
                value={live.data ? formatTime(live.data.receivedAt) : '—'}
              />
              <div className="h-2" />
              <DetailRow
                label="Readiness recibido"
                value={ready.data ? formatTime(ready.data.receivedAt) : '—'}
              />
            </div>
            <p className="border-t border-slate-100 pt-3 text-[11px] leading-relaxed text-slate-400">
              Hora local del navegador:{' '}
              {formatTime(now)}
            </p>
          </CardBody>
        </Card>
      </div>

      <div className="grid gap-4 lg:grid-cols-3">
        <LatencyCard samples={samples} />

        <Card className="lg:col-span-2">
          <CardHeader
            title="Respuestas recientes"
            description="Cuerpo exacto devuelto por cada endpoint."
            action={<TerminalIcon className="size-4 text-slate-400" />}
          />
          <CardBody className="space-y-5">
            <RawBlock label="/api/v1/ping" value={pingResponse} />
            <RawBlock
              label="/health/live"
              value={live.data?.detail ?? 'Sin datos'}
            />
            <RawBlock
              label="/health/ready"
              value={ready.data?.detail ?? 'Sin datos'}
            />
          </CardBody>
        </Card>
      </div>
    </div>
  )
}
