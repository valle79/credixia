import { useState, useEffect, useRef } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useNavigate } from 'react-router-dom'
import { customersApi } from '../api/customers.api'
import { Card } from '../components/ui/Card'
import { Button } from '../components/ui/Button'
import { Input } from '../components/ui/Input'
import { Select } from '../components/ui/Select'
import { getErrorMessage } from '../api/client'
import { useToast } from '../hooks/useToast'
import { useDebounce } from '../hooks/useDebounce'
import { validateMinimumAge, getMaxBirthDate } from '../lib/validation'
import { ToastContainer } from '../components/ui/ToastContainer'
import type { CreateCustomerRequest } from '../api/types'

export function CreateCustomerPage() {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const toast = useToast()

  const [formData, setFormData] = useState({
    type: 'Natural',
    documentType: 'DNI',
    documentNumber: '',
    fullName: '',
    birthDate: '',
    phone: '',
    email: '',
    address: '',
    district: '',
    province: '',
    department: '',
    occupation: '',
    employer: '',
    monthlyIncome: '',
  })

  const [photoFile, setPhotoFile] = useState<File | null>(null)
  const [photoPreview, setPhotoPreview] = useState<string | null>(null)
  const [photoError, setPhotoError] = useState('')

  // Estado para validación de documento
  const [documentValidation, setDocumentValidation] = useState<{
    status: 'idle' | 'checking' | 'available' | 'duplicate' | 'error'
    message: string
  }>({ status: 'idle', message: '' })

  // Estado para validación de edad
  const [ageValidation, setAgeValidation] = useState<{
    isValid: boolean
    message?: string
  }>({ isValid: true })

  const debouncedDocumentNumber = useDebounce(formData.documentNumber, 500)
  const abortControllerRef = useRef<AbortController | null>(null)

  // Validar documento cuando cambie el número con debounce
  useEffect(() => {
    const validateDocument = async () => {
      // No validar si el documento está vacío o es muy corto
      if (!debouncedDocumentNumber || debouncedDocumentNumber.length < 3) {
        setDocumentValidation({ status: 'idle', message: '' })
        return
      }

      // Cancelar validación anterior si existe
      if (abortControllerRef.current) {
        abortControllerRef.current.abort()
      }

      abortControllerRef.current = new AbortController()
      setDocumentValidation({ status: 'checking', message: 'Verificando documento...' })

      try {
        const result = await customersApi.checkDocumentAvailability(
          formData.documentType,
          debouncedDocumentNumber
        )

        setDocumentValidation({
          status: result.available ? 'available' : 'duplicate',
          message: result.message,
        })
      } catch (error) {
        // Ignorar errores de cancelación
        if (error instanceof Error && error.name === 'CanceledError') {
          return
        }
        setDocumentValidation({
          status: 'error',
          message: 'No se pudo verificar el documento',
        })
      }
    }

    validateDocument()

    // Cleanup
    return () => {
      if (abortControllerRef.current) {
        abortControllerRef.current.abort()
      }
    }
  }, [debouncedDocumentNumber, formData.documentType])

  // Limpiar validación cuando cambia el tipo de documento
  useEffect(() => {
    setDocumentValidation({ status: 'idle', message: '' })
  }, [formData.documentType])

  const createMutation = useMutation({
    mutationFn: (data: CreateCustomerRequest) => customersApi.create(data),
    onSuccess: async (created) => {
      let photoFailed = false

      if (photoFile) {
        try {
          await customersApi.uploadPhoto(created.id, photoFile)
          // Invalidar TODAS las queries de customers para asegurar que se refresquen
          queryClient.invalidateQueries({ queryKey: ['customers'] })
          queryClient.invalidateQueries({ queryKey: ['customer'] })
        } catch (error) {
          photoFailed = true
          toast.error('No se pudo subir la foto', getErrorMessage(error))
        }
      }

      queryClient.invalidateQueries({ queryKey: ['customers'] })

      if (!photoFailed) {
        toast.success('Cliente creado', 'El cliente ha sido registrado exitosamente')
      } else {
        toast.success('Cliente creado', 'El cliente se registró, pero la foto no se pudo subir.')
      }

      navigate('/customers')
    },
    onError: (error) => {
      toast.error('Error al crear cliente', getErrorMessage(error))
    },
  })

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()

    // Validar que el documento no esté duplicado
    if (documentValidation.status === 'duplicate') {
      toast.error('Documento duplicado', documentValidation.message)
      return
    }

    // Validar edad mínima
    if (!ageValidation.isValid) {
      toast.error('Edad inválida', ageValidation.message || 'El cliente debe ser mayor de edad')
      return
    }

    const data: CreateCustomerRequest = {
      ...formData,
      type: formData.type as CreateCustomerRequest['type'],
      monthlyIncome: formData.monthlyIncome ? parseFloat(formData.monthlyIncome) : undefined,
      birthDate: formData.birthDate || undefined,
    }
    createMutation.mutate(data)
  }

  const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>) => {
    const { name, value } = e.target
    
    setFormData((prev) => ({ ...prev, [name]: value }))

    // Validar edad cuando cambia la fecha de nacimiento
    if (name === 'birthDate' && value) {
      const validation = validateMinimumAge(value, 18)
      setAgeValidation(validation)
    } else if (name === 'birthDate' && !value) {
      setAgeValidation({ isValid: true })
    }
  }

  const handlePhotoChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    e.target.value = ''
    if (!file) return

    if (!file.type.startsWith('image/')) {
      setPhotoError('Selecciona una imagen JPG, PNG o WEBP.')
      return
    }

    if (file.size > 5 * 1024 * 1024) {
      setPhotoError('La foto no debe superar los 5 MB.')
      return
    }

    setPhotoError('')
    setPhotoFile(file)
    setPhotoPreview((current) => {
      if (current?.startsWith('blob:')) URL.revokeObjectURL(current)
      return URL.createObjectURL(file)
    })
  }

  const resetPhoto = () => {
    setPhotoFile(null)
    setPhotoPreview((current) => {
      if (current?.startsWith('blob:')) URL.revokeObjectURL(current)
      return null
    })
    setPhotoError('')
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-4">
        <Button variant="ghost" onClick={() => navigate('/customers')}>
          ← Volver
        </Button>
        <div>
          <h1 className="text-2xl font-bold text-slate-900">Nuevo Cliente</h1>
          <p className="mt-1 text-sm text-slate-600">Registra la información del cliente</p>
        </div>
      </div>

      <form onSubmit={handleSubmit}>
        <div className="space-y-6">
          <Card title="Información Personal">
            <div className="grid gap-4 sm:grid-cols-2">
              <div className="flex items-center gap-4 rounded-lg border border-slate-200 bg-slate-50 p-3 sm:col-span-2">
                <div className="flex h-16 w-16 shrink-0 items-center justify-center overflow-hidden rounded-full bg-slate-200 text-lg font-semibold text-slate-500">
                  {photoPreview ? (
                    <img
                      src={photoPreview}
                      alt="Vista previa de la foto del cliente"
                      className="h-full w-full object-cover"
                    />
                  ) : (
                    (formData.fullName.trim().charAt(0) || '?').toUpperCase()
                  )}
                </div>
                <div className="min-w-0 flex-1">
                  <p className="text-sm font-medium text-slate-700">Foto del cliente</p>
                  <p className="text-xs text-slate-500">Opcional. JPG, PNG o WEBP, máximo 5 MB.</p>
                  <input
                    type="file"
                    accept="image/*"
                    onChange={handlePhotoChange}
                    disabled={createMutation.isPending}
                    className="mt-2 block w-full text-xs text-slate-600 file:mr-3 file:rounded-lg file:border-0 file:bg-brand-600 file:px-3 file:py-1.5 file:text-xs file:font-medium file:text-white hover:file:bg-brand-700 disabled:opacity-60"
                  />
                  {photoPreview && (
                    <button
                      type="button"
                      onClick={resetPhoto}
                      className="mt-1 text-xs font-medium text-rose-600 hover:text-rose-700"
                    >
                      Quitar foto
                    </button>
                  )}
                  {photoError && <p className="mt-1 text-xs text-rose-600">{photoError}</p>}
                </div>
              </div>

              <Select
                label="Tipo de Cliente"
                name="type"
                value={formData.type}
                onChange={handleChange}
                options={[
                  { value: 'Natural', label: 'Persona Natural' },
                  { value: 'Juridica', label: 'Persona Jurídica' },
                ]}
                required
              />

              <Select
                label="Tipo de Documento"
                name="documentType"
                value={formData.documentType}
                onChange={handleChange}
                options={[
                  { value: 'DNI', label: 'DNI' },
                  { value: 'RUC', label: 'RUC' },
                  { value: 'CE', label: 'Carnet de Extranjería' },
                  { value: 'PASAPORTE', label: 'Pasaporte' },
                ]}
                required
              />

<Input
  label="Número de Documento"
  name="documentNumber"
  value={formData.documentNumber}
  onChange={handleChange}
  maxLength={20}
  {...(
    documentValidation.status === 'duplicate' ||
    documentValidation.status === 'error'
      ? { error: documentValidation.message }
      : {}
  )}
  {...(
    documentValidation.status === 'checking' ||
    documentValidation.status === 'available'
      ? { helperText: documentValidation.message }
      : {}
  )}
  success={documentValidation.status === 'available'}
  required
/>

<Input
  label="Nombre Completo"
  name="fullName"
  value={formData.fullName}
  onChange={handleChange}
  required
/>

<Input
  label="Fecha de Nacimiento"
  type="date"
  name="birthDate"
  value={formData.birthDate}
  onChange={handleChange}
  max={getMaxBirthDate(18)}
  {...(
    !ageValidation.isValid && ageValidation.message
      ? { error: ageValidation.message }
      : {}
  )}
/>

              <Input
                label="Teléfono"
                type="tel"
                name="phone"
                value={formData.phone}
                onChange={handleChange}
              />

              <Input
                label="Correo Electrónico"
                type="email"
                name="email"
                value={formData.email}
                onChange={handleChange}
              />
            </div>
          </Card>

          <Card title="Dirección">
            <div className="grid gap-4 sm:grid-cols-2">
              <div className="sm:col-span-2">
                <Input
                  label="Dirección"
                  name="address"
                  value={formData.address}
                  onChange={handleChange}
                />
              </div>

              <Input
                label="Distrito"
                name="district"
                value={formData.district}
                onChange={handleChange}
              />

              <Input
                label="Provincia"
                name="province"
                value={formData.province}
                onChange={handleChange}
              />

              <Input
                label="Departamento"
                name="department"
                value={formData.department}
                onChange={handleChange}
              />
            </div>
          </Card>

          <Card title="Información Laboral">
            <div className="grid gap-4 sm:grid-cols-2">
              <Input
                label="Ocupación"
                name="occupation"
                value={formData.occupation}
                onChange={handleChange}
              />

              <Input
                label="Empleador"
                name="employer"
                value={formData.employer}
                onChange={handleChange}
              />

              <Input
                label="Ingreso Mensual (S/)"
                type="number"
                name="monthlyIncome"
                value={formData.monthlyIncome}
                onChange={handleChange}
                step="0.01"
                min="0"
              />
            </div>
          </Card>

          <div className="flex justify-end gap-3">
            <Button
              type="button"
              variant="ghost"
              onClick={() => navigate('/customers')}
            >
              Cancelar
            </Button>
            <Button type="submit" isLoading={createMutation.isPending}>
              Crear Cliente
            </Button>
          </div>
        </div>
      </form>
      <ToastContainer toasts={toast.toasts} onClose={toast.close} />
    </div>
  )
}
