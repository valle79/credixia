import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { loansApi } from '../api/loans.api'
import { customersApi } from '../api/customers.api'
import { loanProductsApi } from '../api/loanProducts.api'
import { Card } from '../components/ui/Card'
import { Button } from '../components/ui/Button'
import { Input } from '../components/ui/Input'
import { Select } from '../components/ui/Select'
import { ToastContainer } from '../components/ui/ToastContainer'
import { useToast } from '../hooks/useToast'
import { useAuth } from '../hooks/useAuth'
import { formatCurrency, formatPercent } from '../lib/format'
import { getErrorMessage } from '../api/client'
import type { CreateLoanRequest } from '../api/types'

export function CreateLoanPage() {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const toast = useToast()
  const { tenant } = useAuth()
  const [searchParams] = useSearchParams()

  const [customerId, setCustomerId] = useState(searchParams.get('customerId') ?? '')
  const [productId, setProductId] = useState('')
  const [principalAmount, setPrincipalAmount] = useState('')
  const [frequency, setFrequency] = useState('')
  const [numberOfPayments, setNumberOfPayments] = useState('')
  const [disbursementDate, setDisbursementDate] = useState('')
  const [firstPaymentDate, setFirstPaymentDate] = useState('')
  const [commissionAmount, setCommissionAmount] = useState('')
  const [customInterestRate, setCustomInterestRate] = useState('')
  const [useCustomRate, setUseCustomRate] = useState(false)

  // Calcular días automáticamente según frecuencia
  const calculateTermDays = (freq: string, payments: string) => {
    if (!freq || !payments) return 0
    
    const daysPerPeriod: Record<string, number> = {
      'Daily': 1,
      'Weekly': 7,
      'Biweekly': 15,
      'Monthly': 30,
      'Quarterly': 90
    }
    
    return daysPerPeriod[freq] * parseInt(payments)
  }

  const termDays = calculateTermDays(frequency, numberOfPayments)

  const calculateFirstPaymentDate = (dateValue: string, freq: string) => {
    if (!dateValue || !freq) return ''

    const date = new Date(dateValue)
    const daysToAdd = freq === 'Daily' ? 1 :
                      freq === 'Weekly' ? 7 :
                      freq === 'Biweekly' ? 15 :
                      freq === 'Monthly' ? 30 :
                      freq === 'Quarterly' ? 90 : 30

    date.setDate(date.getDate() + daysToAdd)
    return date.toISOString().split('T')[0]
  }

  const handleFrequencyChange = (value: string) => {
    setFrequency(value)
    setFirstPaymentDate(calculateFirstPaymentDate(disbursementDate, value))
  }

  const handleDisbursementDateChange = (value: string) => {
    setDisbursementDate(value)
    setFirstPaymentDate(calculateFirstPaymentDate(value, frequency))
  }

  const { data: customers } = useQuery({
    queryKey: ['customers', tenant?.id, 'active'],
    queryFn: () => customersApi.list({ status: 'Active', pageSize: 200 }),
    enabled: !!tenant?.id,
  })

  const { data: products } = useQuery({
    queryKey: ['loan-products', tenant?.id],
    queryFn: () => loanProductsApi.list(true),
    enabled: !!tenant?.id,
  })

  const selectedProduct = products?.find((p) => p.id === productId)

  // Calcular estimación de cuota
  const calculatePayment = () => {
    if (!selectedProduct || !principalAmount || !numberOfPayments) return 0

    const principal = parseFloat(principalAmount)
    const n = parseInt(numberOfPayments)
    // Usar tasa personalizada si está habilitada, sino usar la del producto
    const interestRate = useCustomRate && customInterestRate 
      ? parseFloat(customInterestRate) 
      : selectedProduct.interestRate
    const rate = interestRate / 100

    if (selectedProduct.interestType === 'Simple') {
      // Interés simple: se aplica la tasa sobre el capital total
      // Total = Capital + (Capital × Tasa)
      const totalInterest = principal * rate
      const totalAmount = principal + totalInterest
      return totalAmount / n
    } else {
      // Interés compuesto - fórmula de anualidad
      const r = rate / n
      const payment = (principal * r * Math.pow(1 + r, n)) / (Math.pow(1 + r, n) - 1)
      return payment
    }
  }

  const estimatedPayment = calculatePayment()

  const createMutation = useMutation({
    mutationFn: (data: CreateLoanRequest) => loansApi.create(data),
    onSuccess: (result) => {
      queryClient.invalidateQueries({ queryKey: ['loans'] })
      toast.success('¡Préstamo creado!', 'El préstamo se creó correctamente')
      navigate(`/loans/${result.id}`)
    },
    onError: (error) => {
      toast.error('Error al crear préstamo', getErrorMessage(error))
    },
  })

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    
    const data: CreateLoanRequest = {
      customerId,
      loanProductId: productId,
      principalAmount: parseFloat(principalAmount),
      termDays: termDays,
      numberOfPayments: parseInt(numberOfPayments),
      disbursementDate,
      firstPaymentDate,
      ...(commissionAmount ? { commissionAmount: parseFloat(commissionAmount) } : {}),
      ...(useCustomRate && customInterestRate ? { customInterestRate: parseFloat(customInterestRate) } : {}),
    }

    createMutation.mutate(data)
  }

  return (
    <>
      <ToastContainer toasts={toast.toasts} onClose={toast.close} />
      <div className="space-y-6">
      <div className="flex items-center gap-4">
        <Button variant="ghost" onClick={() => navigate('/loans')}>
          ← Volver
        </Button>
        <div>
          <h1 className="text-2xl font-bold text-slate-900">Nuevo Préstamo</h1>
          <p className="mt-1 text-sm text-slate-600">Crea una solicitud de préstamo</p>
        </div>
      </div>

      <form onSubmit={handleSubmit}>
        <div className="grid gap-6 lg:grid-cols-3">
          <div className="lg:col-span-2 space-y-6">
            <Card title="Cliente y Producto">
              <div className="space-y-4">
                <Select
                  label="Cliente"
                  value={customerId}
                  onChange={(e) => setCustomerId(e.target.value)}
                  options={[
                    { value: '', label: 'Seleccionar cliente...' },
                    ...(customers?.items.map((c) => ({
                      value: c.id,
                      label: `${c.fullName} - ${c.documentNumber}`,
                    })) || []),
                  ]}
                  required
                />

                <Select
                  label="Producto de Préstamo"
                  value={productId}
                  onChange={(e) => setProductId(e.target.value)}
                  options={[
                    { value: '', label: 'Seleccionar producto...' },
                    ...(products?.map((p) => ({
                      value: p.id,
                      label: `${p.name} - ${formatPercent(p.interestRate)} ${p.frequency}`,
                    })) || []),
                  ]}
                  required
                />

                {selectedProduct && (
                  <div className="rounded-lg bg-blue-50 p-4 text-sm">
                    <p className="font-medium text-blue-900">Detalles del Producto</p>
                    <ul className="mt-2 space-y-1 text-blue-700">
                      <li>• Tasa: {formatPercent(selectedProduct.interestRate)} {selectedProduct.interestType}</li>
                      <li>• Monto: {formatCurrency(selectedProduct.minimumAmount, selectedProduct.currency)} - {formatCurrency(selectedProduct.maximumAmount, selectedProduct.currency)}</li>
                      <li>• Plazo: {selectedProduct.minimumTermDays} - {selectedProduct.maximumTermDays} días</li>
                      <li>• Frecuencia: {selectedProduct.frequency}</li>
                    </ul>
                  </div>
                )}

                <div className="space-y-3">
                  <div className="flex items-center gap-2">
                    <input
                      type="checkbox"
                      id="useCustomRate"
                      checked={useCustomRate}
                      onChange={(e) => {
                        setUseCustomRate(e.target.checked)
                        if (!e.target.checked) {
                          setCustomInterestRate('')
                        } else if (selectedProduct) {
                          setCustomInterestRate(selectedProduct.interestRate.toString())
                        }
                      }}
                      className="h-4 w-4 rounded border-slate-300 text-brand-600 focus:ring-brand-500"
                    />
                    <label htmlFor="useCustomRate" className="text-sm font-medium text-slate-700">
                      Usar tasa de interés personalizada
                    </label>
                  </div>

                  {useCustomRate && (
                    <div className="rounded-lg bg-amber-50 border border-amber-200 p-4">
                      <Input
                        label="Tasa de Interés Personalizada (%)"
                        type="number"
                        value={customInterestRate}
                        onChange={(e) => setCustomInterestRate(e.target.value)}
                        step="0.01"
                        min="0"
                        max="100"
                        helperText="Esta tasa reemplazará la tasa del producto"
                        required={useCustomRate}
                      />
                      <p className="mt-2 text-xs text-amber-700">
                        ⚠️ La tasa del producto ({formatPercent(selectedProduct?.interestRate ?? 0)}) será reemplazada por esta tasa personalizada.
                      </p>
                    </div>
                  )}
                </div>
              </div>
            </Card>

            <Card title="Términos del Préstamo">
              <div className="grid gap-4 sm:grid-cols-2">
                <Input
                  label="Monto a Prestar (S/)"
                  type="number"
                  value={principalAmount}
                  onChange={(e) => setPrincipalAmount(e.target.value)}
                  step="0.01"
                  min={selectedProduct?.minimumAmount}
                  max={selectedProduct?.maximumAmount}
                  required
                />

                <div>
                  <label className="mb-1.5 block text-sm font-medium text-slate-700">
                    Frecuencia de Pago <span className="text-red-500">*</span>
                  </label>
                  <select
                    value={frequency}
                    onChange={(e) => handleFrequencyChange(e.target.value)}
                    className="block w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm text-slate-900 focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-500/20"
                    required
                  >
                    <option value="">Seleccionar frecuencia...</option>
                    <option value="Daily">📅 Diario (cada día)</option>
                    <option value="Weekly">📅 Semanal (cada 7 días)</option>
                    <option value="Biweekly">📅 Quincenal (cada 15 días)</option>
                    <option value="Monthly">📅 Mensual (cada 30 días)</option>
                    <option value="Quarterly">📅 Trimestral (cada 90 días)</option>
                  </select>
                  {frequency && (
                    <p className="mt-1 text-xs text-slate-500">
                      Plazo total: {termDays} días
                    </p>
                  )}
                </div>

                <Input
                  label="Número de Cuotas"
                  type="number"
                  value={numberOfPayments}
                  onChange={(e) => setNumberOfPayments(e.target.value)}
                  min="1"
                  helperText={frequency ? `Pagos cada ${frequency === 'Daily' ? '1 día' : frequency === 'Weekly' ? '7 días' : frequency === 'Biweekly' ? '15 días' : frequency === 'Monthly' ? '30 días' : '90 días'}` : ''}
                  required
                />

                <Input
                  label="Comisión (S/) - Opcional"
                  type="number"
                  value={commissionAmount}
                  onChange={(e) => setCommissionAmount(e.target.value)}
                  step="0.01"
                  min="0"
                />

                <Input
                  label="Fecha de Desembolso"
                  type="date"
                  value={disbursementDate}
                  onChange={(e) => handleDisbursementDateChange(e.target.value)}
                  required
                />

                <Input
                  label="Fecha Primer Pago"
                  type="date"
                  value={firstPaymentDate}
                  onChange={(e) => setFirstPaymentDate(e.target.value)}
                  helperText="Se calcula automáticamente según la frecuencia"
                  required
                />
              </div>
            </Card>
          </div>

          <div className="space-y-6">
            <Card title="Resumen">
              <div className="space-y-3">
                <div className="flex justify-between text-sm">
                  <span className="text-slate-600">Monto Capital:</span>
                  <span className="font-medium text-slate-900">
                    {principalAmount ? formatCurrency(parseFloat(principalAmount), 'PEN') : 'S/ 0.00'}
                  </span>
                </div>

                {selectedProduct && (
                  <div className="flex justify-between text-sm">
                    <span className="text-slate-600">Tasa de Interés:</span>
                    <span className="font-medium text-slate-900">
                      {useCustomRate && customInterestRate 
                        ? formatPercent(parseFloat(customInterestRate))
                        : formatPercent(selectedProduct.interestRate)}
                      {useCustomRate && <span className="ml-1 text-amber-600 text-xs">★ Personalizada</span>}
                    </span>
                  </div>
                )}
                
                <div className="flex justify-between text-sm">
                  <span className="text-slate-600">Plazo Total:</span>
                  <span className="font-medium text-slate-900">
                    {termDays > 0 ? `${termDays} días` : '-'}
                  </span>
                </div>

                <div className="flex justify-between text-sm">
                  <span className="text-slate-600">Cuota Estimada:</span>
                  <span className="font-medium text-slate-900">
                    {estimatedPayment > 0 ? formatCurrency(estimatedPayment, 'PEN') : 'S/ 0.00'}
                  </span>
                </div>

                <div className="flex justify-between text-sm">
                  <span className="text-slate-600">Número de Cuotas:</span>
                  <span className="font-medium text-slate-900">{numberOfPayments || '0'}</span>
                </div>

                {commissionAmount && (
                  <div className="flex justify-between text-sm">
                    <span className="text-slate-600">Comisión:</span>
                    <span className="font-medium text-slate-900">
                      {formatCurrency(parseFloat(commissionAmount), 'PEN')}
                    </span>
                  </div>
                )}

                <div className="border-t border-slate-200 pt-3 flex justify-between">
                  <span className="font-medium text-slate-900">Total Estimado:</span>
                  <span className="text-lg font-bold text-brand-600">
                    {estimatedPayment > 0 && numberOfPayments
                      ? formatCurrency(estimatedPayment * parseInt(numberOfPayments), 'PEN')
                      : 'S/ 0.00'}
                  </span>
                </div>
              </div>
            </Card>

            <div className="space-y-3">
              <Button
                type="submit"
                className="w-full"
                isLoading={createMutation.isPending}
                disabled={!customerId || !productId || !principalAmount || !frequency || !numberOfPayments}
              >
                Crear Préstamo
              </Button>
              <Button
                type="button"
                variant="ghost"
                className="w-full"
                onClick={() => navigate('/loans')}
              >
                Cancelar
              </Button>
            </div>
          </div>
        </div>
      </form>
    </div>
    </>
  )
}
