import { StatusPill, type ServiceState } from '../../components/ui/StatusPill'
import { formatLatency, formatTime } from '../../lib/format'

interface ServiceStatusRowProps {
  name: string
  description: string
  state: ServiceState
  latencyMs?: number | undefined
  httpStatus?: number | undefined
  checkedAt?: string | undefined
  error?: string | undefined
}

export function ServiceStatusRow({
  name,
  description,
  state,
  latencyMs,
  httpStatus,
  checkedAt,
  error,
}: ServiceStatusRowProps) {
  return (
    <div className="flex flex-col gap-3 py-4 sm:flex-row sm:items-center sm:justify-between">
      <div className="min-w-0">
        <div className="flex flex-wrap items-center gap-2">
          <p className="text-sm font-medium text-slate-900">{name}</p>
          {httpStatus !== undefined ? (
            <span className="rounded-md bg-slate-50 px-1.5 py-0.5 font-mono text-[10px] font-medium text-slate-500 ring-1 ring-inset ring-slate-200">
              HTTP {httpStatus}
            </span>
          ) : null}
        </div>
        <p className="mt-0.5 text-xs text-slate-500">{description}</p>
        {error ? (
          <p className="mt-1 text-xs text-rose-600">{error}</p>
        ) : null}
      </div>

      <div className="flex shrink-0 items-center justify-between gap-5 sm:justify-end">
        <div className="text-right">
          <p className="font-mono text-xs font-medium text-slate-700">
            {formatLatency(latencyMs)}
          </p>
          <p className="text-[10px] text-slate-400">
            {checkedAt ? `Última: ${formatTime(checkedAt)}` : 'Sin datos'}
          </p>
        </div>
        <StatusPill state={state} />
      </div>
    </div>
  )
}
