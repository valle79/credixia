import { useState } from 'react';
import { Link } from 'react-router-dom';
import { keepPreviousData, useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { collectionsApi, type RecordCollectionActionRequest } from '../api/collections.api';
import type { LoanSummaryDto } from '../api/types';
import { useAuth } from '../hooks/useAuth';
import { Button } from '../components/ui/Button';
import { Input } from '../components/ui/Input';
import { Select } from '../components/ui/Select';
import { Badge } from '../components/ui/Badge';
import { Card } from '../components/ui/Card';
import { Modal } from '../components/ui/Modal';
import { formatCurrency, formatDate } from '../lib/format';
import { useToast } from '../hooks/useToast';
import { ToastContainer } from '../components/ui/ToastContainer';
import { getErrorMessage } from '../api/client';

export function CollectionsPage() {
  const queryClient = useQueryClient();
  const toast = useToast();
  const { tenant } = useAuth();
  const [selectedLoan, setSelectedLoan] = useState<string | null>(null);
  const [showActionModal, setShowActionModal] = useState(false);
  const [pageNumber, setPageNumber] = useState(1);

  const { data: overdueLoans, isLoading, isFetching } = useQuery({
    queryKey: ['overdueLoans', tenant?.id, pageNumber],
    queryFn: () => collectionsApi.getOverdueLoans({ pageNumber, pageSize: 20 }),
    placeholderData: keepPreviousData,
    enabled: !!tenant?.id,
  });

  const { data: actions } = useQuery({
    queryKey: ['collectionActions', tenant?.id, selectedLoan],
    queryFn: () => collectionsApi.listActions({ loanId: selectedLoan! }),
    enabled: !!selectedLoan,
  });

  const recordActionMutation = useMutation({
    mutationFn: (data: RecordCollectionActionRequest) => collectionsApi.recordAction(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['collectionActions'] });
      queryClient.invalidateQueries({ queryKey: ['overdueLoans'] });
      setShowActionModal(false);
      toast.success('Gestión registrada', 'La acción de cobranza ha sido registrada exitosamente');
    },
    onError: (error) => {
      toast.error('Error al registrar gestión', getErrorMessage(error));
    },
  });

  const handleRecordAction = (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    const formData = new FormData(e.currentTarget);
    const followUpDateStr = formData.get('followUpDate') as string;
    
    recordActionMutation.mutate({
      loanId: selectedLoan!,
      actionType: String(formData.get('actionType') ?? ''),
      notes: formData.get('notes') ? String(formData.get('notes')) : undefined,
      followUpDate: followUpDateStr ? new Date(followUpDateStr).toISOString() : undefined,
    });
  };

  const getOverdueDays = (loan: LoanSummaryDto) => {
    if (!loan.nextPaymentDueDate) return 0;
    const dueDate = new Date(loan.nextPaymentDueDate);
    const today = new Date();
    const diffTime = today.getTime() - dueDate.getTime();
    return Math.floor(diffTime / (1000 * 60 * 60 * 24));
  };

  const getOverdueBadge = (days: number) => {
    if (days <= 7) return <Badge variant="warning">{days}d</Badge>;
    if (days <= 30) return <Badge variant="danger">{days}d</Badge>;
    return <Badge variant="danger">{days}d - Crítico</Badge>;
  };

  if (isLoading) {
    return <div className="p-6">Cargando...</div>;
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <h1 className="text-2xl font-semibold">Gestión de Cobranza</h1>
        <p className="text-gray-600">Seguimiento de préstamos en mora</p>
      </div>

      {/* Summary Cards */}
      {overdueLoans && (
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
          <Card>
            <p className="text-sm text-gray-600 mb-1">Préstamos en Mora</p>
            <p className="text-3xl font-bold">{overdueLoans.totalCount}</p>
          </Card>
          <Card>
            <p className="text-sm text-gray-600 mb-1">Monto Total en Mora</p>
            <p className="text-3xl font-bold text-red-600">
              {formatCurrency(overdueLoans.totalOverdueAmount)}
            </p>
          </Card>
          <Card>
            <p className="text-sm text-gray-600 mb-1">Promedio Días Mora</p>
            <p className="text-3xl font-bold">
              {overdueLoans.items.length > 0
                ? Math.round(
                    overdueLoans.items.reduce((sum, loan) => sum + getOverdueDays(loan), 0) /
                      overdueLoans.items.length
                  )
                : 0}d
            </p>
          </Card>
        </div>
      )}

      {/* Overdue Loans Table */}
      <Card>
        <h2 className="text-lg font-semibold mb-4">Préstamos Vencidos</h2>
        {overdueLoans && overdueLoans.items.length > 0 ? (
          <>
          <div className={`overflow-x-auto ${isFetching ? 'opacity-60' : ''}`}>
            <table className="w-full">
              <thead className="bg-gray-50">
                <tr>
                  <th className="px-4 py-2 text-left text-sm font-medium">Préstamo</th>
                  <th className="px-4 py-2 text-left text-sm font-medium">Cliente</th>
                  <th className="px-4 py-2 text-left text-sm font-medium">Teléfono</th>
                  <th className="px-4 py-2 text-right text-sm font-medium">Monto Vencido</th>
                  <th className="px-4 py-2 text-left text-sm font-medium">Días Mora</th>
                  <th className="px-4 py-2 text-left text-sm font-medium">Última Acción</th>
                  <th className="px-4 py-2"></th>
                </tr>
              </thead>
              <tbody className="divide-y">
                {overdueLoans.items.map((loan) => {
                  const overdueDays = getOverdueDays(loan);
                  return (
                    <tr key={loan.id} className="hover:bg-gray-50">
                      <td className="px-4 py-3">
                        <p className="text-sm font-mono font-medium">{loan.loanNumber}</p>
                        <p className="text-xs text-gray-600">{loan.productName}</p>
                      </td>
                      <td className="px-4 py-3 text-sm">{loan.customerName}</td>
                      <td className="px-4 py-3 text-sm">
                        <a
                          href={`tel:${loan.customerPhone}`}
                          className="text-blue-600 hover:underline"
                        >
                          {loan.customerPhone}
                        </a>
                      </td>
                      <td className="px-4 py-3 text-sm font-medium text-right text-red-600">
                        {formatCurrency(loan.totalDue || 0)}
                      </td>
                      <td className="px-4 py-3">{getOverdueBadge(overdueDays)}</td>
                      <td className="px-4 py-3 text-sm text-gray-600">
                        {loan.lastCollectionDate
                          ? formatDate(loan.lastCollectionDate)
                          : 'Sin registro'}
                      </td>
                      <td className="px-4 py-3">
                        <div className="flex gap-2">
                          <Button
                            variant="secondary"
                            size="sm"
                            onClick={() => {
                              setSelectedLoan(loan.id);
                              setShowActionModal(true);
                            }}
                          >
                            Registrar
                          </Button>
                          <Link to={`/loans/${loan.id}`}>
                            <Button variant="ghost" size="sm">
                              Ver
                            </Button>
                          </Link>
                        </div>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
          <div className="mt-4 flex items-center justify-between border-t border-gray-200 pt-4">
            <p className="text-sm text-gray-600">
              {overdueLoans.totalCount} préstamos · página {overdueLoans.pageNumber} de{' '}
              {overdueLoans.totalPages || 1}
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
                disabled={pageNumber >= overdueLoans.totalPages || isFetching}
                onClick={() => setPageNumber((page) => page + 1)}
              >
                Siguiente
              </Button>
            </div>
          </div>
          </>
        ) : (
          <div className="text-center py-12">
            <p className="text-gray-600">No hay préstamos en mora</p>
          </div>
        )}
      </Card>

      {/* Collection Actions History */}
      {selectedLoan && actions && (
        <Card>
          <h2 className="text-lg font-semibold mb-4">Historial de Gestión</h2>
          {actions.items.length > 0 ? (
            <div className="space-y-3">
              {actions.items.map((action) => (
                <div key={action.id} className="p-4 border rounded-lg">
                  <div className="flex justify-between items-start mb-2">
                    <div>
                      <Badge variant="default">{action.actionType}</Badge>
                      <p className="text-sm text-gray-600 mt-1">
                        {formatDate(action.actionDate)} - {action.performedBy}
                      </p>
                    </div>
                    {action.followUpDate && (
                      <div className="text-right">
                        <p className="text-xs text-gray-600">Seguimiento</p>
                        <p className="text-sm font-medium">{formatDate(action.followUpDate)}</p>
                      </div>
                    )}
                  </div>
                  {action.notes && (
                    <p className="text-sm text-gray-700 mt-2">{action.notes}</p>
                  )}
                </div>
              ))}
            </div>
          ) : (
            <p className="text-center text-gray-600 py-4">No hay acciones registradas</p>
          )}
        </Card>
      )}

      {/* Record Action Modal */}
      <Modal
        isOpen={showActionModal}
        onClose={() => setShowActionModal(false)}
        title="Registrar Gestión de Cobranza"
      >
        <form onSubmit={handleRecordAction} className="space-y-4">
          <Select label="Tipo de Acción" name="actionType" required>
            <option value="">Seleccionar...</option>
            <option value="PhoneCall">Llamada Telefónica</option>
            <option value="Visit">Visita en Campo</option>
            <option value="Message">Mensaje SMS/WhatsApp</option>
            <option value="Promise">Promesa de Pago</option>
            <option value="LegalAction">Acción Legal</option>
          </Select>
          <div>
            <label className="block text-sm font-medium mb-1">Notas</label>
            <textarea
              name="notes"
              rows={4}
              className="w-full px-3 py-2 border rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
              placeholder="Detalles de la gestión..."
            />
          </div>
          <Input
            label="Fecha de Seguimiento (opcional)"
            name="followUpDate"
            type="date"
            helperText="Cuándo hacer el próximo contacto"
          />
          <div className="flex gap-3">
            <Button type="submit" disabled={recordActionMutation.isPending}>
              {recordActionMutation.isPending ? 'Guardando...' : 'Guardar'}
            </Button>
            <Button
              type="button"
              variant="secondary"
              onClick={() => setShowActionModal(false)}
            >
              Cancelar
            </Button>
          </div>
        </form>
      </Modal>
      <ToastContainer toasts={toast.toasts} onClose={toast.close} />
    </div>
  );
}
