import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useParams, useNavigate, Link } from 'react-router-dom'
import { Copy, ExternalLink, MessageCircle } from 'lucide-react'
import { loansApi } from '../api/loans.api'
import { Card } from '../components/ui/Card'
import { Button } from '../components/ui/Button'
import { Badge } from '../components/ui/Badge'
import { Input } from '../components/ui/Input'
import { Modal } from '../components/ui/Modal'
import { formatCurrency, formatDate, getStatusVariant, getStatusLabel } from '../lib/format'
import { getErrorMessage } from '../api/client'
import { useToast } from '../hooks/useToast'
import { useAuth } from '../hooks/useAuth'
import { ToastContainer } from '../components/ui/ToastContainer'
import type { LoanDetails, PaymentScheduleItem, RegisterPaymentRequest } from '../api/types'

function normalizeWhatsAppPhone(phone?: string): string | null {
  if (!phone) return null

  const digits = phone.replace(/\D/g, '')
  if (!digits) return null

  if (digits.startsWith('51') && digits.length >= 11) return digits
  if (digits.length === 9 && digits.startsWith('9')) return `51${digits}`

  return digits
}

function buildScheduleMessage(
  loan: LoanDetails,
  schedule: PaymentScheduleItem[],
  tenantName?: string,
): string {
  const ordered = [...schedule].sort((a, b) => a.paymentNumber - b.paymentNumber)
  const scheduleLines = ordered.map((item) => {
    const status = item.isPaid ? 'Pagado' : item.isOverdue ? 'Vencido' : 'Pendiente'
    return `${item.paymentNumber}. ${formatDate(item.dueDate)} - ${formatCurrency(item.dueAmount, loan.currency)} - ${status}`
  })

  const lines = [
    `Hola ${loan.customerName}, le compartimos el cronograma de pagos de su préstamo ${loan.loanNumber}.`,
    '',
    `Producto: ${loan.productName}`,
    `Capital: ${formatCurrency(loan.principalAmount, loan.currency)}`,
    `Total a pagar: ${formatCurrency(loan.totalAmount, loan.currency)}`,
    `Cuotas: ${ordered.length}`,
    '',
    'Cronograma:',
    ...scheduleLines,
    '',
    `Pagado: ${formatCurrency(loan.paidAmount, loan.currency)}`,
    `Saldo: ${formatCurrency(loan.balanceAmount, loan.currency)}`,
  ]

  if (tenantName) {
    lines.push('', tenantName)
  }

  return lines.join('\n')
}

