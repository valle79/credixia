import { Card, CardBody, CardHeader } from '../../components/ui/Card'
import { Sparkline } from '../../components/ui/Sparkline'
import { formatLatency } from '../../lib/format'
import type { LatencySample } from './types'

interface LatencyCardProps {
  samples: readonly LatencySample[]
}

function average(values: readonly number[]): number {
  return values.reduce((total, value) => total + value, 0) / values.length
}

export function LatencyCard({ samples }: LatencyCardProps) {
  const values = samples.map((sample) => sample.latencyMs)
  const stats =
    values.length > 0
      ? {
          min: Math.min(...values),
          avg: average(values),
          max: Math.max(...values),
        }
      : null

  return (
    <Card>
      <CardHeader
        title="Latencia de la API"
        description="Muestras tomadas durante esta sesión."
        action={
          <span className="font-mono text-[10px] text-slate-400">
            {values.length} {values.length === 1 ? 'muestra' : 'muestras'}
          </span>
        }
      />
      <CardBody>
        <Sparkline values={values} height={72} />

        <dl className="mt-4 grid grid-cols-3 gap-3 border-t border-slate-100 pt-4">
          <div>
            <dt className="text-[10px] font-medium tracking-wide text-slate-400 uppercase">
              Mínima
            </dt>
            <dd className="mt-1 font-mono text-xs font-medium text-slate-700">
              {formatLatency(stats?.min)}
            </dd>
          </div>
          <div>
            <dt className="text-[10px] font-medium tracking-wide text-slate-400 uppercase">
              Promedio
            </dt>
            <dd className="mt-1 font-mono text-xs font-medium text-slate-700">
              {formatLatency(stats?.avg)}
            </dd>
          </div>
          <div>
            <dt className="text-[10px] font-medium tracking-wide text-slate-400 uppercase">
              Máxima
            </dt>
            <dd className="mt-1 font-mono text-xs font-medium text-slate-700">
              {formatLatency(stats?.max)}
            </dd>
          </div>
        </dl>
      </CardBody>
    </Card>
  )
}
