import { cn } from '../../lib/cn'

interface SparklineProps {
  values: readonly number[]
  className?: string
  height?: number
}

const VIEWBOX_WIDTH = 320

export function Sparkline({ values, className, height = 64 }: SparklineProps) {
  if (values.length < 2) {
    return (
      <div
        className={cn(
          'flex items-center justify-center rounded-lg bg-slate-50 text-[11px] text-slate-400',
          className,
        )}
        style={{ height }}
      >
        Recopilando muestras…
      </div>
    )
  }

  const min = Math.min(...values)
  const max = Math.max(...values)
  const span = Math.max(max - min, 1)
  const padding = 6

  const points = values
    .map((value, index) => {
      const x = (index / (values.length - 1)) * VIEWBOX_WIDTH
      const y =
        height - padding - ((value - min) / span) * (height - padding * 2)
      return `${x.toFixed(2)},${y.toFixed(2)}`
    })
    .join(' ')

  return (
    <svg
      viewBox={`0 0 ${VIEWBOX_WIDTH} ${height}`}
      preserveAspectRatio="none"
      role="img"
      aria-label="Historial de latencia de la API"
      className={cn('w-full text-brand-500', className)}
      style={{ height }}
    >
      <polyline
        points={points}
        fill="none"
        stroke="currentColor"
        strokeWidth="2"
        strokeLinecap="round"
        strokeLinejoin="round"
        vectorEffect="non-scaling-stroke"
      />
    </svg>
  )
}
