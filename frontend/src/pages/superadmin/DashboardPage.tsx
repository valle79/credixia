import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import type { ReactNode } from 'react'
import { platformApi } from '../../api/platform.api'
import { getErrorMessage } from '../../api/client'
import { Card } from '../../components/ui/Card'
import { Button } from '../../components/ui/Button'

const numberFormatter = new Intl.NumberFormat('es-PE')

function formatNumber(value: number): string {
  return numberFormatter.format(value)
}

function getPercentage(part: number, total: number): number {
  if (total <= 0) return 0
  return Math.min(100, Math.max(0, Math.round((part / total) * 100)))
}

type IconName = 'building' | 'users' | 'user-check' | 'pause' | 'arrow-right' | 'refresh' | 'chart'

function Icon({ name, className = 'h-5 w-5' }: { name: IconName; className?: string }): ReactNode {
  const commonProps = {
    className,
    viewBox: '0 0 24 24',
    fill: 'none',
    stroke: 'currentColor',
    strokeWidth: 1.7,
    strokeLinecap: 'round' as const,
    strokeLinejoin: 'round' as const,
    'aria-hidden': true as const,
  }

  switch (name) {
    case 'building':
      return (
        <svg {...commonProps}>
          <path d="M3.5 20.5h17" />
          <path d="M5.5 20.5V5.8a1.3 1.3 0 0 1 1.3-1.3h10.4a1.3 1.3 0 0 1 1.3 1.3v14.7" />
          <path d="M9 8h.01M15 8h.01M9 11.5h.01M15 11.5h.01M9 15h.01M15 15h.01M11 20.5v-3h2v3" />
        </svg>
      )
    case 'users':
      return (
        <svg {...commonProps}>
          <path d="M16 20v-1.5a3.5 3.5 0 0 0-3.5-3.5h-5A3.5 3.5 0 0 0 4 18.5V20" />
          <circle cx="10" cy="7.5" r="3.5" />
          <path d="M17 4.3a3.5 3.5 0 0 1 0 6.8M20 20v-1.5a3.5 3.5 0 0 0-2.7-3.4" />
        </svg>
      )
    case 'user-check':
      return (
        <svg {...commonProps}>
          <circle cx="9" cy="7.5" r="3.5" />
          <path d="M2.8 20v-1.5A3.5 3.5 0 0 1 6.3 15h5.4a3.5 3.5 0 0 1 2.8 1.4M16 19l2 2 4-4" />
        </svg>
      )
    case 'pause':
      return (
        <svg {...commonProps}>
          <circle cx="12" cy="12" r="9" />
          <path d="M9.5 8.5v7M14.5 8.5v7" />
        </svg>
      )
    case 'arrow-right':
      return (
        <svg {...commonProps}>
          <path d="M5 12h14M13 6l6 6-6 6" />
        </svg>
      )
    case 'refresh':
      return (
        <svg {...commonProps}>
          <path d="M20 7v5h-5M4 17v-5h5" />
          <path d="M5.5 9a7 7 0 0 1 11.8-2L20 12M4 12l2.7 5a7 7 0 0 0 11.8-2" />
        </svg>
      )
    case 'chart':
      return (
        <svg {...commonProps}>
          <path d="M4 19.5h16" />
          <path d="M7 16v-4M12 16V7M17 16v-7" />
        </svg>
      )
    default:
      return null
  }
}

type StatCardProps = {
  label: string
  value: number
  detail: string
  icon: IconName
  tone: 'slate' | 'green' | 'blue' | 'amber'
}

const statToneClasses: Record<StatCardProps['tone'], { icon: string; accent: string }> = {
  slate: {
    icon: 'bg-slate-100 text-slate-700',
    accent: 'bg-slate-400',
  },
  green: {
    icon: 'bg-emerald-50 text-emerald-700',
    accent: 'bg-emerald-500',
  },
  blue: {
    icon: 'bg-blue-50 text-blue-700',
    accent: 'bg-blue-500',
  },
  amber: {
    icon: 'bg-amber-50 text-amber-700',
    accent: 'bg-amber-500',
  },
}

