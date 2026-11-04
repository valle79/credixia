import { apiProbeText, apiRequest, type ApiRequestOptions } from '../../lib/api'
import type {
  HealthKind,
  HealthResult,
  HealthState,
  PingResult,
} from './types'

interface PingPayload {
  status: string
  service: string
  environment: string
  version: string
  utc: string
}

const HEALTH_PATHS: Record<HealthKind, string> = {
  live: '/health/live',
  ready: '/health/ready',
}

const REQUEST_TIMEOUT_MS = 8_000

function optionsWithSignal(signal: AbortSignal | undefined): ApiRequestOptions {
  const options: ApiRequestOptions = { timeoutMs: REQUEST_TIMEOUT_MS }

  if (signal) {
    options.signal = signal
  }

  return options
}

function resolveHealthState(ok: boolean, detail: string): HealthState {
  const normalized = detail.toLowerCase()

  if (!ok || normalized === 'unhealthy') {
    return 'unhealthy'
  }

  if (normalized === 'healthy') {
    return 'healthy'
  }

  return 'unknown'
}

export async function fetchPing(signal?: AbortSignal): Promise<PingResult> {
  const result = await apiRequest<PingPayload>(
    '/api/v1/ping',
    optionsWithSignal(signal),
  )

  return {
    ...result.data,
    httpStatus: result.status,
    latencyMs: result.latencyMs,
    receivedAt: result.receivedAt,
    correlationId: result.correlationId,
  }
}

export async function fetchHealth(
  kind: HealthKind,
  signal?: AbortSignal,
): Promise<HealthResult> {
  const path = HEALTH_PATHS[kind]
  const result = await apiProbeText(path, optionsWithSignal(signal))
  const detail = result.data.trim()

  return {
    kind,
    path,
    state: resolveHealthState(result.ok, detail),
    detail: detail || `HTTP ${result.status}`,
    httpStatus: result.status,
    latencyMs: result.latencyMs,
    receivedAt: result.receivedAt,
    correlationId: result.correlationId,
  }
}
