import type { ServiceState } from '../../components/ui/StatusPill'
import type { HealthResult } from './types'

const stateLabels: Record<ServiceState, string> = {
  loading: 'Verificando',
  online: 'Operativa',
  offline: 'Sin respuesta',
  degraded: 'Degradada',
}

export function serviceStateLabel(state: ServiceState): string {
  return stateLabels[state]
}

export function queryServiceState(
  isPending: boolean,
  isError: boolean,
): ServiceState {
  if (isPending) {
    return 'loading'
  }

  return isError ? 'offline' : 'online'
}

export function healthServiceState(
  isPending: boolean,
  isError: boolean,
  health: HealthResult | undefined,
): ServiceState {
  if (isPending) {
    return 'loading'
  }

  if (isError || health?.state === 'unhealthy') {
    return 'offline'
  }

  if (health?.state === 'unknown') {
    return 'degraded'
  }

  return 'online'
}
