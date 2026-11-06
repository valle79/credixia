import { useState } from 'react'
import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { customersApi } from '../api/customers.api'
import type { CustomerSummary } from '../api/types'
import { Card } from '../components/ui/Card'
import { Button } from '../components/ui/Button'
import { Input } from '../components/ui/Input'
import { Badge } from '../components/ui/Badge'
import { Modal } from '../components/ui/Modal'
import { ToastContainer } from '../components/ui/ToastContainer'
import { getErrorMessage } from '../api/client'
import { useAuth } from '../hooks/useAuth'
import { useDebouncedValue } from '../hooks/useDebouncedValue'
import { useToast } from '../hooks/useToast'
import { formatDate, formatDocumentNumber, getStatusVariant, getStatusLabel } from '../lib/format'

const PAGE_SIZE = 20

export function CustomersPage() {
  const { tenant } = useAuth()
  const queryClient = useQueryClient()
  const toast = useToast()
  const [search, setSearch] = useState('')
  const [pageNumber, setPageNumber] = useState(1)
  const [customerToDelete, setCustomerToDelete] = useState<CustomerSummary | null>(null)
  const debouncedSearch = useDebouncedValue(search)

  const { data, isLoading, isFetching } = useQuery({
    queryKey: ['customers', tenant?.id, debouncedSearch, pageNumber],
    queryFn: () =>
      customersApi.list({
        search: debouncedSearch || undefined,
        pageNumber,
        pageSize: PAGE_SIZE,
      }),
    placeholderData: keepPreviousData,
    enabled: !!tenant?.id,
  })

  const customers = data?.items ?? []
  const totalPages = data?.totalPages ?? 0

  const deleteMutation = useMutation({
    mutationFn: (id: string) => customersApi.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['customers'] })
      toast.success('Cliente eliminado', 'El cliente se eliminó correctamente')
      setCustomerToDelete(null)
    },
    onError: (error) => {
      toast.error('No se pudo eliminar', getErrorMessage(error))
    },
  })

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-slate-900">Clientes</h1>
          <p className="mt-1 text-sm text-slate-600">Gestiona la información de tus clientes</p>
        </div>
        <Link to="/customers/new">
          <Button>
            <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
            </svg>
            Nuevo cliente
          </Button>
        </Link>
      </div>

      <Card>
        <div className="space-y-4">
          <Input
            placeholder="Buscar por nombre, documento o teléfono..."
            value={search}
            onChange={(e) => {
              setSearch(e.target.value)
              setPageNumber(1)
            }}
          />

          {isLoading ? (
            <div className="py-12 text-center text-sm text-slate-500">Cargando...</div>
          ) : customers.length > 0 ? (
            <>
              <div className={`overflow-x-auto ${isFetching ? 'opacity-60' : ''}`}>
                <table className="w-full">
                  <thead className="border-b border-slate-200 bg-slate-50">
                    <tr>
                      <th className="px-4 py-3 text-left text-xs font-medium uppercase text-slate-600">Cliente</th>
                      <th className="px-4 py-3 text-left text-xs font-medium uppercase text-slate-600">Documento</th>
                      <th className="px-4 py-3 text-left text-xs font-medium uppercase text-slate-600">Teléfono</th>
                      <th className="px-4 py-3 text-left text-xs font-medium uppercase text-slate-600">Estado</th>
                      <th className="px-4 py-3 text-left text-xs font-medium uppercase text-slate-600">Registrado</th>
                      <th className="px-4 py-3 text-right text-xs font-medium uppercase text-slate-600">Acciones</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-100">
                    {customers.map((customer) => (
                      <tr key={customer.id} className="hover:bg-slate-50">
                        <td className="px-4 py-3">
                          <div className="flex items-center gap-3">
                            <div className="flex h-9 w-9 shrink-0 items-center justify-center overflow-hidden rounded-full bg-slate-200 text-xs font-semibold text-slate-500">
                              {customer.photoUrl ? (
                                <img
                                  src={customer.photoUrl}
                                  alt={`Foto de ${customer.fullName}`}
                                  className="h-full w-full object-cover"
                                />
                              ) : (
                                customer.fullName.charAt(0).toUpperCase()
                              )}
                            </div>
                            <div>
                              <div className="font-medium text-slate-900">{customer.fullName}</div>
                              <div className="text-xs text-slate-500">{getStatusLabel(customer.type)}</div>
                            </div>
                          </div>
                        </td>
                        <td className="px-4 py-3">
                          <div className="text-sm">
                            <div className="font-medium text-slate-700">{customer.documentType}</div>
                            <div className="text-slate-500">{formatDocumentNumber(customer.documentType, customer.documentNumber)}</div>
                          </div>
                        </td>
                        <td className="px-4 py-3 text-sm text-slate-600">{customer.phone || '-'}</td>
                        <td className="px-4 py-3">
                          <Badge variant={getStatusVariant(customer.status)}>
                            {getStatusLabel(customer.status)}
                          </Badge>
                        </td>
                        <td className="px-4 py-3 text-sm text-slate-600">
                          {formatDate(customer.createdAt)}
                        </td>
                        <td className="px-4 py-3">
                          <div className="flex justify-end gap-2">
                            <Link to={`/customers/${customer.id}`}>
                              <Button variant="ghost" size="sm">Ver detalles</Button>
                            </Link>
                            <Button
                              variant="ghost"
                              size="sm"
                              className="text-red-600 hover:bg-red-50 hover:text-red-700"
                              onClick={() => setCustomerToDelete(customer)}
                            >
                              Eliminar
                            </Button>
                          </div>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>

              <div className="flex items-center justify-between border-t border-slate-200 pt-4">
                <p className="text-sm text-slate-600">
                  {data?.totalCount ?? 0} clientes · página {pageNumber} de {totalPages || 1}
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
              <p className="text-sm text-slate-500">No se encontraron clientes</p>
              <Link to="/customers/new">
                <Button variant="secondary" size="sm" className="mt-4">
                  Crear primer cliente
                </Button>
              </Link>
            </div>
          )}
        </div>
      </Card>

      <Modal
        isOpen={!!customerToDelete}
        onClose={() => setCustomerToDelete(null)}
        title="Eliminar cliente"
        size="sm"
      >
        <div className="space-y-4">
          <p className="text-sm text-slate-600">
            ¿Estás seguro de que deseas eliminar a{' '}
            <span className="font-semibold text-slate-900">{customerToDelete?.fullName}</span>?
            Esta acción no se puede deshacer.
          </p>
          <div className="rounded-lg border border-amber-200 bg-amber-50 p-3">
            <p className="text-xs text-amber-800">
              Solo se pueden eliminar clientes sin préstamos asociados. Si el cliente tiene
              préstamos, suspéndelo o bloquéalo en su lugar.
            </p>
          </div>
          <div className="flex justify-end gap-3">
            <Button variant="ghost" onClick={() => setCustomerToDelete(null)}>
              Cancelar
            </Button>
            <Button
              variant="danger"
              isLoading={deleteMutation.isPending}
              onClick={() => customerToDelete && deleteMutation.mutate(customerToDelete.id)}
            >
              Eliminar cliente
            </Button>
          </div>
        </div>
      </Modal>

      <ToastContainer toasts={toast.toasts} onClose={toast.close} />
    </div>
  )
}
