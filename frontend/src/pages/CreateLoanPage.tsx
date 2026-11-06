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

const PAYMENT_FREQUENCY_DAYS: Record<string, number> = {
  Daily: 1,
  Weekly: 7,
  Biweekly: 15,
  Monthly: 30,
  Quarterly: 90,
  Biannual: 180,
  Annual: 365,
}

const PAYMENT_FREQUENCY_LABELS: Record<string, string> = {
  Daily: 'Diario',
  Weekly: 'Semanal',
  Biweekly: 'Quincenal',
  Monthly: 'Mensual',
  Quarterly: 'Trimestral',
  Biannual: 'Semestral',
  Annual: 'Anual',
}

const PAYMENT_FREQUENCY_HELP: Record<string, string> = {
  Daily: 'cada día',
  Weekly: 'cada 7 días',
  Biweekly: 'cada 15 días',
  Monthly: 'cada mes',
  Quarterly: 'cada 3 meses',
  Biannual: 'cada 6 meses',
  Annual: 'cada año',
}

function parseDateOnly(value: string): Date | null {
  const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(value)
  if (!match) return null

  return new Date(Date.UTC(Number(match[1]), Number(match[2]) - 1, Number(match[3])))
}

function formatDateOnly(date: Date): string {
  return date.toISOString().slice(0, 10)
}

function addMonthsClamped(date: Date, months: number): Date {
  const target = new Date(Date.UTC(date.getUTCFullYear(), date.getUTCMonth() + months, 1))
  const lastDay = new Date(
    Date.UTC(target.getUTCFullYear(), target.getUTCMonth() + 1, 0)
  ).getUTCDate()
  target.setUTCDate(Math.min(date.getUTCDate(), lastDay))
  return target
}

function addPaymentPeriods(date: Date, frequency: string, periods: number): Date {
  if (frequency === 'Daily') {
    date.setUTCDate(date.getUTCDate() + periods)
    return date
  }
  if (frequency === 'Weekly') {
    date.setUTCDate(date.getUTCDate() + 7 * periods)
    return date
  }
  if (frequency === 'Biweekly') {
    date.setUTCDate(date.getUTCDate() + 15 * periods)
    return date
  }
  if (frequency === 'Quarterly') {
    return addMonthsClamped(date, 3 * periods)
  }
  if (frequency === 'Biannual') {
    return addMonthsClamped(date, 6 * periods)
  }
  if (frequency === 'Annual') {
    return addMonthsClamped(date, 12 * periods)
  }
  return addMonthsClamped(date, periods)
}

function calculateFirstPaymentDate(disbursementDate: string, frequency: string): string {
  const date = parseDateOnly(disbursementDate)
  if (!date || !frequency) return ''

  return formatDateOnly(addPaymentPeriods(date, frequency, 1))
}

function calculateTermDays(
  disbursementDate: string,
  frequency: string,
  numberOfPayments: string
): number {
  const payments = Number.parseInt(numberOfPayments, 10)
  if (!frequency || !Number.isFinite(payments) || payments <= 0) return 0

  const disbursement = parseDateOnly(disbursementDate)
  if (!disbursement) {
    return (PAYMENT_FREQUENCY_DAYS[frequency] ?? 30) * payments
  }

  const firstPayment = addPaymentPeriods(new Date(disbursement), frequency, 1)
  const lastPayment = addPaymentPeriods(new Date(firstPayment), frequency, payments - 1)
  return Math.max(0, Math.round((lastPayment.getTime() - disbursement.getTime()) / 86_400_000))
}

