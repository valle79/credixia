import { useState } from 'react'
import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { loansApi } from '../api/loans.api'
import { Card } from '../components/ui/Card'
import { Button } from '../components/ui/Button'
import { Input } from '../components/ui/Input'
import { Select } from '../components/ui/Select'
import { Badge } from '../components/ui/Badge'
import { useAuth } from '../hooks/useAuth'
import { useDebouncedValue } from '../hooks/useDebouncedValue'
import { formatCurrency, formatDate, getStatusVariant, getStatusLabel } from '../lib/format'

const PAGE_SIZE = 20

export function LoansPage() {
  const { tenant } = useAuth()
  const [status, setStatus] = useState<string>('')
  const [search, setSearch] = useState('')
  const [pageNumber, setPageNumber] = useState(1)
  const debouncedSearch = useDebouncedValue(search)

  const { data, isLoading, isFetching } = useQuery({
    queryKey: ['loans', tenant?.id, status, debouncedSearch, pageNumber],
    queryFn: () =>
      loansApi.list({
        status: status || undefined,
        search: debouncedSearch || undefined,
        pageNumber,
        pageSize: PAGE_SIZE,
      }),
    placeholderData: keepPreviousData,
    enabled: !!tenant?.id,
  })

  const loans = data?.items ?? []
  const totalPages = data?.totalPages ?? 0

  const statusOptions = [
    { value: '', label: 'Todos los estados' },
    { value: 'Pending', label: 'Pendiente' },
    { value: 'Approved', label: 'Aprobado' },
    { value: 'Active', label: 'Activo' },
    { value: 'PaidOff', label: 'Pagado' },
    { value: 'Rejected', label: 'Rechazado' },
  ]

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-slate-900">Préstamos</h1>
          <p className="mt-1 text-sm text-slate-600">Gestiona todos los préstamos de tu cartera</p>
        </div>
        <Link to="/loans/new">
          <Button>
            <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
            </svg>
            Nuevo préstamo
          </Button>
        </Link>
      </div>

      <Card>
        <div className="space-y-4">
          <div className="flex flex-wrap gap-4">
            <div className="w-full sm:w-72">
              <Input
                placeholder="Buscar por préstamo, cliente o documento..."
                value={search}
                onChange={(e) => {
                  setSearch(e.target.value)
                  setPageNumber(1)
                }}
              />
            </div>
            <div className="w-full sm:w-64">
              <Select
                options={statusOptions}
                value={status}
                onChange={(e) => {
                  setStatus(e.target.value)
                  setPageNumber(1)
                }}
              />
            </div>
          </div>

          {isLoading ? (
            <div className="py-12 text-center text-sm text-slate-500">Cargando...</div>
          ) : loans.length > 0 ? (
            <>
              <div className={`overflow-x-auto ${isFetching ? 'opacity-60' : ''}`}>
                <table className="w-full">
                  <thead className="border-b border-slate-200 bg-slate-50">
                    <tr>
                      <th className="px-4 py-3 text-left text-xs font-medium uppercase text-slate-600">Préstamo</th>
                      <th className="px-4 py-3 text-left text-xs font-medium uppercase text-slate-600">Cliente</th>
                      <th className="px-4 py-3 text-right text-xs font-medium uppercase text-slate-600">Monto</th>
                      <th className="px-4 py-3 text-right text-xs font-medium uppercase text-slate-600">Total</th>
                      <th className="px-4 py-3 text-right text-xs font-medium uppercase text-slate-600">Saldo</th>
                      <th className="px-4 py-3 text-center text-xs font-medium uppercase text-slate-600">Estado</th>
                      <th className="px-4 py-3 text-left text-xs font-medium uppercase text-slate-600">Desembolso</th>
                      <th className="px-4 py-3 text-right text-xs font-medium uppercase text-slate-600">Acciones</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-100">
                    {loans.map((loan) => (
                      <tr key={loan.id} className="hover:bg-slate-50">
                        <td className="px-4 py-3">
                          <div className="font-mono text-sm font-medium text-slate-900">{loan.loanNumber}</div>
                          <div className="text-xs text-slate-500">{loan.interestRate}% interés</div>
                        </td>
                        <td className="px-4 py-3 text-sm text-slate-700">{loan.customerName}</td>
                        <td className="px-4 py-3 text-right font-medium text-slate-900">
                          {formatCurrency(loan.principalAmount, loan.currency)}
                        </td>
                        <td className="px-4 py-3 text-right text-sm text-slate-700">
                          {formatCurrency(loan.totalAmount, loan.currency)}
                        </td>
                        <td className="px-4 py-3 text-right font-medium text-slate-900">
                          {formatCurrency(loan.balanceAmount, loan.currency)}
                        </td>
                        <td className="px-4 py-3 text-center">
                          <Badge variant={getStatusVariant(loan.status)}>
                            {getStatusLabel(loan.status)}
                          </Badge>
                        </td>
                        <td className="px-4 py-3 text-sm text-slate-600">
                          {formatDate(loan.disbursementDate)}
                        </td>
                        <td className="px-4 py-3 text-right">
                          <Link to={`/loans/${loan.id}`}>
                            <Button variant="ghost" size="sm">Ver</Button>
                          </Link>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>

              <div className="flex items-center justify-between border-t border-slate-200 pt-4">
                <p className="text-sm text-slate-600">
                  {data?.totalCount ?? 0} préstamos · página {pageNumber} de {totalPages || 1}
                </p>
                <div className="flex gap-2">
                  <Button
                    variant="secondary"
                    size="sm"
                    disabled={pageNumber <= 1 || isFetching}
                    onClick={() => setPageNumber((page) => Math.max(1, page - 1))}
                  >
                    Anterior
                  </Button>
                  <Button
                    variant="secondary"
                    size="sm"
                    disabled={pageNumber >= totalPages || isFetching}
                    onClick={() => setPageNumber((page) => page + 1)}
                  >
                    Siguiente
                  </Button>
                </div>
              </div>
            </>
          ) : (
            <div className="py-12 text-center">
              <p className="text-sm text-slate-500">No se encontraron préstamos</p>
              <Link to="/loans/new">
                <Button variant="secondary" size="sm" className="mt-4">
                  Crear primer préstamo
                </Button>
              </Link>
            </div>
          )}
        </div>
      </Card>
    </div>
  )
}
