import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { cashApi } from '../api/cash.api';
import { Button } from '../components/ui/Button';
import { Input } from '../components/ui/Input';
import { Badge } from '../components/ui/Badge';
import { Card } from '../components/ui/Card';
import { Modal } from '../components/ui/Modal';
import { ToastContainer } from '../components/ui/ToastContainer';
import { useToast } from '../hooks/useToast';
import { useAuth } from '../hooks/useAuth';
import { formatCurrency, formatDate } from '../lib/format';
import { getErrorMessage } from '../api/client';

export function CashPage() {
  const queryClient = useQueryClient();
  const toast = useToast();
  const { tenant } = useAuth();
  const [showOpenModal, setShowOpenModal] = useState(false);
  const [showCloseModal, setShowCloseModal] = useState(false);
  const [showHistory, setShowHistory] = useState(false);

  const { data: currentSession, isLoading } = useQuery({
    queryKey: ['cashSession', tenant?.id, 'current'],
    queryFn: () => cashApi.getCurrent(),
    enabled: !!tenant?.id,
    refetchInterval: 30000,
  });

  const { data: history } = useQuery({
    queryKey: ['cashSessions', tenant?.id, 'history'],
    queryFn: () => cashApi.list({ status: 'Closed', pageSize: 10 }),
    enabled: showHistory && !!tenant?.id,
  });

  const { data: transactions } = useQuery({
    queryKey: ['cashTransactions', tenant?.id, currentSession?.id],
    queryFn: () => cashApi.getTransactions(currentSession!.id),
    enabled: !!tenant?.id && !!currentSession,
  });

  const openMutation = useMutation({
    mutationFn: (data: { openingBalance: number; notes?: string | undefined }) =>
      cashApi.open(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['cashSession'] });
      setShowOpenModal(false);
      toast.success('¡Caja abierta!', 'La sesión de caja se abrió correctamente');
    },
    onError: (error) => {
      toast.error('Error al abrir caja', getErrorMessage(error));
    },
  });

  const closeMutation = useMutation({
    mutationFn: (data: { closingBalance: number; notes?: string | undefined }) =>
      cashApi.close(currentSession!.id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['cashSession'] });
      setShowCloseModal(false);
      toast.success('¡Caja cerrada!', 'La sesión de caja se cerró correctamente');
    },
    onError: (error) => {
      toast.error('Error al cerrar caja', getErrorMessage(error));
    },
  });

  const handleOpenSession = (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    const formData = new FormData(e.currentTarget);
    openMutation.mutate({
      openingBalance: Number(formData.get('openingBalance')),
      notes: formData.get('notes') as string || undefined,
    });
  };

  const handleCloseSession = (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    const formData = new FormData(e.currentTarget);
    closeMutation.mutate({
      closingBalance: Number(formData.get('closingBalance')),
      notes: formData.get('notes') as string || undefined,
    });
  };

  const getTransactionTypeLabel = (type: string) => {
    const labels: Record<string, string> = {
      LoanPayment: 'Pago de Préstamo',
      LoanDisbursement: 'Desembolso de Préstamo',
      Expense: 'Gasto',
      Deposit: 'Depósito',
      Withdrawal: 'Retiro',
      Adjustment: 'Ajuste',
    };
    return labels[type] || type;
  };

  if (isLoading) {
    return <div className="p-6">Cargando...</div>;
  }

  return (
    <>
      <ToastContainer toasts={toast.toasts} onClose={toast.close} />
      <div className="space-y-6">
        {/* Header */}
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-2xl font-bold text-slate-900">Gestión de Caja</h1>
            <p className="mt-1 text-sm text-slate-600">Controla los movimientos de efectivo</p>
          </div>
          {currentSession ? (
            <Button variant="danger" onClick={() => setShowCloseModal(true)}>
              Cerrar Caja
            </Button>
          ) : (
            <Button onClick={() => setShowOpenModal(true)}>Abrir Caja</Button>
          )}
        </div>

        {/* Current Session */}
        {currentSession ? (
          <div className="space-y-6">
            {/* Session Summary */}
            <Card>
              <div className="flex items-start justify-between mb-6">
                <div>
                  <h2 className="text-lg font-semibold">Sesión Actual</h2>
                  <p className="text-sm text-gray-600">#{currentSession.sessionNumber}</p>
                  <p className="text-sm text-gray-600">
                    Abierta: {formatDate(currentSession.openedAt)} por {currentSession.openedBy}
                  </p>
                </div>
                <Badge variant="success">Abierta</Badge>
              </div>

              <div className="grid grid-cols-2 md:grid-cols-4 gap-6">
                <div>
                  <p className="text-sm text-gray-600 mb-1">Saldo Inicial</p>
                  <p className="text-2xl font-bold">{formatCurrency(currentSession.openingBalance)}</p>
                </div>
                <div>
                  <p className="text-sm text-gray-600 mb-1">Ingresos</p>
                  <p className="text-2xl font-bold text-green-600">
                    +{formatCurrency(currentSession.totalIn)}
                  </p>
                </div>
                <div>
                  <p className="text-sm text-gray-600 mb-1">Egresos</p>
                  <p className="text-2xl font-bold text-red-600">
                    -{formatCurrency(currentSession.totalOut)}
                  </p>
                </div>
                <div>
                  <p className="text-sm text-gray-600 mb-1">Saldo Esperado</p>
                  <p className="text-2xl font-bold text-blue-600">
                    {formatCurrency(currentSession.expectedBalance)}
                  </p>
                </div>
              </div>

              {currentSession.notes && (
                <div className="mt-4 p-3 bg-gray-50 rounded">
                  <p className="text-sm text-gray-700">{currentSession.notes}</p>
                </div>
              )}
            </Card>

            {/* Transactions */}
            <Card>
              <h2 className="text-lg font-semibold mb-4">Movimientos del Día</h2>
              {transactions && transactions.length > 0 ? (
                <div className="overflow-x-auto">
                  <table className="w-full">
                    <thead className="bg-gray-50">
                      <tr>
                        <th className="px-4 py-2 text-left text-sm font-medium">Hora</th>
                        <th className="px-4 py-2 text-left text-sm font-medium">Tipo</th>
                        <th className="px-4 py-2 text-left text-sm font-medium">Referencia</th>
                        <th className="px-4 py-2 text-right text-sm font-medium">Monto</th>
                        <th className="px-4 py-2 text-left text-sm font-medium">Usuario</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y">
                      {transactions.map((tx) => {
                        const isIncome = tx.direction === 'Inflow';
                        return (
                          <tr key={tx.id} className="hover:bg-gray-50">
                            <td className="px-4 py-3 text-sm">{formatDate(tx.createdAt)}</td>
                            <td className="px-4 py-3 text-sm">
                              {getTransactionTypeLabel(tx.transactionType)}
                            </td>
                            <td className="px-4 py-3 text-sm font-mono text-gray-600">
                              {tx.reference || '-'}
                            </td>
                            <td className={`px-4 py-3 text-sm font-medium text-right ${
                              isIncome ? 'text-green-600' : 'text-red-600'
                            }`}>
                              {isIncome ? '+' : '-'}{formatCurrency(tx.amount)}
                            </td>
                            <td className="px-4 py-3 text-sm">{tx.createdBy}</td>
                          </tr>
                        );
                      })}
                    </tbody>
                  </table>
                </div>
              ) : (
                <p className="text-center text-gray-600 py-8">No hay movimientos registrados</p>
              )}
            </Card>
          </div>
        ) : (
          <Card>
            <div className="text-center py-12">
              <div className="text-6xl mb-4">💰</div>
              <p className="text-lg font-medium text-gray-900 mb-2">No hay una caja abierta</p>
              <p className="text-gray-600 mb-6">Abre una sesión de caja para comenzar a registrar movimientos</p>
              <Button onClick={() => setShowOpenModal(true)}>Abrir Caja</Button>
            </div>
          </Card>
        )}

        {/* History Section */}
        <div>
          <Button variant="secondary" onClick={() => setShowHistory(!showHistory)}>
            {showHistory ? 'Ocultar Historial' : 'Ver Historial'}
          </Button>
        </div>

        {showHistory && history && (
          <Card>
            <h2 className="text-lg font-semibold mb-4">Historial de Cajas Cerradas</h2>
            <div className="overflow-x-auto">
              <table className="w-full">
                <thead className="bg-gray-50">
                  <tr>
                    <th className="px-4 py-2 text-left text-sm font-medium">Sesión</th>
                    <th className="px-4 py-2 text-left text-sm font-medium">Apertura</th>
                    <th className="px-4 py-2 text-left text-sm font-medium">Cierre</th>
                    <th className="px-4 py-2 text-right text-sm font-medium">Inicial</th>
                    <th className="px-4 py-2 text-right text-sm font-medium">Final</th>
                    <th className="px-4 py-2 text-right text-sm font-medium">Diferencia</th>
                  </tr>
                </thead>
                <tbody className="divide-y">
                  {history.items.map((session) => (
                    <tr key={session.id} className="hover:bg-gray-50">
                      <td className="px-4 py-3 text-sm font-mono">{session.sessionNumber}</td>
                      <td className="px-4 py-3 text-sm">{formatDate(session.openedAt)}</td>
                      <td className="px-4 py-3 text-sm">
                        {session.closedAt ? formatDate(session.closedAt) : '-'}
                      </td>
                      <td className="px-4 py-3 text-sm text-right">
                        {formatCurrency(session.openingBalance)}
                      </td>
                      <td className="px-4 py-3 text-sm text-right">
                        {session.closingBalance ? formatCurrency(session.closingBalance) : '-'}
                      </td>
                      <td className={`px-4 py-3 text-sm font-medium text-right ${
                        session.difference && session.difference !== 0
                          ? session.difference > 0
                            ? 'text-green-600'
                            : 'text-red-600'
                          : ''
                      }`}>
                        {session.difference !== undefined && session.difference !== null
                          ? formatCurrency(Math.abs(session.difference))
                          : '-'}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </Card>
        )}

        {/* Open Modal */}
        <Modal isOpen={showOpenModal} onClose={() => setShowOpenModal(false)} title="Abrir Caja" size="md">
          <form onSubmit={handleOpenSession} className="space-y-4">
            <div className="rounded-lg bg-blue-50 p-4 border border-blue-200">
              <p className="text-sm text-blue-900">
                💰 Ingresa el saldo inicial con el que comenzarás el día
              </p>
            </div>
            <Input
              label="Saldo Inicial (S/)"
              name="openingBalance"
              type="number"
              step="0.01"
              min="0"
              placeholder="0.00"
              required
              autoFocus
            />
            <div>
              <label className="mb-1.5 block text-sm font-medium text-slate-700">
                Notas (opcional)
              </label>
              <textarea
                name="notes"
                rows={3}
                className="block w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-500/20"
                placeholder="Observaciones sobre la apertura de caja..."
              />
            </div>
            <div className="flex gap-3 pt-2">
              <Button type="submit" disabled={openMutation.isPending} className="flex-1">
                {openMutation.isPending ? 'Abriendo...' : '✓ Abrir Caja'}
              </Button>
              <Button
                type="button"
                variant="ghost"
                onClick={() => setShowOpenModal(false)}
              >
                Cancelar
              </Button>
            </div>
          </form>
        </Modal>

        {/* Close Modal */}
        <Modal isOpen={showCloseModal && !!currentSession} onClose={() => setShowCloseModal(false)} title="Cerrar Caja" size="md">
          {currentSession && (
            <form onSubmit={handleCloseSession} className="space-y-4">
              <div className="rounded-lg bg-blue-50 p-4 border border-blue-200">
                <p className="text-sm font-medium text-blue-900 mb-1">Saldo Esperado (Sistema)</p>
                <p className="text-2xl font-bold text-blue-600">
                  {formatCurrency(currentSession.expectedBalance)}
                </p>
              </div>
              <div className="rounded-lg bg-amber-50 p-4 border border-amber-200">
                <p className="text-sm text-amber-900">
                  ⚠️ Cuenta el efectivo físico y registra el saldo real
                </p>
              </div>
              <Input
                label="Saldo Real (Conteo Físico)"
                name="closingBalance"
                type="number"
                step="0.01"
                min="0"
                placeholder="0.00"
                required
                autoFocus
              />
              <div>
                <label className="mb-1.5 block text-sm font-medium text-slate-700">
                  Notas (opcional)
                </label>
                <textarea
                  name="notes"
                  rows={3}
                  className="block w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-500/20"
                  placeholder="Observaciones sobre el cierre de caja..."
                />
              </div>
              <div className="flex gap-3 pt-2">
                <Button type="submit" variant="danger" disabled={closeMutation.isPending} className="flex-1">
                  {closeMutation.isPending ? 'Cerrando...' : '✓ Cerrar Caja'}
                </Button>
                <Button
                  type="button"
                  variant="ghost"
                  onClick={() => setShowCloseModal(false)}
                >
                  Cancelar
                </Button>
              </div>
            </form>
          )}
        </Modal>
      </div>
    </>
  );
}
