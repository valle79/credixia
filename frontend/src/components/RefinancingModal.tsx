import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useNavigate } from 'react-router-dom'
import { Modal } from './ui/Modal'
import { Button } from './ui/Button'
import { Input } from './ui/Input'
import { Card } from './ui/Card'
import { loansApi } from '../api/loans.api'
import { loanProductsApi } from '../api/loanProducts.api'
import { formatCurrency } from '../lib/format'
import { getErrorMessage } from '../api/client'
import { useToast } from '../hooks/useToast'
import type { LoanDetails, SimulateRefinancingResponse, LoanProductSummary } from '../api/types'

function addOneMonth(date: string): string {
  const result = new Date(date)
  result.setMonth(result.getMonth() + 1)
  return result.toISOString().split('T')[0]
}

interface RefinancingModalProps {
  isOpen: boolean
  onClose: () => void
  loan: LoanDetails
  tenantId?: string | undefined
}

export function RefinancingModal(props: RefinancingModalProps) {
  if (!props.isOpen) return null
  return <RefinancingModalContent {...props} />
}

function RefinancingModalContent({ isOpen, onClose, loan, tenantId }: RefinancingModalProps) {
  const queryClient = useQueryClient()
  const navigate = useNavigate()
  const toast = useToast()
  const [step, setStep] = useState<'simulate' | 'confirm'>('simulate')
  
  // Formulario
  const [refinancingDate, setRefinancingDate] = useState(() => new Date().toISOString().split('T')[0])
  const [additionalAmount, setAdditionalAmount] = useState('')
  const [newInterestRate, setNewInterestRate] = useState(loan.interestRate.toString())
  const [newTermMonths, setNewTermMonths] = useState(loan.numberOfPayments.toString())
  const [selectedProductId, setSelectedProductId] = useState('')
  const [firstPaymentDate, setFirstPaymentDate] = useState(() => addOneMonth(new Date().toISOString().split('T')[0]))
  const [notes, setNotes] = useState('')
  
  const [simulation, setSimulation] = useState<SimulateRefinancingResponse | null>(null)

  // Cargar productos de préstamo
  const { data: productsData } = useQuery({
    queryKey: ['loan-products', tenantId],
    queryFn: () => loanProductsApi.list({ active: true }),
    enabled: !!tenantId && isOpen,
  })

  const activeProducts = productsData ?? []
  const effectiveProductId = selectedProductId || activeProducts[0]?.id || ''

  const handleRefinancingDateChange = (value: string) => {
    setRefinancingDate(value)
    if (!value) return

    setFirstPaymentDate(addOneMonth(value))
  }

  const simulateMutation = useMutation({
    mutationFn: () =>
      loansApi.simulateRefinancing(loan.id, {
        refinancingDate,
        additionalAmount: additionalAmount ? parseFloat(additionalAmount) : undefined,
        newInterestRate: parseFloat(newInterestRate),
        newTermMonths: parseInt(newTermMonths),
      }),
    onSuccess: (data) => {
      setSimulation(data)
      setStep('confirm')
    },
    onError: (error) => {
      toast.error('Error al simular', getErrorMessage(error))
    },
  })

  const refinanceMutation = useMutation({
    mutationFn: () => {
      if (!simulation || !effectiveProductId) {
        throw new Error('Datos incompletos')
      }

      const selectedProduct = activeProducts.find((p: LoanProductSummary) => p.id === effectiveProductId)
      if (!selectedProduct) {
        throw new Error('Producto no encontrado')
      }

      // Calcular termDays basado en la frecuencia del producto
      const periodsPerYear: Record<string, number> = {
        Daily: 365,
        Weekly: 52,
        Biweekly: 24,
        Monthly: 12,
        Quarterly: 4,
        Biannual: 2,
        Annual: 1,
      }
      const periods = periodsPerYear[selectedProduct.frequency] || 12
      const termDays = Math.round((parseInt(newTermMonths) / periods) * 365)

      return loansApi.refinanceLoan(loan.id, {
        newLoanProductId: effectiveProductId,
        newInterestRate: parseFloat(newInterestRate),
        newTermDays: termDays,
        newNumberOfPayments: parseInt(newTermMonths),
        refinancingDate,
        firstPaymentDate,
        additionalAmount: additionalAmount ? parseFloat(additionalAmount) : undefined,
        notes: notes.trim() || undefined,
      })
    },
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: ['loans', tenantId] })
      toast.success(
        'Refinanciamiento exitoso',
        `Nuevo préstamo ${data.newLoanNumber} creado. El préstamo original ${data.originalLoanNumber} ha sido marcado como refinanciado.`
      )
      handleClose()
      navigate(`/loans/${data.newLoanId}`)
    },
    onError: (error) => {
      toast.error('Error al refinanciar', getErrorMessage(error))
    },
  })

  const handleSimulate = (e: React.FormEvent) => {
    e.preventDefault()
    simulateMutation.mutate()
  }

  const handleRefinance = () => {
    if (!simulation || !effectiveProductId) return
    refinanceMutation.mutate()
  }

  const handleClose = () => {
    setStep('simulate')
    setSimulation(null)
    setNotes('')
    onClose()
  }

  const monthlyPayment = simulation
    ? simulation.newPaymentAmount
    : 0

  return (
    <Modal
      isOpen={isOpen}
      onClose={handleClose}
      title="Refinanciar Préstamo"
      size="xl"
    >
      <div className="space-y-6">
        {/* Información del préstamo actual */}
        <div className="rounded-lg bg-slate-50 p-4 border border-slate-200">
          <div className="grid grid-cols-2 gap-4">
            <div>
              <p className="text-xs text-slate-600">Préstamo actual</p>
              <p className="text-sm font-semibold text-slate-900">{loan.loanNumber}</p>
              <p className="text-xs text-slate-600 mt-1">{loan.customerName}</p>
            </div>
            <div className="text-right">
              <p className="text-xs text-slate-600">Saldo actual</p>
              <p className="text-lg font-bold text-red-600">
                {formatCurrency(loan.balanceAmount, loan.currency)}
              </p>
            </div>
          </div>
        </div>

        {step === 'simulate' && (
          <form onSubmit={handleSimulate} className="space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <Input
                label="Fecha de refinanciamiento"
                type="date"
                value={refinancingDate}
                onChange={(e) => handleRefinancingDateChange(e.target.value)}
                required
              />
              <Input
                label="Primera fecha de pago"
                type="date"
                value={firstPaymentDate}
                onChange={(e) => setFirstPaymentDate(e.target.value)}
                required
              />
            </div>

            <Input
              label="Capital adicional (opcional)"
              type="number"
              step="0.01"
              min="0"
              value={additionalAmount}
              onChange={(e) => setAdditionalAmount(e.target.value)}
              placeholder="0.00"
              helperText="Monto adicional que desea agregar al refinanciamiento"
            />

            <div className="grid grid-cols-2 gap-4">
              <Input
                label="Nueva tasa de interés (%)"
                type="number"
                step="0.01"
                min="0"
                max="100"
                value={newInterestRate}
                onChange={(e) => setNewInterestRate(e.target.value)}
                required
              />
              <Input
                label="Nuevo plazo (cuotas)"
                type="number"
                min="1"
                value={newTermMonths}
                onChange={(e) => setNewTermMonths(e.target.value)}
                required
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-slate-700 mb-2">
                Producto de préstamo
              </label>
              <select
                value={effectiveProductId}
                onChange={(e) => setSelectedProductId(e.target.value)}
                className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-brand-500 focus:outline-none focus:ring-1 focus:ring-brand-500"
                required
              >
                {activeProducts.map((product: LoanProductSummary) => (
                  <option key={product.id} value={product.id}>
                    {product.name} ({product.interestRate}% {product.interestType} - {product.frequency})
                  </option>
                ))}
              </select>
            </div>

            <div className="rounded-lg border border-blue-200 bg-blue-50 p-4">
              <p className="text-sm text-blue-800">
                <strong>Nota:</strong> El refinanciamiento creará un nuevo préstamo que incluirá
                el capital pendiente más el interés devengado del préstamo actual, más cualquier
                capital adicional solicitado. El préstamo original será marcado como refinanciado.
              </p>
            </div>

            <div className="flex justify-end gap-3">
              <Button type="button" variant="ghost" onClick={handleClose}>
                Cancelar
              </Button>
              <Button type="submit" isLoading={simulateMutation.isPending}>
                Simular Refinanciamiento
              </Button>
            </div>
          </form>
        )}

        {step === 'confirm' && simulation && (
          <div className="space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <Card title="Deuda Actual">
                <div className="space-y-2">
                  <div className="flex justify-between text-sm">
                    <span className="text-slate-600">Capital pendiente:</span>
                    <span className="font-medium">
                      {formatCurrency(simulation.outstandingPrincipal, simulation.currency)}
                    </span>
                  </div>
                  <div className="flex justify-between text-sm">
                    <span className="text-slate-600">Interés devengado:</span>
                    <span className="font-medium">
                      {formatCurrency(simulation.accruedInterest, simulation.currency)}
                    </span>
                  </div>
                  <div className="border-t pt-2 flex justify-between">
                    <span className="font-semibold">Total deuda:</span>
                    <span className="font-bold text-red-600">
                      {formatCurrency(simulation.totalDebt, simulation.currency)}
                    </span>
                  </div>
                </div>
              </Card>

              <Card title="Nuevo Préstamo">
                <div className="space-y-2">
                  <div className="flex justify-between text-sm">
                    <span className="text-slate-600">Deuda actual:</span>
                    <span className="font-medium">
                      {formatCurrency(simulation.totalDebt, simulation.currency)}
                    </span>
                  </div>
                  {simulation.additionalAmount > 0 && (
                    <div className="flex justify-between text-sm">
                      <span className="text-slate-600">Capital adicional:</span>
                      <span className="font-medium text-green-600">
                        +{formatCurrency(simulation.additionalAmount, simulation.currency)}
                      </span>
                    </div>
                  )}
                  <div className="border-t pt-2 flex justify-between">
                    <span className="font-semibold">Nuevo capital:</span>
                    <span className="font-bold text-blue-600">
                      {formatCurrency(simulation.newPrincipal, simulation.currency)}
                    </span>
                  </div>
                </div>
              </Card>
            </div>

            <Card>
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <p className="text-xs text-slate-600">Nueva tasa</p>
                  <p className="text-lg font-semibold">{simulation.newInterestRate}%</p>
                </div>
                <div>
                  <p className="text-xs text-slate-600">Nuevo plazo</p>
                  <p className="text-lg font-semibold">{simulation.newTermMonths} cuotas</p>
                </div>
                <div>
                  <p className="text-xs text-slate-600">Cuota mensual estimada</p>
                  <p className="text-lg font-semibold text-green-600">
                    {formatCurrency(monthlyPayment, simulation.currency)}
                  </p>
                </div>
                <div>
                  <p className="text-xs text-slate-600">Monto total nuevo préstamo</p>
                  <p className="text-lg font-semibold">
                    {formatCurrency(simulation.newTotalAmount, simulation.currency)}
                  </p>
                </div>
              </div>
            </Card>

            <div>
              <label className="block text-sm font-medium text-slate-700 mb-2">
                Notas (opcional)
              </label>
              <textarea
                value={notes}
                onChange={(e) => setNotes(e.target.value)}
                rows={3}
                className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-brand-500 focus:outline-none focus:ring-1 focus:ring-brand-500"
                placeholder="Ingrese observaciones sobre el refinanciamiento..."
              />
            </div>

            <div className="rounded-lg border border-amber-200 bg-amber-50 p-4">
              <p className="text-sm text-amber-800">
                <strong>Confirmación:</strong> Al confirmar, se creará un nuevo préstamo y el
                préstamo actual ({loan.loanNumber}) será marcado como refinanciado. Esta acción
                no se puede deshacer.
              </p>
            </div>

            <div className="flex justify-end gap-3">
              <Button
                type="button"
                variant="ghost"
                onClick={() => setStep('simulate')}
              >
                Modificar
              </Button>
              <Button
                onClick={handleRefinance}
                isLoading={refinanceMutation.isPending}
              >
                Confirmar Refinanciamiento
              </Button>
            </div>
          </div>
        )}
      </div>
    </Modal>
  )
}
