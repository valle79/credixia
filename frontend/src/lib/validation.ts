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

/**
 * Valida que una fecha de nacimiento corresponda a una persona de al menos 18 años
 */
export function validateMinimumAge(birthDate: string, minimumAge: number = 18): {
  isValid: boolean
  message?: string
} {
  if (!birthDate) {
    return { isValid: true }
  }

  const birth = new Date(birthDate)
  const today = new Date()

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
export function getMaxBirthDate(minimumAge: number = 18): string {
  const today = new Date()
  const maxDate = new Date(
    today.getFullYear() - minimumAge,
    today.getMonth(),
    today.getDate()
  )
  return maxDate.toISOString().split('T')[0]
}
