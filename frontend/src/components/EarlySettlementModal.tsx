import { useState } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { Modal } from './ui/Modal'
import { Button } from './ui/Button'
import { Input } from './ui/Input'
import { Card } from './ui/Card'
import { loansApi } from '../api/loans.api'
import { formatCurrency, formatDate } from '../lib/format'
import { getErrorMessage } from '../api/client'
import { useToast } from '../hooks/useToast'
import type { LoanDetails, CalculateSettlementResponse } from '../api/types'

interface EarlySettlementModalProps {
  isOpen: boolean
  onClose: () => void
  loan: LoanDetails
  tenantId?: string | undefined
}

export function EarlySettlementModal(props: EarlySettlementModalProps) {
  if (!props.isOpen) return null
  return <EarlySettlementModalContent {...props} />
}

function EarlySettlementModalContent({ isOpen, onClose, loan, tenantId }: EarlySettlementModalProps) {
  const queryClient = useQueryClient()
  const toast = useToast()
  const [step, setStep] = useState<'calculate' | 'confirm'>('calculate')
  const [settlementDate, setSettlementDate] = useState(new Date().toISOString().split('T')[0])
  const [calculation, setCalculation] = useState<CalculateSettlementResponse | null>(null)
  const [notes, setNotes] = useState('')

  const calculateMutation = useMutation({
    mutationFn: (date: string) => loansApi.calculateSettlement(loan.id, date),
    onSuccess: (data) => {
      setCalculation(data)
      setStep('confirm')
    },
    onError: (error) => {
      toast.error('Error al calcular', getErrorMessage(error))
    },
  })

  const settleMutation = useMutation({
    mutationFn: () =>
      loansApi.settleLoan(loan.id, {
        amount: calculation!.totalSettlementAmount,
        settlementDate,
        notes: notes.trim() || undefined,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['loans', tenantId, loan.id] })
      queryClient.invalidateQueries({ queryKey: ['loans', tenantId] })
      toast.success('Préstamo liquidado', 'El préstamo ha sido liquidado anticipadamente')
      handleClose()
    },
    onError: (error) => {
      toast.error('Error al liquidar', getErrorMessage(error))
    },
  })

  const handleCalculate = (e: React.FormEvent) => {
    e.preventDefault()
    calculateMutation.mutate(settlementDate)
  }

  const handleSettle = () => {
    if (!calculation) return
    settleMutation.mutate()
  }

  const handleClose = () => {
    setStep('calculate')
    setCalculation(null)
    setNotes('')
    onClose()
  }

  return (
    <Modal
      isOpen={isOpen}
      onClose={handleClose}
      title="Liquidación Anticipada"
      size="lg"
    >
      <div className="space-y-6">
        {/* Información del préstamo */}
        <div className="rounded-lg bg-blue-50 p-4">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium text-blue-900">
                Préstamo {loan.loanNumber}
              </p>
              <p className="text-xs text-blue-700">{loan.customerName}</p>
            </div>
            <div className="text-right">
              <p className="text-xs text-blue-700">Saldo actual</p>
              <p className="text-lg font-bold text-blue-900">
                {formatCurrency(loan.balanceAmount, loan.currency)}
              </p>
            </div>
          </div>
        </div>

        {step === 'calculate' && (
          <form onSubmit={handleCalculate} className="space-y-4">
            <Input
              label="Fecha de liquidación"
              type="date"
              value={settlementDate}
              onChange={(e) => setSettlementDate(e.target.value)}
              required
            />

            <div className="rounded-lg border border-amber-200 bg-amber-50 p-4">
              <p className="text-sm text-amber-800">
                <strong>Nota:</strong> La liquidación anticipada calculará únicamente el capital
                pendiente más el interés devengado hasta la fecha. No se cobrará el interés
                futuro no devengado.
              </p>
            </div>

            <div className="flex justify-end gap-3">
              <Button type="button" variant="ghost" onClick={handleClose}>
                Cancelar
              </Button>
              <Button type="submit" isLoading={calculateMutation.isPending}>
                Calcular Liquidación
              </Button>
            </div>
          </form>
        )}

        {step === 'confirm' && calculation && (
          <div className="space-y-4">
            <Card>
              <div className="space-y-3">
                <div className="flex justify-between text-sm">
                  <span className="text-slate-600">Fecha de liquidación:</span>
                  <span className="font-medium text-slate-900">
                    {formatDate(calculation.calculationDate)}
                  </span>
                </div>
                <div className="flex justify-between text-sm">
                  <span className="text-slate-600">Capital pendiente:</span>
                  <span className="font-medium text-slate-900">
                    {formatCurrency(calculation.outstandingPrincipal, calculation.currency)}
                  </span>
                </div>
                <div className="flex justify-between text-sm">
                  <span className="text-slate-600">Interés devengado:</span>
                  <span className="font-medium text-slate-900">
                    {formatCurrency(calculation.accruedInterest, calculation.currency)}
                  </span>
                </div>
                <div className="border-t border-slate-200 pt-3 flex justify-between">
                  <span className="font-semibold text-slate-900">Total a pagar:</span>
                  <span className="text-xl font-bold text-green-600">
                    {formatCurrency(calculation.totalSettlementAmount, calculation.currency)}
                  </span>
                </div>
                {calculation.savings > 0 && (
                  <div className="rounded-lg bg-green-50 p-3 border border-green-200">
                    <p className="text-sm text-green-800">
                      <strong>Ahorro en intereses:</strong>{' '}
                      {formatCurrency(calculation.savings, calculation.currency)}
                    </p>
                  </div>
                )}
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
                placeholder="Ingrese observaciones sobre la liquidación..."
              />
            </div>

            <div className="flex justify-end gap-3">
              <Button
                type="button"
                variant="ghost"
                onClick={() => setStep('calculate')}
              >
                Recalcular
              </Button>
              <Button
                onClick={handleSettle}
                isLoading={settleMutation.isPending}
              >
                Confirmar Liquidación
              </Button>
            </div>
          </div>
        )}
      </div>
    </Modal>
  )
}
