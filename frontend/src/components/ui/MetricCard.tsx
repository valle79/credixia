import type { ReactNode } from 'react'
import { cn } from '../../lib/cn'

export type MetricTone = 'neutral' | 'success' | 'warning' | 'danger'

const toneClasses: Record<MetricTone, string> = {
  neutral: 'bg-slate-100 text-slate-600',
  success: 'bg-emerald-50 text-emerald-600',
  warning: 'bg-amber-50 text-amber-600',
  danger: 'bg-rose-50 text-rose-600',
}

interface MetricCardProps {
  label: string
  value: ReactNode
  hint: string
  icon: ReactNode
  tone?: MetricTone
  badge?: ReactNode
}

export function MetricCard({
  label,
  value,
  hint,
  icon,
  tone = 'neutral',
  badge,
}: MetricCardProps) {
  return (
    <div className="rounded-2xl border border-slate-200/80 bg-white p-5 shadow-card">
      <div className="flex items-start justify-between gap-3">
        <span
          className={cn(
            'grid size-9 place-items-center rounded-xl',
            toneClasses[tone],
          )}
        >
          {icon}
        </span>
        {badge}
      </div>
      <p className="mt-4 text-xs font-medium text-slate-500">{label}</p>
      <p className="mt-1 text-xl font-semibold tracking-tight text-slate-950">
        {value}
      </p>
      <p className="mt-1 truncate text-[11px] text-slate-400" title={hint}>
        {hint}
      </p>
    </div>
  )
}
