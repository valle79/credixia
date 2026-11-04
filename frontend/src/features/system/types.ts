export type HealthKind = 'live' | 'ready'

export type HealthState = 'healthy' | 'unhealthy' | 'unknown'

export interface PingResult {
  status: string
  service: string
  environment: string
  version: string
  utc: string
  httpStatus: number
  latencyMs: number
  receivedAt: string
  correlationId: string | null
}

export interface HealthResult {
  kind: HealthKind
  path: string
  state: HealthState
  detail: string
  httpStatus: number
  latencyMs: number
  receivedAt: string
  correlationId: string | null
}

export interface LatencySample {
  at: number
  latencyMs: number
}
