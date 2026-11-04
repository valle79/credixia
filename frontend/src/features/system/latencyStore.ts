import type { LatencySample } from './types'

const EMPTY: LatencySample[] = []
const MAX_SAMPLES = 24
const samples = new Map<string, LatencySample[]>()
const listeners = new Set<() => void>()

export function recordLatency(key: string, sample: LatencySample): void {
  const current = samples.get(key) ?? EMPTY

  if (current.length > 0 && current[current.length - 1]?.at === sample.at) {
    return
  }

  samples.set(key, [...current, sample].slice(-MAX_SAMPLES))
  listeners.forEach((listener) => listener())
}

function subscribe(listener: () => void): () => void {
  listeners.add(listener)
  return () => {
    listeners.delete(listener)
  }
}

export function getLatencySnapshot(key: string): LatencySample[] {
  return samples.get(key) ?? EMPTY
}

export const latencySource = {
  subscribe,
  getSnapshot: getLatencySnapshot,
}
