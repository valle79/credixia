import { useState } from 'react'
import { useMutation } from '@tanstack/react-query'
import { TerminalIcon } from '../../components/icons'
import { Badge } from '../../components/ui/Badge'
import { Button } from '../../components/ui/Button'
import { Card, CardBody, CardHeader } from '../../components/ui/Card'
import { apiProbe } from '../../lib/api'
import { formatLatency } from '../../lib/format'
import { API_ENDPOINTS } from './endpoints'

function formatBody(value: unknown): string {
  if (typeof value === 'string') {
    return value
  }

  try {
    return JSON.stringify(value, null, 2)
  } catch {
    return String(value)
  }
}

export function ApiConsole() {
  const [selectedPath, setSelectedPath] = useState(API_ENDPOINTS[0]?.path ?? '')
  const mutation = useMutation({
    mutationFn: (path: string) => apiProbe(path, { timeoutMs: 8_000 }),
  })

  function handleRun() {
    mutation.mutate(selectedPath)
  }

  return (
    <Card>
      <CardHeader
        title="Consola de API"
        description="Ejecuta los endpoints públicos y revisa la respuesta real."
        action={<TerminalIcon className="size-4 text-slate-400" />}
      />
      <CardBody className="space-y-4">
        <div className="flex flex-col gap-2 sm:flex-row">
          <label className="sr-only" htmlFor="endpoint-select">
            Endpoint
          </label>
          <select
            id="endpoint-select"
            value={selectedPath}
            onChange={(event) => {
              setSelectedPath(event.target.value)
              mutation.reset()
            }}
            className="h-10 min-w-0 flex-1 rounded-xl border border-slate-200 bg-white px-3 font-mono text-xs text-slate-700 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-brand-500"
          >
            {API_ENDPOINTS.map((endpoint) => (
              <option key={endpoint.path} value={endpoint.path}>
                {endpoint.method} {endpoint.path}
              </option>
            ))}
          </select>
          <Button
            variant="primary"
            onClick={handleRun}
            disabled={mutation.isPending || !selectedPath}
          >
            {mutation.isPending ? 'Ejecutando…' : 'Ejecutar'}
          </Button>
        </div>

        {mutation.data ? (
          <div className="space-y-3">
            <div className="flex flex-wrap items-center gap-3 text-xs text-slate-500">
              <Badge tone={mutation.data.ok ? 'success' : 'danger'}>
                HTTP {mutation.data.status}
              </Badge>
              <span className="font-mono">
                {formatLatency(mutation.data.latencyMs)}
              </span>
              {mutation.data.correlationId ? (
                <span className="truncate font-mono text-[10px] text-slate-400">
                  {mutation.data.correlationId}
                </span>
              ) : null}
            </div>
            <pre className="max-h-64 overflow-auto rounded-xl bg-slate-950 p-4 font-mono text-[11px] leading-relaxed text-slate-200">
              {formatBody(mutation.data.data)}
            </pre>
          </div>
        ) : null}

        {mutation.error ? (
          <p className="rounded-xl border border-rose-200 bg-rose-50 px-3 py-2 text-xs text-rose-700">
            {mutation.error.message}
          </p>
        ) : null}

        {!mutation.data && !mutation.error && !mutation.isPending ? (
          <p className="rounded-xl border border-dashed border-slate-200 bg-slate-50 px-3 py-6 text-center text-xs text-slate-400">
            Selecciona un endpoint y ejecútalo para ver la respuesta.
          </p>
        ) : null}
      </CardBody>
    </Card>
  )
}
