import { useCallback } from 'react'
import { useIsFetching, useQuery, useQueryClient } from '@tanstack/react-query'
import { fetchHealth, fetchPing } from './api'
import { recordLatency } from './latencyStore'
import type { HealthKind } from './types'

const all = ['system'] as const

export const systemKeys = {
  all,
  ping: [...all, 'ping'] as const,
  health: (kind: HealthKind) => [...all, 'health', kind] as const,
}

export const REFRESH_INTERVAL_MS = 20_000

async function queryPing(signal: AbortSignal) {
  const result = await fetchPing(signal)
  const at = Date.parse(result.receivedAt)

  if (!Number.isNaN(at)) {
    recordLatency('ping', { at, latencyMs: result.latencyMs })
  }

  return result
}

export function usePing(refetchInterval: number | false = REFRESH_INTERVAL_MS) {
  return useQuery({
    queryKey: systemKeys.ping,
    queryFn: ({ signal }) => queryPing(signal),
    refetchInterval,
    staleTime: 10_000,
  })
}

export function useHealth(
  kind: HealthKind,
  refetchInterval: number | false = REFRESH_INTERVAL_MS,
) {
  return useQuery({
    queryKey: systemKeys.health(kind),
    queryFn: ({ signal }) => fetchHealth(kind, signal),
    refetchInterval,
    staleTime: 10_000,
  })
}

export function useRefreshSystem() {
  const queryClient = useQueryClient()
  const isFetching = useIsFetching({ queryKey: systemKeys.all }) > 0

  const refresh = useCallback(() => {
    void queryClient.invalidateQueries({ queryKey: systemKeys.all })
  }, [queryClient])

  return { refresh, isFetching }
}