function StatCard({ label, value, detail, icon, tone }: StatCardProps) {
  const styles = statToneClasses[tone]

  return (
    <Card className="h-full border border-slate-200 bg-white shadow-sm">
      <div className="flex h-full items-start justify-between gap-4 p-5 sm:p-6">
        <div className="min-w-0">
          <p className="text-sm font-medium text-slate-600">{label}</p>
          <p className="mt-3 text-3xl font-semibold tracking-tight text-slate-950 tabular-nums">
            {formatNumber(value)}
          </p>
          <p className="mt-1.5 text-sm text-slate-500">{detail}</p>
        </div>
        <span className={`inline-flex h-11 w-11 shrink-0 items-center justify-center rounded-xl ${styles.icon}`}>
          <Icon name={icon} className="h-5 w-5" />
        </span>
      </div>
      <div className={`h-0.5 w-full ${styles.accent}`} />
    </Card>
  )
}

function ProgressRow({
  label,
  value,
  percentage,
  color,
}: {
  label: string
  value: number
  percentage: number
  color: 'emerald' | 'amber' | 'blue'
}) {
  const barColors = {
    emerald: 'bg-emerald-500',
    amber: 'bg-amber-500',
    blue: 'bg-blue-500',
  }

  return (
    <div className="space-y-2">
      <div className="flex items-center justify-between gap-3 text-sm">
        <span className="text-slate-600">{label}</span>
        <span className="font-medium tabular-nums text-slate-900">
          {formatNumber(value)} <span className="font-normal text-slate-500">({percentage}%)</span>
        </span>
      </div>
      <div
        className="h-2 overflow-hidden rounded-full bg-slate-100"
        role="progressbar"
        aria-label={label}
        aria-valuemin={0}
        aria-valuemax={100}
        aria-valuenow={percentage}
      >
        <div className={`h-full rounded-full transition-[width] duration-300 ${barColors[color]}`} style={{ width: `${percentage}%` }} />
      </div>
    </div>
  )
}

function QuickAction({
  to,
  title,
  description,
  icon,
}: {
  to: string
  title: string
  description: string
  icon: ReactNode
}) {
  return (
    <Link
      to={to}
      className="group flex items-center gap-3 rounded-lg border border-slate-200 p-4 transition-colors hover:border-slate-300 hover:bg-slate-50 focus:outline-none focus-visible:ring-2 focus-visible:ring-slate-400 focus-visible:ring-offset-2"
    >
      <span className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-slate-100 text-slate-700 transition-colors group-hover:bg-white">
        {icon}
      </span>
      <span className="min-w-0 flex-1">
        <span className="block text-sm font-medium text-slate-900">{title}</span>
        <span className="mt-0.5 block text-xs text-slate-500">{description}</span>
      </span>
      <Icon name="arrow-right" className="h-4 w-4 shrink-0 text-slate-400 transition-transform group-hover:translate-x-0.5 group-hover:text-slate-700" />
    </Link>
  )
}

function DashboardSkeleton() {
  return (
    <div className="space-y-6" aria-label="Cargando dashboard" aria-busy="true">
      <div className="space-y-2">
        <div className="h-7 w-40 animate-pulse rounded bg-slate-200" />
        <div className="h-4 w-64 max-w-full animate-pulse rounded bg-slate-100" />
      </div>
      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        {Array.from({ length: 4 }, (_, index) => (
          <Card key={index} className="border border-slate-200 p-6 shadow-sm">
            <div className="flex justify-between gap-4">
              <div className="flex-1 space-y-3">
                <div className="h-4 w-28 animate-pulse rounded bg-slate-200" />
                <div className="h-8 w-20 animate-pulse rounded bg-slate-200" />
                <div className="h-3 w-32 animate-pulse rounded bg-slate-100" />
              </div>
              <div className="h-11 w-11 animate-pulse rounded-xl bg-slate-100" />
            </div>
          </Card>
        ))}
      </div>
      <div className="grid gap-4 xl:grid-cols-2">
        {Array.from({ length: 2 }, (_, index) => (
          <Card key={index} className="h-56 animate-pulse border border-slate-200 p-6 shadow-sm">
            <div className="h-5 w-44 rounded bg-slate-200" />
            <div className="mt-8 space-y-5">
              <div className="h-3 rounded bg-slate-100" />
              <div className="h-3 rounded bg-slate-100" />
              <div className="h-3 rounded bg-slate-100" />
            </div>
          </Card>
        ))}
      </div>
    </div>
  )
}