export function CreateLoanPage() {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const toast = useToast()
  const { tenant } = useAuth()
  const [searchParams] = useSearchParams()

  const [customerId, setCustomerId] = useState(searchParams.get('customerId') ?? '')
  const [productId, setProductId] = useState('')
  const [principalAmount, setPrincipalAmount] = useState('')
  const [numberOfPayments, setNumberOfPayments] = useState('')
  const [disbursementDate, setDisbursementDate] = useState('')
  const [commissionAmount, setCommissionAmount] = useState('')
  const [customInterestRate, setCustomInterestRate] = useState('')
  const [useCustomRate, setUseCustomRate] = useState(false)
  const [photoPreview, setPhotoPreview] = useState<string | null>(null)
  const [isUploadingPhoto, setIsUploadingPhoto] = useState(false)

  const { data: customers } = useQuery({
    queryKey: ['customers', tenant?.id, 'active'],
    queryFn: () => customersApi.list({ status: 'Active', pageSize: 200 }),
    enabled: !!tenant?.id,
  })

  const { data: products } = useQuery({
    queryKey: ['loan-products', tenant?.id],
    queryFn: () => loanProductsApi.list({ active: true }),
    enabled: !!tenant?.id,
  })

  const selectedProduct = products?.find((p) => p.id === productId)
  const selectedCustomer = customers?.items.find((c) => c.id === customerId)
  const currentPhoto = photoPreview ?? selectedCustomer?.photoUrl ?? null
  const frequency = selectedProduct?.frequency ?? ''
  const firstPaymentDate = calculateFirstPaymentDate(disbursementDate, frequency)
  const termDays = calculateTermDays(disbursementDate, frequency, numberOfPayments)

  const handleCustomerChange = (value: string) => {
    setCustomerId(value)
    setPhotoPreview(null)
  }

  const handlePhotoChange = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0]
    event.target.value = ''
    if (!file || !customerId) return

    if (!file.type.startsWith('image/')) {
      toast.error('Archivo no válido', 'Selecciona una imagen JPG, PNG o WEBP.')
      return
    }

    if (file.size > 5 * 1024 * 1024) {
      toast.error('Imagen demasiado grande', 'La foto no debe superar los 5 MB.')
      return
    }

    const objectUrl = URL.createObjectURL(file)
    setPhotoPreview(objectUrl)
    setIsUploadingPhoto(true)

    try {
      const { photoUrl } = await customersApi.uploadPhoto(customerId, file)
      setPhotoPreview(photoUrl)
      void queryClient.invalidateQueries({ queryKey: ['customers'] })
      toast.success('Foto subida', 'La foto del cliente se guardó correctamente.')
    } catch (error) {
      setPhotoPreview(selectedCustomer?.photoUrl ?? null)
      toast.error('Error al subir foto', getErrorMessage(error))
    } finally {
      URL.revokeObjectURL(objectUrl)
      setIsUploadingPhoto(false)
    }
  }

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
      // Interés plano: la tasa se aplica sobre el capital total.
      const totalInterest = principal * rate
      const totalAmount = principal + totalInterest
      return totalAmount / n
    }

    // Amortización francesa: cuota fija con interés sobre saldo pendiente.
    const periodsPerYear: Record<string, number> = {
      Daily: 365,
      Weekly: 52,
      Biweekly: 24,
      Monthly: 12,
      Quarterly: 4,
      Biannual: 2,
      Annual: 1,
    }
    const r = rate / (periodsPerYear[selectedProduct.frequency] || 12)
    if (r === 0) return principal / n

    const factor = Math.pow(1 + r, n)
    return (principal * r * factor) / (factor - 1)
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
                  onChange={(e) => handleCustomerChange(e.target.value)}
                  options={[
                    { value: '', label: 'Seleccionar cliente...' },
                    ...(customers?.items.map((c) => ({
                      value: c.id,
                      label: `${c.fullName} - ${c.documentNumber}`,
                    })) || []),
                  ]}
                  required
                />

                {customerId && (
                  <div className="flex items-center gap-4 rounded-lg border border-slate-200 bg-slate-50 p-3">
                    <div className="flex h-16 w-16 shrink-0 items-center justify-center overflow-hidden rounded-full bg-slate-200 text-lg font-semibold text-slate-500">
                      {currentPhoto ? (
                        <img
                          src={currentPhoto}
                          alt={`Foto de ${selectedCustomer?.fullName ?? 'cliente'}`}
                          className="h-full w-full object-cover"
                        />
                      ) : (
                        (selectedCustomer?.fullName?.charAt(0) ?? '?').toUpperCase()
                      )}
                    </div>
                    <div className="min-w-0 flex-1">
                      <p className="text-sm font-medium text-slate-700">Foto del cliente</p>
                      <p className="text-xs text-slate-500">
                        JPG, PNG o WEBP. Máximo 5 MB.
                      </p>
                      <input
                        type="file"
                        accept="image/*"
                        onChange={handlePhotoChange}
                        disabled={isUploadingPhoto}
                        className="mt-2 block w-full text-xs text-slate-600 file:mr-3 file:rounded-lg file:border-0 file:bg-brand-600 file:px-3 file:py-1.5 file:text-xs file:font-medium file:text-white hover:file:bg-brand-700 disabled:opacity-60"
                      />
                    </div>
                    {isUploadingPhoto && (
                      <span className="shrink-0 text-xs font-medium text-slate-500">Subiendo…</span>
                    )}
                  </div>
                )}

                <Select
                  label="Producto de Préstamo"
                  value={productId}
                  onChange={(e) => setProductId(e.target.value)}
                  options={[
                    { value: '', label: 'Seleccionar producto...' },
                    ...(products?.map((p) => ({
                      value: p.id,
                      label: `${p.name} - ${formatPercent(p.interestRate)} ${PAYMENT_FREQUENCY_LABELS[p.frequency] ?? p.frequency}`,
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
                      <li>• Frecuencia: {PAYMENT_FREQUENCY_LABELS[selectedProduct.frequency] ?? selectedProduct.frequency}</li>
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

                <Input
                  label="Número de Cuotas"
                  type="number"
                  value={numberOfPayments}
                  onChange={(e) => setNumberOfPayments(e.target.value)}
                  min="1"
                  helperText={
                    frequency
                      ? `Pagos ${PAYMENT_FREQUENCY_HELP[frequency] ?? 'según el producto'}`
                      : 'La frecuencia se toma del producto seleccionado'
                  }
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
                  onChange={(e) => setDisbursementDate(e.target.value)}
                  required
                />

                <Input
                  label="Fecha Primer Pago"
                  type="date"
                  value={firstPaymentDate}
                  disabled
                  helperText="Se calcula automáticamente según la frecuencia del producto"
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
                disabled={
                  !customerId ||
                  !selectedProduct ||
                  !principalAmount ||
                  !numberOfPayments ||
                  !disbursementDate ||
                  !firstPaymentDate
                }
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
