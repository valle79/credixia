import { format, formatDistance, parseISO } from 'date-fns'
import { es } from 'date-fns/locale'

type DateInput = string | number | Date

function toDate(value: DateInput): Date {
  if (value instanceof Date) {
    return value
  }

  if (typeof value === 'number') {
    return new Date(value)
  }

  return parseISO(value)
}

export function formatCurrency(
  amount: number | null | undefined,
  currency: string = 'PEN',
): string {
  if (amount === null || amount === undefined) {
    return '—'
  }

  if (!Number.isFinite(amount)) {
    return '—'
  }

  const symbol = currency === 'PEN' ? 'S/' : '$'

  return `${symbol} ${amount.toLocaleString('es-PE', {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  })}`
}

export function formatDate(date: DateInput, formatStr: string = 'dd/MM/yyyy'): string {
  return format(toDate(date), formatStr, { locale: es })
}

export function formatDateTime(date: DateInput): string {
  return format(toDate(date), 'dd/MM/yyyy HH:mm', { locale: es })
}

export function formatTime(date: DateInput): string {
  return format(toDate(date), 'HH:mm:ss')
}

export function formatRelative(value: DateInput, now: DateInput = new Date()): string {
  return formatDistance(toDate(value), toDate(now), {
    addSuffix: true,
    locale: es,
  })
}

export function formatLatency(value?: number | null): string {
  return value === undefined || value === null ? '—' : `${Math.round(value)} ms`
}

export function formatPercent(value: number, decimals: number = 2): string {
  return `${value.toFixed(decimals)}%`
}

export function formatDocumentNumber(type: string, number: string): string {
  if (type === 'DNI' && number.length === 8) {
    return number.replace(/(\d{4})(\d{4})/, '$1-$2')
  }
  if (type === 'RUC' && number.length === 11) {
    return number.replace(/(\d{2})(\d{3})(\d{3})(\d{3})/, '$1-$2-$3-$4')
  }
  return number
}

export function getStatusVariant(status: string): 'default' | 'success' | 'warning' | 'error' | 'info' {
  const statusMap: Record<string, 'default' | 'success' | 'warning' | 'error' | 'info'> = {
    Active: 'success',
    Pending: 'warning',
    Approved: 'info',
    Rejected: 'error',
    Suspended: 'warning',
    Blocked: 'error',
    PaidOff: 'success',
    Cancelled: 'error',
  }
  return statusMap[status] || 'default'
}

export function getStatusLabel(status: string): string {
  const labelMap: Record<string, string> = {
    Active: 'Activo',
    Pending: 'Pendiente',
    Approved: 'Aprobado',
    Rejected: 'Rechazado',
    Suspended: 'Suspendido',
    Blocked: 'Bloqueado',
    PaidOff: 'Pagado',
    Cancelled: 'Cancelado',
    Defaulted: 'En Mora',
    Natural: 'Natural',
    Juridica: 'Jurídica',
  }
  return labelMap[status] || status
}
