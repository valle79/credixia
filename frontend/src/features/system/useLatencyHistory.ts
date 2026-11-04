import { useSyncExternalStore } from 'react'
import { latencySource } from './latencyStore'
import type { LatencySample } from './types'

const LATENCY_KEY = 'ping'

export function useLatencyHistory(): LatencySample[] {
  return useSyncExternalStore(
    latencySource.subscribe,
    () => latencySource.getSnapshot(LATENCY_KEY),
    () => latencySource.getSnapshot(LATENCY_KEY),
  )
}
