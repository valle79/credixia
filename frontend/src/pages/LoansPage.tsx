import { useState } from 'react'
import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { Trash2 } from 'lucide-react'
import { loansApi } from '../api/loans.api'
import type { LoanSummary } from '../api/types'
import { getErrorMessage } from '../api/client'
import { Card } from '../components/ui/Card'
import { Button } from '../components/ui/Button'
import { Input } from '../components/ui/Input'
import { Select } from '../components/ui/Select'
import { Badge } from '../components/ui/Badge'
import { Modal } from '../components/ui/Modal'
import { CustomerAvatar } from '../components/ui/CustomerAvatar'
import { ToastContainer } from '../components/ui/ToastContainer'
import { useAuth } from '../hooks/useAuth'
import { useDebouncedValue } from '../hooks/useDebouncedValue'
import { useToast } from '../hooks/useToast'
import { formatCurrency, formatDate, getStatusVariant, getStatusLabel } from '../lib/format'

const PAGE_SIZE = 20
const DELETABLE_LOAN_STATUSES = new Set(['Pending', 'Approved', 'Rejected', 'Cancelled'])

export function LoansPage() {
  const { tenant, hasPermission } = useAuth()
  const queryClient = useQueryClient()
  const toast = useToast()
  const [status, setStatus] = useState<string>('')
  const [search, setSearch] = useState('')
  const [pageNumber, setPageNumber] = useState(1)
  const [loanToDelete, setLoanToDelete] = useState<LoanSummary | null>(null)
  const debouncedSearch = useDebouncedValue(search)
  const canManageLoans = hasPermission('loans.manage')

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

  const deleteMutation = useMutation({
    mutationFn: (loanId: string) => loansApi.delete(loanId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['loans'] })
      queryClient.invalidateQueries({ queryKey: ['dashboard'] })
      setLoanToDelete(null)
      toast.success('Préstamo eliminado', 'El préstamo se eliminó correctamente.')
    },
    onError: (error) => {
      toast.error('No se pudo eliminar el préstamo', getErrorMessage(error))
    },
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
                        <td className="px-4 py-3">
                          <div className="flex items-center gap-3">
                            <CustomerAvatar
                              name={loan.customerName}
                              photoUrl={loan.customerPhotoUrl}
                              size="md"
                            />
                            <span className="font-medium text-slate-900">{loan.customerName}</span>
                          </div>
                        </td>
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
                        <td className="px-4 py-3">
                          <div className="flex justify-end gap-1">
                            <Link to={`/loans/${loan.id}`}>
                              <Button variant="ghost" size="sm">Ver</Button>
                            </Link>
                            {canManageLoans && DELETABLE_LOAN_STATUSES.has(loan.status) && (
                              <Button
                                variant="ghost"
                                size="sm"
                                className="text-red-600 hover:bg-red-50 hover:text-red-700"
                                onClick={() => setLoanToDelete(loan)}
                              >
                                <Trash2 className="h-4 w-4" />
                                Eliminar
                              </Button>
                            )}
                          </div>
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

      <Modal
        isOpen={!!loanToDelete}
        onClose={() => setLoanToDelete(null)}
        title="Eliminar préstamo"
        size="sm"
      >
        <div className="space-y-4">
          {loanToDelete && (
            <div className="flex items-center gap-3 rounded-lg border border-slate-200 bg-slate-50 p-3">
              <CustomerAvatar
                name={loanToDelete.customerName}
                photoUrl={loanToDelete.customerPhotoUrl}
                size="lg"
              />
              <div className="min-w-0">
                <p className="truncate font-semibold text-slate-900">
                  {loanToDelete.customerName}
                </p>
                <p className="font-mono text-xs text-slate-500">{loanToDelete.loanNumber}</p>
                <p className="mt-0.5 text-xs text-slate-600">
                  {formatCurrency(loanToDelete.principalAmount, loanToDelete.currency)} ·{' '}
                  {getStatusLabel(loanToDelete.status)}
                </p>
              </div>
            </div>
          )}

          <p className="text-sm text-slate-600">
            ¿Estás seguro de que deseas eliminar este préstamo? Esta acción no se puede
            deshacer.
          </p>

          <div className="rounded-lg border border-amber-200 bg-amber-50 p-3">
            <p className="text-xs text-amber-800">
              Solo se pueden eliminar préstamos pendientes, aprobados sin desembolso,
              rechazados o cancelados. Los préstamos desembolsados conservan su historial
              financiero.
            </p>
          </div>

          <div className="flex justify-end gap-3">
            <Button variant="ghost" onClick={() => setLoanToDelete(null)}>
              Cancelar
            </Button>
            <Button
              variant="danger"
              isLoading={deleteMutation.isPending}
              onClick={() => loanToDelete && deleteMutation.mutate(loanToDelete.id)}
            >
              <Trash2 className="h-4 w-4" />
              Eliminar préstamo
            </Button>
          </div>
        </div>
      </Modal>

      <ToastContainer toasts={toast.toasts} onClose={toast.close} />
    </div>
  )
}
