import { afterEach, describe, expect, it, vi } from 'vitest'
import { apiFetch, apiProbeText } from './api'

describe('api client', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('sends a correlation id and parses json responses', async () => {
    const fetchMock = vi.fn().mockResolvedValue(
      new Response(JSON.stringify({ status: 'ok' }), {
        status: 200,
        headers: { 'Content-Type': 'application/json' },
      }),
    )
    vi.stubGlobal('fetch', fetchMock)

    const result = await apiFetch<{ status: string }>('/api/v1/ping')

    expect(result.status).toBe('ok')
    const call = fetchMock.mock.calls[0] as [string, RequestInit]
    expect(call[0]).toBe('/api/v1/ping')
    expect(new Headers(call[1].headers).get('X-Correlation-ID')).toBeTruthy()
    expect(call[1].credentials).toBe('include')
  })

  it('maps problem details to a typed error', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(
        new Response(
          JSON.stringify({
            title: 'No encontrado',
            detail: 'El recurso no existe.',
            code: 'tenant.not_found',
          }),
          { status: 404, headers: { 'Content-Type': 'application/json' } },
        ),
      ),
    )

    await expect(apiFetch('/api/v1/tenants/missing')).rejects.toMatchObject({
      name: 'ApiError',
      status: 404,
      code: 'tenant.not_found',
      message: 'El recurso no existe.',
    })
  })

  it('returns health probes without throwing on http errors', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(
        new Response('Unhealthy', {
          status: 503,
          headers: { 'Content-Type': 'text/plain' },
        }),
      ),
    )

    const result = await apiProbeText('/health/ready')

    expect(result.ok).toBe(false)
    expect(result.status).toBe(503)
    expect(result.data).toBe('Unhealthy')
  })
})
