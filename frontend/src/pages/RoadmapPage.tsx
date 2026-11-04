import { CheckIcon } from '../components/icons'
import { Badge, type BadgeTone } from '../components/ui/Badge'
import { Card, CardBody, CardHeader } from '../components/ui/Card'
import { PageHeader } from '../components/ui/PageHeader'

interface Phase {
  id: string
  title: string
  status: string
  tone: BadgeTone
  description: string
  items: readonly string[]
}

const phases: readonly Phase[] = [
  {
    id: '01',
    title: 'Base técnica',
    status: 'Entregada',
    tone: 'success',
    description:
      'Fundaciones verificadas con pruebas automatizadas y contenedores.',
    items: [
      'Solución .NET 10 por capas con Clean Architecture',
      'Persistencia PostgreSQL, migraciones y multitenancy',
      'API con health checks, ProblemDetails y observabilidad',
      'Portal React 19 con TypeScript estricto',
      'Docker Compose y pipeline de integración continua',
    ],
  },
  {
    id: '02',
    title: 'Identidad y tenancy',
    status: 'Siguiente',
    tone: 'info',
    description:
      'Acceso seguro por organización, usuarios, roles y permisos.',
    items: [
      'JWT de corta duración y refresh token rotativo',
      'Resolución de tenant desde la sesión',
      'Row Level Security activo en PostgreSQL',
      'Gestión de membresías y roles',
      'Auditoría de operaciones críticas',
    ],
  },
  {
    id: '03',
    title: 'Crédito y cobranza',
    status: 'Planificada',
    tone: 'neutral',
    description:
      'Ciclo completo del préstamo, desde la solicitud hasta el cobro.',
    items: [
      'Clientes, solicitudes y evaluación',
      'Motor financiero y cronogramas versionados',
      'Desembolsos, pagos y reversos auditados',
      'Caja, mora y gestión de cobranza',
      'Documentos y notificaciones',
    ],
  },
  {
    id: '04',
    title: 'Reportes y analítica',
    status: 'Planificada',
    tone: 'neutral',
    description:
      'Indicadores operativos y financieros con exportación.',
    items: [
      'Cartera vigente, vencida y recuperación',
      'Rentabilidad y proyecciones de flujo',
      'Exportaciones PDF y Excel',
      'Jobs en segundo plano',
    ],
  },
]

export function RoadmapPage() {
  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Producto"
        title="Hoja de ruta"
        description="Alcance entregado y siguientes fases del sistema de préstamos."
        actions={<Badge tone="success">Fase 1 completada</Badge>}
      />

      <Card className="border-brand-200 bg-brand-50/50">
        <CardBody className="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
          <div>
            <p className="text-sm font-semibold text-slate-900">
              El backend actual solo expone endpoints de sistema
            </p>
            <p className="mt-0.5 text-xs leading-relaxed text-slate-600">
              El portal muestra datos reales de ping y health. Los módulos de
              clientes, préstamos y cobranza aún no tienen API en esta fase.
            </p>
          </div>
          <Badge tone="warning">Sin datos simulados</Badge>
        </CardBody>
      </Card>

      <Card>
        <CardHeader
          title="Fases del producto"
          description="Secuencia planificada y verificable."
        />
        <CardBody>
          <ol className="space-y-0">
            {phases.map((phase, index) => (
              <li
                key={phase.id}
                className="relative border-l border-slate-200 pb-8 pl-6 last:pb-0"
              >
                <span
                  className={
                    phase.tone === 'success'
                      ? 'absolute top-1 -left-[7px] size-3.5 rounded-full border-2 border-white bg-emerald-500'
                      : phase.tone === 'info'
                        ? 'absolute top-1 -left-[7px] size-3.5 rounded-full border-2 border-white bg-sky-500'
                        : 'absolute top-1 -left-[7px] size-3.5 rounded-full border-2 border-white bg-slate-300'
                  }
                />

                <div className="flex flex-wrap items-center gap-2">
                  <span className="font-mono text-[10px] font-medium tracking-wide text-slate-400">
                    FASE {phase.id}
                  </span>
                  <Badge tone={phase.tone}>{phase.status}</Badge>
                  {index === 1 ? (
                    <span className="text-[10px] font-medium text-sky-600">
                      Próximo hito
                    </span>
                  ) : null}
                </div>

                <h3 className="mt-2 text-base font-semibold text-slate-900">
                  {phase.title}
                </h3>
                <p className="mt-1 text-xs leading-relaxed text-slate-500">
                  {phase.description}
                </p>

                <ul className="mt-3 grid gap-2 sm:grid-cols-2">
                  {phase.items.map((item) => (
                    <li
                      key={item}
                      className="flex items-start gap-2 text-xs text-slate-600"
                    >
                      <span
                        className={
                          phase.tone === 'success'
                            ? 'mt-0.5 grid size-4 shrink-0 place-items-center rounded-full bg-emerald-50 text-emerald-700'
                            : 'mt-0.5 grid size-4 shrink-0 place-items-center rounded-full bg-slate-100 text-slate-400'
                        }
                      >
                        <CheckIcon className="size-2.5" />
                      </span>
                      <span className="leading-relaxed">{item}</span>
                    </li>
                  ))}
                </ul>
              </li>
            ))}
          </ol>
        </CardBody>
      </Card>
    </div>
  )
}
