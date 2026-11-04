import { cn } from '../../lib/cn'
import { Badge, type BadgeTone } from './Badge'

export type ServiceState = 'loading' | 'online' | 'offline' | 'degraded'

interface StateConfig {
  label: string
  tone: BadgeTone
  dot: string
}

const stateConfig: Record<ServiceState, StateConfig> = {
  loading: {
    label: 'Verificando',
    tone: 'warning',
    dot: 'bg-amber-400 animate-pulse',
  },
  online: {
    label: 'Operativo',
    tone: 'success',
    dot: 'bg-emerald-500',
  },
  degraded: {
    label: 'Degradado',
    tone: 'warning',
    dot: 'bg-amber-500',
  },
  offline: {
    label: 'Sin conexión',
    tone: 'danger',
    dot: 'bg-rose-500',
  },
}

interface StatusPillProps {
  state: ServiceState
  label?: string
}

export function StatusPill({ state, label }: StatusPillProps) {
  const config = stateConfig[state]

  return (
    <Badge tone={config.tone}>
      <span className={cn('size-1.5 rounded-full', config.dot)} />
      {label ?? config.label}
    </Badge>
  )
}
