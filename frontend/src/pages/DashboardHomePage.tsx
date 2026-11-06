import { useQuery } from '@tanstack/react-query'
import { dashboardApi } from '../api/dashboard.api'
import { Card } from '../components/ui/Card'
import { formatCurrency } from '../lib/format'
import { useAuth } from '../hooks/useAuth'

export function DashboardHomePage() {
  const { tenant } = useAuth()

  const { data: summary } = useQuery({
    queryKey: ['dashboard', 'summary', tenant?.id],
    queryFn: () => dashboardApi.getSummary(),
    enabled: !!tenant?.id,
  })

  const pendingLoans = summary?.pendingLoans ?? []
  const hasNoData =
    !!summary && summary.activeLoanCount === 0 && summary.pendingLoanCount === 0

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-slate-900">
          ¡Bienvenido a {tenant?.name || 'Portal Credixia'}!
        </h1>
        <p className="mt-1 text-sm text-slate-600">
          Resumen de tu operación de préstamos
        </p>
      </div>

      {/* Métricas principales */}
      <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-4">
        <Card className="p-6">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium text-slate-600">Cartera Activa</p>
              <p className="mt-2 text-2xl font-bold text-slate-900">
                {formatCurrency(summary?.activePortfolio ?? 0, 'PEN')}
              </p>
            </div>
            <div className="rounded-full bg-brand-100 p-3">
              <svg className="h-6 w-6 text-brand-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
            </div>
          </div>
          <p className="mt-2 text-xs text-slate-500">{summary?.activeLoanCount ?? 0} préstamos activos</p>
        </Card>

        <Card className="p-6">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium text-slate-600">Capital Prestado</p>
              <p className="mt-2 text-2xl font-bold text-slate-900">
                {formatCurrency(summary?.disbursedThisMonth ?? 0, 'PEN')}
              </p>
            </div>
            <div className="rounded-full bg-blue-100 p-3">
              <svg className="h-6 w-6 text-blue-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M17 9V7a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2m2 4h10a2 2 0 002-2v-6a2 2 0 00-2-2H9a2 2 0 00-2 2v6a2 2 0 002 2zm7-5a2 2 0 11-4 0 2 2 0 014 0z" />
              </svg>
            </div>
          </div>
          <p className="mt-2 text-xs text-slate-500">Desembolsado este mes</p>
        </Card>

        <Card className="p-6">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium text-slate-600">Intereses Ganados</p>
              <p className="mt-2 text-2xl font-bold text-slate-900">
                {formatCurrency(summary?.activeInterest ?? 0, 'PEN')}
              </p>
            </div>
            <div className="rounded-full bg-emerald-100 p-3">
              <svg className="h-6 w-6 text-emerald-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 7h8m0 0v8m0-8l-8 8-4-4-6 6" />
              </svg>
            </div>
          </div>
          <p className="mt-2 text-xs text-slate-500">En cartera activa</p>
        </Card>

        <Card className="p-6">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium text-slate-600">Clientes Activos</p>
              <p className="mt-2 text-2xl font-bold text-slate-900">{summary?.activeCustomerCount ?? 0}</p>
            </div>
            <div className="rounded-full bg-purple-100 p-3">
              <svg className="h-6 w-6 text-purple-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z" />
              </svg>
            </div>
          </div>
          <p className="mt-2 text-xs text-slate-500">Con préstamos vigentes</p>
        </Card>
      </div>

      {/* Préstamos pendientes de aprobación */}
      {pendingLoans.length > 0 && (
        <Card title="Préstamos pendientes de aprobación" subtitle={`${summary?.pendingLoanCount ?? pendingLoans.length} solicitudes`}>
          <div className="space-y-3">
            {pendingLoans.map((loan) => (
              <div key={loan.id} className="flex items-center justify-between rounded-lg border border-slate-200 p-4">
                <div>
                  <p className="font-medium text-slate-900">{loan.customerName}</p>
                  <p className="text-sm text-slate-500">
                    {loan.loanNumber} · {formatCurrency(loan.principalAmount, loan.currency)}
                  </p>
                </div>
                <a
                  href={`/loans/${loan.id}`}
                  className="text-sm font-medium text-brand-600 hover:text-brand-700"
                >
                  Revisar →
                </a>
              </div>
            ))}
          </div>
        </Card>
      )}

      {/* Mensaje si no hay datos */}
      {hasNoData && (
        <Card>
          <div className="py-12 text-center">
            <div className="mx-auto flex h-16 w-16 items-center justify-center rounded-full bg-slate-100">
              <svg className="h-8 w-8 text-slate-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
              </svg>
            </div>
            <h3 className="mt-4 text-lg font-medium text-slate-900">¡Comienza a gestionar préstamos!</h3>
            <p className="mt-2 text-sm text-slate-600">
              Crea tu primer cliente y otorga tu primer préstamo
            </p>
            <div className="mt-6 flex justify-center gap-3">
              <a href="/customers/new" className="rounded-lg bg-brand-600 px-4 py-2 text-sm font-medium text-white hover:bg-brand-700">
                Crear cliente
              </a>
              <a href="/loan-products" className="rounded-lg bg-slate-100 px-4 py-2 text-sm font-medium text-slate-700 hover:bg-slate-200">
                Ver productos
              </a>
            </div>
          </div>
        </Card>
      )}
    </div>
  )
}
