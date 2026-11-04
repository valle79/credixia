import type { HTMLAttributes } from 'react'
import { cn } from '../../lib/cn'

export type BadgeTone =
  | 'default'
  | 'neutral'
  | 'success'
  | 'warning'
  | 'danger'
  | 'error'
  | 'info'

type BadgeVariant = 'default' | 'success' | 'warning' | 'danger' | 'error' | 'info'

interface BadgeProps extends HTMLAttributes<HTMLSpanElement> {
  tone?: BadgeTone
  variant?: BadgeVariant
}

export function Badge({ className, tone, variant = 'default', children, ...props }: BadgeProps) {
  const resolvedTone = tone ?? variant

  return (
    <span
      className={cn(
        'inline-flex items-center gap-1.5 rounded-full px-2.5 py-0.5 text-xs font-medium',
        {
          'bg-slate-100 text-slate-700': resolvedTone === 'default' || resolvedTone === 'neutral',
          'bg-emerald-100 text-emerald-700': resolvedTone === 'success',
          'bg-amber-100 text-amber-700': resolvedTone === 'warning',
          'bg-red-100 text-red-700': resolvedTone === 'danger' || resolvedTone === 'error',
          'bg-blue-100 text-blue-700': resolvedTone === 'info',
        },
        className
      )}
      {...props}
    >
      {children}
    </span>
  )
}
