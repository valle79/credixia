import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import {
  getMaxBirthDate,
  normalizeDocumentNumber,
  validateDocumentFormat,
  validateMinimumAge,
} from './validation'

describe('age validation', () => {
  beforeEach(() => {
    vi.useFakeTimers()
    vi.setSystemTime(new Date(2026, 10, 6, 12))
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  it('accepts a customer who turns 18 today', () => {
    expect(validateMinimumAge('2008-11-06')).toEqual({ isValid: true })
  })

  it('rejects a customer one day short of 18', () => {
    expect(validateMinimumAge('2008-11-07')).toEqual({
      isValid: false,
      message: 'El cliente debe tener al menos 18 años para poder registrarse',
    })
  })

  it('rejects future and invalid birth dates', () => {
    expect(validateMinimumAge('2026-11-07')).toEqual({
      isValid: false,
      message: 'La fecha de nacimiento no puede ser futura',
    })

    expect(validateMinimumAge('2026-02-30')).toEqual({
      isValid: false,
      message: 'Ingresa una fecha de nacimiento válida',
    })
  })

  it('builds the max date without UTC date shifts', () => {
    expect(getMaxBirthDate(18)).toBe('2008-11-06')
  })
})

describe('document validation', () => {
  it('normalizes document numbers', () => {
    expect(normalizeDocumentNumber('  ab-123  ')).toBe('AB-123')
  })

  it('validates the supported document formats', () => {
    expect(validateDocumentFormat('DNI', '12345678')).toEqual({ isValid: true })
    expect(validateDocumentFormat('RUC', '12345678901')).toEqual({ isValid: true })
    expect(validateDocumentFormat('CE', 'AB1234567')).toEqual({ isValid: true })
    expect(validateDocumentFormat('PASAPORTE', 'AB1234')).toEqual({ isValid: true })
  })

  it('rejects incomplete or invalid document formats', () => {
    expect(validateDocumentFormat('DNI', '1234').isValid).toBe(false)
    expect(validateDocumentFormat('RUC', '12345678').isValid).toBe(false)
    expect(validateDocumentFormat('CE', '12345678').isValid).toBe(false)
    expect(validateDocumentFormat('PASAPORTE', 'AB-123').isValid).toBe(false)
  })
})