export function LoanDetailPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const toast = useToast()
  const { tenant } = useAuth()

  const [showPaymentForm, setShowPaymentForm] = useState(false)
  const [showScheduleModal, setShowScheduleModal] = useState(false)
  const [paymentData, setPaymentData] = useState({
    receiptNumber: '',
    amount: '',
    paymentDate: new Date().toISOString().split('T')[0],
  })

  const { data: loan, isLoading } = useQuery({
    queryKey: ['loans', tenant?.id, id],
    queryFn: () => loansApi.get(id!),
    enabled: !!tenant?.id && !!id,
  })

  const { data: schedule } = useQuery({
    queryKey: ['loans', tenant?.id, id, 'schedule'],
    queryFn: () => loansApi.getSchedule(id!),
    enabled: !!tenant?.id && !!id,
  })

  const approveMutation = useMutation({
    mutationFn: () => loansApi.approve(id!),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['loans', tenant?.id, id] })
      toast.success('Préstamo aprobado', 'El préstamo ha sido aprobado exitosamente')
    },
    onError: (error) => {
      toast.error('Error al aprobar', getErrorMessage(error))
    },
  })

  const disburseMutation = useMutation({
    mutationFn: () => loansApi.disburse(id!),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['loans', tenant?.id, id] })
      toast.success('Préstamo desembolsado', 'El préstamo ha sido desembolsado exitosamente')
    },
    onError: (error) => {
      toast.error('Error al desembolsar', getErrorMessage(error))
    },
  })

  const registerPaymentMutation = useMutation({
    mutationFn: (data: RegisterPaymentRequest) => loansApi.registerPayment(id!, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['loans', tenant?.id, id] })
      queryClient.invalidateQueries({ queryKey: ['loans', tenant?.id, id, 'schedule'] })
      setShowPaymentForm(false)
      setPaymentData({ receiptNumber: '', amount: '', paymentDate: new Date().toISOString().split('T')[0] })
      toast.success('Pago registrado', 'El pago ha sido registrado exitosamente')
    },
    onError: (error) => {
      toast.error('Error al registrar pago', getErrorMessage(error))
    },
  })

  const handleRegisterPayment = (e: React.FormEvent) => {
    e.preventDefault()
    registerPaymentMutation.mutate({
      receiptNumber: paymentData.receiptNumber,
      amount: parseFloat(paymentData.amount),
      paymentDate: paymentData.paymentDate,
    })
  }

  if (isLoading) {
    return <div className="py-12 text-center text-sm text-slate-500">Cargando...</div>
  }

  if (!loan) {
    return <div className="py-12 text-center text-sm text-slate-500">Préstamo no encontrado</div>
  }

  const isPending = loan.status === 'Pending'
  const isApproved = loan.status === 'Approved'
  const isActive = loan.status === 'Active'

  const orderedSchedule = schedule ? [...schedule].sort((a, b) => a.paymentNumber - b.paymentNumber) : []
  const whatsAppPhone = normalizeWhatsAppPhone(loan.customerPhone)
  const scheduleMessage = buildScheduleMessage(loan, orderedSchedule, tenant?.name)
  const whatsAppUrl = whatsAppPhone
    ? `https://wa.me/${whatsAppPhone}?text=${encodeURIComponent(scheduleMessage)}`
    : null
  const canShareSchedule = !!whatsAppPhone && orderedSchedule.length > 0

  const handleCopySchedule = async () => {
    try {
      await navigator.clipboard.writeText(scheduleMessage)
      toast.success('Mensaje copiado', 'Pega el mensaje en WhatsApp para enviarlo al cliente.')
    } catch {
      toast.error('No se pudo copiar', 'Selecciona el texto de la vista previa y cópialo manualmente.')
    }
  }

  const handleOpenWhatsApp = () => {
    if (!whatsAppUrl) return
    window.open(whatsAppUrl, '_blank', 'noopener,noreferrer')
    setShowScheduleModal(false)
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-4">
          <Button variant="ghost" onClick={() => navigate('/loans')}>
            ← Volver
          </Button>
          <div>
            <h1 className="text-2xl font-bold text-slate-900">Préstamo {loan.loanNumber}</h1>
            <p className="mt-1 text-sm text-slate-600">
              Cliente: <Link to={`/customers/${loan.customerId}`} className="font-medium text-brand-600 hover:text-brand-700">{loan.customerName}</Link>
            </p>
          </div>
        </div>
        <div className="flex flex-wrap items-center justify-end gap-3">
          <Button
            variant="secondary"
            onClick={() => setShowScheduleModal(true)}
            disabled={!canShareSchedule}
            title={
              !whatsAppPhone
                ? 'El cliente no tiene un teléfono registrado'
                : orderedSchedule.length === 0
                  ? 'El cronograma todavía no está disponible'
                  : 'Compartir el cronograma por WhatsApp'
            }
          >
            <MessageCircle className="h-4 w-4 text-emerald-600" />
            Enviar por WhatsApp
          </Button>
          <Badge variant={getStatusVariant(loan.status)} className="text-sm px-3 py-1">
            {getStatusLabel(loan.status)}
          </Badge>
        </div>
      </div>

      <div className="grid gap-6 lg:grid-cols-3">
        <div className="lg:col-span-2 space-y-6">
          <Card title="Detalles del Préstamo">
            <div className="grid gap-4 sm:grid-cols-2">
              <div>
                <p className="text-xs font-medium text-slate-500">Producto</p>
                <p className="mt-1 text-sm text-slate-900">{loan.productName}</p>
              </div>
              <div>
                <p className="text-xs font-medium text-slate-500">Frecuencia</p>
                <p className="mt-1 text-sm text-slate-900">{loan.frequency}</p>
              </div>
              <div>
                <p className="text-xs font-medium text-slate-500">Tasa de Interés</p>
                <p className="mt-1 text-sm text-slate-900">{loan.interestRate}% {loan.interestType}</p>
              </div>
              <div>
                <p className="text-xs font-medium text-slate-500">Plazo</p>
                <p className="mt-1 text-sm text-slate-900">{loan.termDays} días ({loan.numberOfPayments} cuotas)</p>
              </div>
              <div>
                <p className="text-xs font-medium text-slate-500">Fecha Desembolso</p>
                <p className="mt-1 text-sm text-slate-900">{formatDate(loan.disbursementDate)}</p>
              </div>
              <div>
                <p className="text-xs font-medium text-slate-500">Primer Pago</p>
                <p className="mt-1 text-sm text-slate-900">{formatDate(loan.firstPaymentDate)}</p>
              </div>
            </div>
          </Card>

          {/* Cronograma de Pagos */}
          <Card title="Cronograma de Pagos" subtitle={`${schedule?.length || 0} cuotas`}>
            {schedule && schedule.length > 0 ? (
              <div className="overflow-x-auto">
                <table className="w-full">
                  <thead className="border-b border-slate-200 bg-slate-50">
                    <tr>
                      <th className="px-3 py-2 text-left text-xs font-medium uppercase text-slate-600">#</th>
                      <th className="px-3 py-2 text-left text-xs font-medium uppercase text-slate-600">Vencimiento</th>
                      <th className="px-3 py-2 text-right text-xs font-medium uppercase text-slate-600">Capital</th>
                      <th className="px-3 py-2 text-right text-xs font-medium uppercase text-slate-600">Interés</th>
                      <th className="px-3 py-2 text-right text-xs font-medium uppercase text-slate-600">Cuota</th>
                      <th className="px-3 py-2 text-right text-xs font-medium uppercase text-slate-600">Saldo</th>
                      <th className="px-3 py-2 text-right text-xs font-medium uppercase text-slate-600">Pagado</th>
                      <th className="px-3 py-2 text-center text-xs font-medium uppercase text-slate-600">Estado</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-100">
                    {[...schedule].sort((a, b) => a.paymentNumber - b.paymentNumber).map((item) => (
                      <tr key={item.id} className={item.isOverdue ? 'bg-red-50' : item.isPaid ? 'bg-emerald-50' : ''}>
                        <td className="px-3 py-2 text-sm font-medium text-slate-900">{item.paymentNumber}</td>
                        <td className="px-3 py-2 text-sm text-slate-700">{formatDate(item.dueDate)}</td>
                        <td className="px-3 py-2 text-right text-sm text-slate-900">
                          {item.principalAmount ? formatCurrency(item.principalAmount, loan.currency) : '-'}
                        </td>
                        <td className="px-3 py-2 text-right text-sm text-slate-600">
                          {item.interestAmount ? formatCurrency(item.interestAmount, loan.currency) : '-'}
                        </td>
                        <td className="px-3 py-2 text-right text-sm font-medium text-slate-900">
                          {formatCurrency(item.dueAmount, loan.currency)}
                        </td>
                        <td className="px-3 py-2 text-right text-sm font-semibold text-blue-600">
                          {item.remainingBalance !== undefined ? formatCurrency(item.remainingBalance, loan.currency) : '-'}
                        </td>
                        <td className="px-3 py-2 text-right text-sm text-emerald-600">
                          {formatCurrency(item.paidAmount, loan.currency)}
                        </td>
                        <td className="px-3 py-2 text-center">
                          {item.isPaid ? (
                            <Badge variant="success" className="text-xs">Pagado</Badge>
                          ) : item.isOverdue ? (
                            <Badge variant="error" className="text-xs">Vencido</Badge>
                          ) : (
                            <Badge variant="warning" className="text-xs">Pendiente</Badge>
                          )}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            ) : (
              <p className="py-8 text-center text-sm text-slate-500">No hay cronograma disponible</p>
            )}
          </Card>
        </div>

        <div className="space-y-6">
          <Card title="Resumen Financiero">
            <div className="space-y-3">
              <div className="flex justify-between text-sm">
                <span className="text-slate-600">Capital:</span>
                <span className="font-medium text-slate-900">{formatCurrency(loan.principalAmount, loan.currency)}</span>
              </div>
              <div className="flex justify-between text-sm">
                <span className="text-slate-600">Intereses:</span>
                <span className="font-medium text-slate-900">{formatCurrency(loan.totalInterest, loan.currency)}</span>
              </div>
              {loan.commission && (
                <div className="flex justify-between text-sm">
                  <span className="text-slate-600">Comisión:</span>
                  <span className="font-medium text-slate-900">{formatCurrency(loan.commission, loan.currency)}</span>
                </div>
              )}
              <div className="border-t border-slate-200 pt-3 flex justify-between">
                <span className="font-medium text-slate-900">Total:</span>
                <span className="text-lg font-bold text-slate-900">{formatCurrency(loan.totalAmount, loan.currency)}</span>
              </div>
              <div className="flex justify-between text-sm">
                <span className="text-slate-600">Pagado:</span>
                <span className="font-medium text-emerald-600">{formatCurrency(loan.paidAmount, loan.currency)}</span>
              </div>
              <div className="flex justify-between border-t border-slate-200 pt-3">
                <span className="font-medium text-slate-900">Saldo:</span>
                <span className="text-lg font-bold text-red-600">{formatCurrency(loan.balanceAmount, loan.currency)}</span>
              </div>
            </div>
          </Card>

          {/* Acciones */}
          <Card title="Acciones">
            <div className="space-y-3">
              {isPending && (
                  <Button
                    className="w-full"
                    onClick={() => approveMutation.mutate()}
                    isLoading={approveMutation.isPending}
                  >
                    Aprobar Préstamo
                  </Button>
              )}

              {isApproved && (
                  <Button
                    className="w-full"
                    onClick={() => disburseMutation.mutate()}
                    isLoading={disburseMutation.isPending}
                  >
                    Desembolsar
                  </Button>
              )}

              {isActive && (
                <>
                  {!showPaymentForm ? (
                    <Button className="w-full" onClick={() => setShowPaymentForm(true)}>
                      Registrar Pago
                    </Button>
                  ) : (
                    <form onSubmit={handleRegisterPayment} className="space-y-3">
                      <Input
                        label="N° Recibo"
                        value={paymentData.receiptNumber}
                        onChange={(e) => setPaymentData((prev) => ({ ...prev, receiptNumber: e.target.value }))}
                        required
                      />
                      <Input
                        label="Monto (S/)"
                        type="number"
                        step="0.01"
                        value={paymentData.amount}
                        onChange={(e) => setPaymentData((prev) => ({ ...prev, amount: e.target.value }))}
                        required
                      />
                      <Input
                        label="Fecha"
                        type="date"
                        value={paymentData.paymentDate}
                        onChange={(e) => setPaymentData((prev) => ({ ...prev, paymentDate: e.target.value }))}
                        required
                      />
                      <div className="flex gap-2">
                        <Button
                          type="submit"
                          size="sm"
                          className="flex-1"
                          isLoading={registerPaymentMutation.isPending}
                        >
                          Guardar
                        </Button>
                        <Button
                          type="button"
                          size="sm"
                          variant="ghost"
                          onClick={() => setShowPaymentForm(false)}
                        >
                          Cancelar
                        </Button>
                      </div>
                    </form>
                  )}
                </>
              )}
            </div>
          </Card>
        </div>
      </div>

      <Modal
        isOpen={showScheduleModal}
        onClose={() => setShowScheduleModal(false)}
        title="Compartir cronograma por WhatsApp"
        size="lg"
      >
        <div className="space-y-4">
          <div className="rounded-lg border border-emerald-100 bg-emerald-50 px-4 py-3">
            <p className="text-sm font-medium text-slate-900">{loan.customerName}</p>
            <p className="mt-1 text-sm text-slate-600">
              {loan.customerPhone} · {orderedSchedule.length} cuotas
            </p>
          </div>

          <div>
            <p className="mb-2 text-xs font-medium uppercase tracking-wide text-slate-500">
              Vista previa del mensaje
            </p>
            <pre className="max-h-72 overflow-y-auto whitespace-pre-wrap break-words rounded-lg border border-slate-200 bg-slate-50 p-4 font-sans text-sm leading-6 text-slate-700">
              {scheduleMessage}
            </pre>
          </div>

          <div className="flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
            <Button variant="secondary" onClick={handleCopySchedule}>
              <Copy className="h-4 w-4" />
              Copiar mensaje
            </Button>
            <Button onClick={handleOpenWhatsApp}>
              <ExternalLink className="h-4 w-4" />
              Abrir WhatsApp
            </Button>
          </div>
        </div>
      </Modal>

      <ToastContainer toasts={toast.toasts} onClose={toast.close} />
    </div>
  )
}

