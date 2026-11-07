export type CustomerDocumentType = 'DNI' | 'RUC' | 'CE' | 'PASAPORTE'

export interface ValidationResult {
  isValid: boolean
  message?: string
}

/**
 * Calcula la edad exacta en años considerando la fecha completa (año, mes, día)
 */
export function calculateAge(birthDate: Date, referenceDate: Date = new Date()): number {
  let age = referenceDate.getFullYear() - birthDate.getFullYear()
  const monthDiff = referenceDate.getMonth() - birthDate.getMonth()

  // Ajustar si aún no ha cumplido años este año
  if (monthDiff < 0 || (monthDiff === 0 && referenceDate.getDate() < birthDate.getDate())) {
    age--
  }

  return age
}

function parseDateInput(value: string): Date | null {
  const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(value)
  if (!match) return null

  const year = Number(match[1])
  const month = Number(match[2])
  const day = Number(match[3])
  const date = new Date(year, month - 1, day)

  if (
    date.getFullYear() !== year ||
    date.getMonth() !== month - 1 ||
    date.getDate() !== day
  ) {
    return null
  }

  return date
}

/**
 * Valida que una fecha de nacimiento corresponda a una persona de al menos 18 años
 */
export function validateMinimumAge(
  birthDate: string,
  minimumAge: number = 18,
  referenceDate: Date = new Date()
): ValidationResult {
  if (!birthDate) {
    return { isValid: true }
  }

  const birth = parseDateInput(birthDate)
  if (!birth) {
    return {
      isValid: false,
      message: 'Ingresa una fecha de nacimiento válida',
    }
  }

  const today = new Date(
    referenceDate.getFullYear(),
    referenceDate.getMonth(),
    referenceDate.getDate()
  )

  // Validar que no sea fecha futura
  if (birth > today) {
    return {
      isValid: false,
      message: 'La fecha de nacimiento no puede ser futura',
    }
  }

  const age = calculateAge(birth, today)

  if (age < minimumAge) {
    return {
      isValid: false,
      message: `El cliente debe tener al menos ${minimumAge} años para poder registrarse`,
    }
  }

  return { isValid: true }
}

/**
 * Calcula la fecha máxima de nacimiento para una persona que ya cumplió la edad mínima
 * Útil para el atributo max de un input type="date"
 */
export function getMaxBirthDate(
  minimumAge: number = 18,
  referenceDate: Date = new Date()
): string {
  const maxDate = new Date(
    referenceDate.getFullYear() - minimumAge,
    referenceDate.getMonth(),
    referenceDate.getDate()
  )

  const year = String(maxDate.getFullYear())
  const month = String(maxDate.getMonth() + 1).padStart(2, '0')
  const day = String(maxDate.getDate()).padStart(2, '0')

  return `${year}-${month}-${day}`
}

export function normalizeDocumentNumber(documentNumber: string): string {
  return documentNumber.trim().toUpperCase()
}

/**
 * Debe mantenerse alineado con Customer.NormalizeDocumentNumber en el backend.
 */
export function validateDocumentFormat(
  documentType: string,
  documentNumber: string
): ValidationResult {
  const normalized = normalizeDocumentNumber(documentNumber)

  if (!normalized) {
    return {
      isValid: false,
      message: 'El número de documento es requerido',
    }
  }

  if (normalized.length > 20) {
    return {
      isValid: false,
      message: 'El número de documento no puede exceder 20 caracteres',
    }
  }

  const isAsciiDigit = (value: string) => /^[0-9]+$/.test(value)
  const isAsciiAlphanumeric = (value: string) => /^[A-Z0-9]+$/.test(value)

  switch (documentType) {
    case 'DNI':
      return normalized.length === 8 && isAsciiDigit(normalized)
        ? { isValid: true }
        : { isValid: false, message: 'El DNI debe tener exactamente 8 dígitos' }
    case 'RUC':
      return normalized.length === 11 && isAsciiDigit(normalized)
        ? { isValid: true }
        : { isValid: false, message: 'El RUC debe tener exactamente 11 dígitos' }
    case 'CE':
      return normalized.length >= 9 && normalized.length <= 12 && isAsciiAlphanumeric(normalized)
        ? { isValid: true }
        : { isValid: false, message: 'El CE debe tener entre 9 y 12 caracteres alfanuméricos' }
    case 'PASAPORTE':
      return normalized.length >= 6 && normalized.length <= 20 && isAsciiAlphanumeric(normalized)
        ? { isValid: true }
        : { isValid: false, message: 'El pasaporte debe tener entre 6 y 20 caracteres alfanuméricos' }
    default:
      return { isValid: false, message: 'Tipo de documento inválido' }
  }
}
