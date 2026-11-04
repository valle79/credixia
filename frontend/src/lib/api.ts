const configuredBaseUrl = (import.meta.env.VITE_API_BASE_URL ?? '').replace(/\/+$/, '')

export const API_BASE_URL = configuredBaseUrl

const DEFAULT_TIMEOUT_MS = 10_000

export interface ApiResult<T> {
  data: T
  ok: boolean
  status: number
  correlationId: string | null
  latencyMs: number
  receivedAt: string
}

export interface ApiRequestOptions extends RequestInit {
  timeoutMs?: number
  allowHttpError?: boolean
}

export class ApiError extends Error {
  readonly status: number
  readonly code: string | undefined
  readonly correlationId: string | null

  constructor(
    message: string,
    status: number,
    code?: string,
    correlationId: string | null = null,
  ) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.code = code
    this.correlationId = correlationId
  }
}

export function resolveApiUrl(path: string): string {
  const normalized = path.startsWith('/') ? path : `/${path}`
  return `${API_BASE_URL}${normalized}`
}

function correlationId(): string {
  if (typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function') {
    return crypto.randomUUID()
  }

  return `web-${Date.now()}`
}

async function buildHttpError(
  response: Response,
  correlationIdValue: string | null,
): Promise<ApiError> {
  const body = await response.text().catch(() => '')
  let payload: unknown = null

  if (body) {
    try {
      payload = JSON.parse(body) as unknown
    } catch {
      payload = body
    }
  }

  const problem = typeof payload === 'object' && payload !== null ? payload : null
  const detail =
    problem && 'detail' in problem && typeof problem.detail === 'string'
      ? problem.detail
      : typeof payload === 'string' && payload.trim()
        ? payload.trim()
        : null
  const title =
    problem && 'title' in problem && typeof problem.title === 'string'
      ? problem.title
      : null
  const code =
    problem && 'code' in problem && typeof problem.code === 'string'
      ? problem.code
      : undefined

  return new ApiError(
    detail ?? title ?? `La solicitud falló con estado ${response.status}.`,
    response.status,
    code,
    correlationIdValue,
  )
}

async function parseBody<T>(response: Response): Promise<T> {
  if (response.status === 204) {
    return undefined as T
  }

  const contentType = response.headers.get('content-type') ?? ''

  if (contentType.includes('application/json')) {
    return (await response.json()) as T
  }

  return (await response.text()) as T
}

export async function apiRequest<T>(
  path: string,
  options: ApiRequestOptions = {},
): Promise<ApiResult<T>> {
  const {
    timeoutMs = DEFAULT_TIMEOUT_MS,
    allowHttpError = false,
    signal,
    headers: initialHeaders,
    ...init
  } = options

  const controller = new AbortController()
  const headers = new Headers(initialHeaders)
  let timedOut = false

  headers.set('Accept', 'application/json, text/plain;q=0.9')
  headers.set('X-Correlation-ID', headers.get('X-Correlation-ID') ?? correlationId())

  if (init.body !== undefined && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json')
  }

  const abortFromCaller = () => controller.abort()

  if (signal) {
    if (signal.aborted) {
      controller.abort()
    } else {
      signal.addEventListener('abort', abortFromCaller, { once: true })
    }
  }

  const timer = window.setTimeout(() => {
    timedOut = true
    controller.abort()
  }, timeoutMs)

  const startedAt = performance.now()
  let response: Response

  try {
    response = await fetch(resolveApiUrl(path), {
      ...init,
      headers,
      credentials: 'include',
      signal: controller.signal,
    })
  } catch (error) {
    if (timedOut) {
      throw new ApiError('La API tardó demasiado en responder.', 0, 'timeout')
    }

    if (error instanceof DOMException && error.name === 'AbortError') {
      throw error
    }

    throw new ApiError('No se pudo conectar con la API.', 0, 'network_error')
  } finally {
    window.clearTimeout(timer)
    signal?.removeEventListener('abort', abortFromCaller)
  }

  const latencyMs = Math.round(performance.now() - startedAt)
  const receivedAt = new Date().toISOString()
  const responseCorrelationId = response.headers.get('X-Correlation-ID')

  if (!response.ok && !allowHttpError) {
    throw await buildHttpError(response, responseCorrelationId)
  }

  const data = await parseBody<T>(response)

  return {
    data,
    ok: response.ok,
    status: response.status,
    correlationId: responseCorrelationId,
    latencyMs,
    receivedAt,
  }
}

export async function apiFetch<T>(
  path: string,
  options: ApiRequestOptions = {},
): Promise<T> {
  const result = await apiRequest<T>(path, options)
  return result.data
}

export async function apiText(
  path: string,
  options: ApiRequestOptions = {},
): Promise<string> {
  return apiFetch<string>(path, options)
}

export async function apiProbeText(
  path: string,
  options: ApiRequestOptions = {},
): Promise<ApiResult<string>> {
  return apiRequest<string>(path, { ...options, allowHttpError: true })
}

export async function apiProbe(
  path: string,
  options: ApiRequestOptions = {},
): Promise<ApiResult<unknown>> {
  return apiRequest<unknown>(path, { ...options, allowHttpError: true })
}
