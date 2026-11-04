import { useState } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useNavigate } from 'react-router-dom'
import { customersApi } from '../api/customers.api'
import { Card } from '../components/ui/Card'
import { Button } from '../components/ui/Button'
import { Input } from '../components/ui/Input'
import { Select } from '../components/ui/Select'
import { getErrorMessage } from '../api/client'
import { useToast } from '../hooks/useToast'
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

  const createMutation = useMutation({
    mutationFn: (data: CreateCustomerRequest) => customersApi.create(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['customers'] })
      toast.success('Cliente creado', 'El cliente ha sido registrado exitosamente')
      navigate('/customers')
    },
    onError: (error) => {
      toast.error('Error al crear cliente', getErrorMessage(error))
    },
  })

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    const data: CreateCustomerRequest = {
      ...formData,
      type: formData.type as CreateCustomerRequest['type'],
      monthlyIncome: formData.monthlyIncome ? parseFloat(formData.monthlyIncome) : undefined,
      birthDate: formData.birthDate || undefined,
    }
    createMutation.mutate(data)
  }

  const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>) => {
    setFormData((prev) => ({ ...prev, [e.target.name]: e.target.value }))
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