export function SuperAdminDashboardPage() {
  const {
    data: dashboard,
    isLoading,
    isError,
    error,
    refetch,
    isFetching,
  } = useQuery({
    queryKey: ['platform', 'dashboard'],
    queryFn: () => platformApi.getDashboard(),
    staleTime: 30_000,
  })

  if (isLoading) return <DashboardSkeleton />

  if (isError) {
    return (
      <section className="mx-auto flex max-w-xl flex-col items-center px-4 py-16 text-center">
        <span className="flex h-12 w-12 items-center justify-center rounded-full bg-red-50 text-red-600">
          <Icon name="chart" className="h-6 w-6" />
        </span>
        <h1 className="mt-4 text-lg font-semibold text-slate-900">No se pudo cargar el dashboard</h1>
        <p className="mt-2 text-sm leading-6 text-slate-600">
          {getErrorMessage(error) || 'Ocurrió un problema al consultar las estadísticas de la plataforma.'}
        </p>
        <Button
          type="button"
          variant="secondary"
          className="mt-5"
          onClick={() => void refetch()}
          disabled={isFetching}
        >
          <Icon name="refresh" className="mr-2 h-4 w-4" />
          {isFetching ? 'Reintentando...' : 'Volver a intentar'}
        </Button>
      </section>
    )
  }

  if (!dashboard) {
    return (
      <section className="rounded-xl border border-slate-200 bg-white px-6 py-14 text-center shadow-sm">
        <h1 className="text-lg font-semibold text-slate-900">No hay información disponible</h1>
        <p className="mt-2 text-sm text-slate-600">No se recibieron estadísticas de la plataforma.</p>
        <Button type="button" variant="secondary" className="mt-5" onClick={() => void refetch()}>
          Actualizar información
        </Button>
      </section>
    )
  }

  const activeTenantPercentage = getPercentage(dashboard.activeTenants, dashboard.totalTenants)
  const suspendedTenantPercentage = getPercentage(dashboard.suspendedTenants, dashboard.totalTenants)
  const activeUserPercentage = getPercentage(dashboard.activeUsers, dashboard.totalUsers)
  const averageMembers = dashboard.totalTenants > 0
    ? (dashboard.totalMembers / dashboard.totalTenants).toFixed(1)
    : '0.0'

  return (
    <main className="space-y-6 pb-8">
      <header className="flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <p className="text-sm font-medium text-slate-500">Administración de la plataforma</p>
          <h1 className="mt-1 text-2xl font-semibold tracking-tight text-slate-950">Dashboard</h1>
          <p className="mt-1 text-sm text-slate-600">Resumen general de organizaciones y usuarios.</p>
        </div>
        <div className="flex items-center gap-2">
          <span className="text-xs text-slate-500">Datos de la plataforma</span>
          <button
            type="button"
            onClick={() => void refetch()}
            disabled={isFetching}
            className="inline-flex h-9 items-center gap-2 rounded-lg border border-slate-200 bg-white px-3 text-sm font-medium text-slate-700 transition-colors hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-60 focus:outline-none focus-visible:ring-2 focus-visible:ring-slate-400 focus-visible:ring-offset-2"
          >
            <Icon name="refresh" className={`h-4 w-4 ${isFetching ? 'animate-spin' : ''}`} />
            Actualizar
          </button>
        </div>
      </header>

      <section aria-label="Indicadores principales" className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <StatCard
          label="Organizaciones"
          value={dashboard.totalTenants}
          detail={`${formatNumber(dashboard.activeTenants)} activas`}
          icon="building"
          tone="slate"
        />
        <StatCard
          label="Usuarios registrados"
          value={dashboard.totalUsers}
          detail={`${formatNumber(dashboard.activeUsers)} activos`}
          icon="users"
          tone="blue"
        />
        <StatCard
          label="Miembros"
          value={dashboard.totalMembers}
          detail="Usuarios vinculados a organizaciones"
          icon="user-check"
          tone="green"
        />
        <StatCard
          label="Organizaciones suspendidas"
          value={dashboard.suspendedTenants}
          detail="Requieren revisión administrativa"
          icon="pause"
          tone="amber"
        />
      </section>

      <section className="grid gap-4 xl:grid-cols-2">
        <Card className="border border-slate-200 bg-white shadow-sm">
          <div className="border-b border-slate-100 px-5 py-4 sm:px-6">
            <h2 className="text-base font-semibold text-slate-900">Estado de las organizaciones</h2>
            <p className="mt-1 text-sm text-slate-500">Distribución según su estado actual.</p>
          </div>
          <div className="space-y-5 p-5 sm:p-6">
            <div className="flex h-2.5 overflow-hidden rounded-full bg-slate-100" aria-label="Distribución de organizaciones">
              <div className="bg-emerald-500 transition-[width]" style={{ width: `${activeTenantPercentage}%` }} />
              <div className="bg-amber-400 transition-[width]" style={{ width: `${suspendedTenantPercentage}%` }} />
            </div>
            <ProgressRow
              label="Activas"
              value={dashboard.activeTenants}
              percentage={activeTenantPercentage}
              color="emerald"
            />
            <ProgressRow
              label="Suspendidas"
              value={dashboard.suspendedTenants}
              percentage={suspendedTenantPercentage}
              color="amber"
            />
            <div className="border-t border-slate-100 pt-4">
              <div className="flex items-center justify-between gap-3 text-sm">
                <span className="text-slate-600">Total de organizaciones</span>
                <span className="font-semibold tabular-nums text-slate-900">{formatNumber(dashboard.totalTenants)}</span>
              </div>
            </div>
          </div>
        </Card>

        <Card className="border border-slate-200 bg-white shadow-sm">
          <div className="border-b border-slate-100 px-5 py-4 sm:px-6">
            <h2 className="text-base font-semibold text-slate-900">Actividad de usuarios</h2>
            <p className="mt-1 text-sm text-slate-500">Usuarios activos respecto al total registrado.</p>
          </div>
          <div className="space-y-5 p-5 sm:p-6">
            <ProgressRow
              label="Usuarios activos"
              value={dashboard.activeUsers}
              percentage={activeUserPercentage}
              color="blue"
            />
            <div className="grid grid-cols-2 gap-3">
              <div className="rounded-lg bg-slate-50 p-4">
                <p className="text-xs font-medium text-slate-500">Usuarios registrados</p>
                <p className="mt-2 text-xl font-semibold tabular-nums text-slate-900">{formatNumber(dashboard.totalUsers)}</p>
              </div>
              <div className="rounded-lg bg-slate-50 p-4">
                <p className="text-xs font-medium text-slate-500">Promedio de miembros por organización</p>
                <p className="mt-2 text-xl font-semibold tabular-nums text-slate-900">{averageMembers}</p>
              </div>
            </div>
            <p className="text-xs leading-5 text-slate-500">
              El promedio se calcula dividiendo los miembros registrados entre el total de organizaciones.
            </p>
          </div>
        </Card>
      </section>

      <section aria-labelledby="quick-actions-heading" className="space-y-3">
        <div>
          <h2 id="quick-actions-heading" className="text-base font-semibold text-slate-900">Accesos directos</h2>
          <p className="mt-1 text-sm text-slate-500">Accede a las tareas habituales de administración.</p>
        </div>
        <div className="grid gap-3 md:grid-cols-2">
          <QuickAction
            to="/admin/organizations"
            title="Gestionar organizaciones"
            description="Consultar, revisar y administrar organizaciones registradas."
            icon={<Icon name="building" className="h-5 w-5" />}
          />
          <QuickAction
            to="/admin/organizations?action=create"
            title="Crear organización"
            description="Registrar una nueva organización en la plataforma."
            icon={<Icon name="users" className="h-5 w-5" />}
          />
        </div>
      </section>
    </main>
  )
}
