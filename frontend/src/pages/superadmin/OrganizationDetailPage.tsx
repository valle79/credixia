import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useParams, useNavigate } from 'react-router-dom'
import { platformApi } from '../../api/platform.api'
import { Button } from '../../components/ui/Button'
import { Input } from '../../components/ui/Input'
import { Card } from '../../components/ui/Card'
import { Badge } from '../../components/ui/Badge'
import { getErrorMessage } from '../../api/client'

export function OrganizationDetailPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [isEditing, setIsEditing] = useState(false)
  const [showPasswordReset, setShowPasswordReset] = useState(false)
  const [showPasswordModal, setShowPasswordModal] = useState(false)
  const [customPassword, setCustomPassword] = useState('')
  const [newPassword, setNewPassword] = useState<string | null>(null)

  const { data: organization, isLoading } = useQuery({
    queryKey: ['platform', 'tenants', id],
    queryFn: () => platformApi.getTenantDetails(id!),
    enabled: !!id,
  })

  const updateMutation = useMutation({
    mutationFn: (data: { name: string; ownerFullName: string; ownerPhone: string }) =>
      platformApi.updateTenant(id!, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['platform', 'tenants', id] })
      queryClient.invalidateQueries({ queryKey: ['platform', 'tenants'] })
      setIsEditing(false)
    },
  })

  const resetPasswordMutation = useMutation({
    mutationFn: (newPwd?: string) => platformApi.resetOwnerPassword(id!, newPwd),
    onSuccess: (response) => {
      setNewPassword(response.newPassword)
      setShowPasswordReset(true)
      setShowPasswordModal(false)
      setCustomPassword('')
    },
  })

  const handleUpdate = (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault()
    const formData = new FormData(e.currentTarget)
    updateMutation.mutate({
      name: String(formData.get('name') ?? ''),
      ownerFullName: String(formData.get('ownerFullName') ?? ''),
      ownerPhone: String(formData.get('ownerPhone') ?? ''),
    })
  }

  const handleOpenPasswordModal = () => {
    setShowPasswordModal(true)
    setCustomPassword('')
  }

  const handleResetPassword = (useCustom: boolean) => {
    if (useCustom) {
      if (!customPassword || customPassword.length < 6) {
        alert('La contraseña debe tener al menos 6 caracteres')
        return
      }
      resetPasswordMutation.mutate(customPassword)
    } else {
      resetPasswordMutation.mutate()
    }
  }

  const copyToClipboard = (text: string) => {
    navigator.clipboard.writeText(text)
  }

  if (isLoading) {
    return (
      <div className="space-y-6">
        <div className="flex items-center gap-4">
          <Button variant="ghost" size="sm" onClick={() => navigate('/admin/organizations')}>
            ← Volver
          </Button>
        </div>
        <div className="text-center py-12">
          <p className="text-slate-600">Cargando...</p>
        </div>
      </div>
    )
  }

  if (!organization) {
    return (
      <div className="space-y-6">
        <div className="flex items-center gap-4">
          <Button variant="ghost" size="sm" onClick={() => navigate('/admin/organizations')}>
            ← Volver
          </Button>
        </div>
        <div className="text-center py-12">
          <p className="text-slate-600">Organización no encontrada</p>
        </div>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-4">
          <Button variant="ghost" size="sm" onClick={() => navigate('/admin/organizations')}>
            ← Volver
          </Button>
          <div>
            <h1 className="text-2xl font-bold text-slate-900">{organization.name}</h1>
            <p className="mt-1 text-sm text-slate-600">@{organization.slug}</p>
          </div>
          <Badge variant={organization.status === 'Active' ? 'success' : 'warning'}>
            {organization.status === 'Active' ? 'Activa' : 'Suspendida'}
          </Badge>
        </div>
        <div className="flex items-center gap-2">
          {!isEditing && (
            <Button onClick={() => setIsEditing(true)}>
              ✏️ Editar
            </Button>
          )}
        </div>
      </div>

      {/* Información General */}
      <Card>
        <div className="border-b border-slate-200 px-6 py-4">
          <h2 className="text-lg font-semibold text-slate-900">Información General</h2>
        </div>
        <div className="p-6">
          {isEditing ? (
            <form onSubmit={handleUpdate} className="space-y-4">
              <Input
                label="Nombre de la Organización"
                name="name"
                defaultValue={organization.name}
                required
              />
              <Input
                label="Identificador (slug)"
                value={organization.slug}
                disabled
                helperText="El identificador no puede ser modificado"
              />
              <Input
                label="Nombre Completo del Propietario"
                name="ownerFullName"
                defaultValue={organization.ownerFullName}
                required
              />
              <Input
                label="Teléfono del Propietario"
                name="ownerPhone"
                defaultValue={organization.ownerPhone}
                placeholder="987654321"
                pattern="[0-9]{9,15}"
                helperText="Solo números. Se agregará +51 automáticamente"
              />
              {updateMutation.error && (
                <div className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
                  {getErrorMessage(updateMutation.error)}
                </div>
              )}
              <div className="flex gap-3 pt-2">
                <Button type="submit" disabled={updateMutation.isPending}>
                  {updateMutation.isPending ? 'Guardando...' : 'Guardar Cambios'}
                </Button>
                <Button
                  type="button"
                  variant="secondary"
                  onClick={() => setIsEditing(false)}
                >
                  Cancelar
                </Button>
              </div>
            </form>
          ) : (
            <div className="grid gap-6 md:grid-cols-2">
              <div>
                <label className="text-xs font-medium text-slate-500 uppercase">
                  Nombre
                </label>
                <p className="mt-1 text-sm text-slate-900">{organization.name}</p>
              </div>
              <div>
                <label className="text-xs font-medium text-slate-500 uppercase">
                  Identificador
                </label>
                <p className="mt-1 text-sm font-mono text-slate-900">@{organization.slug}</p>
              </div>
              <div>
                <label className="text-xs font-medium text-slate-500 uppercase">
                  Miembros Activos
                </label>
                <p className="mt-1 text-sm text-slate-900">{organization.memberCount}</p>
              </div>
              <div>
                <label className="text-xs font-medium text-slate-500 uppercase">
                  Fecha de Creación
                </label>
                <p className="mt-1 text-sm text-slate-900">
                  {new Date(organization.createdAt).toLocaleDateString('es-PE', {
                    year: 'numeric',
                    month: 'long',
                    day: 'numeric',
                  })}
                </p>
              </div>
            </div>
          )}
        </div>
      </Card>

      {/* Información del Propietario */}
      <Card>
        <div className="border-b border-slate-200 px-6 py-4">
          <div className="flex items-center justify-between">
            <h2 className="text-lg font-semibold text-slate-900">Propietario</h2>
            <Button
              size="sm"
              variant="secondary"
              onClick={handleOpenPasswordModal}
              disabled={resetPasswordMutation.isPending}
            >
              🔑 Cambiar Contraseña
            </Button>
          </div>
        </div>
        <div className="p-6 space-y-4">
          <div className="grid gap-6 md:grid-cols-2">
            <div>
              <label className="text-xs font-medium text-slate-500 uppercase">
                Nombre Completo
              </label>
              <p className="mt-1 text-sm text-slate-900">{organization.ownerFullName}</p>
            </div>
            <div>
              <label className="text-xs font-medium text-slate-500 uppercase">
                Email
              </label>
              <p className="mt-1 text-sm font-mono text-slate-900">{organization.ownerEmail}</p>
            </div>
            <div>
              <label className="text-xs font-medium text-slate-500 uppercase">
                Teléfono
              </label>
              <p className="mt-1 text-sm text-slate-900">{organization.ownerPhone}</p>
            </div>
          </div>
        </div>
      </Card>

      {/* Modal de Cambio de Contraseña */}
      {showPasswordModal && (
        <Modal
          onClose={() => setShowPasswordModal(false)}
          title="🔑 Cambiar Contraseña"
        >
          <div className="space-y-4">
            <div className="rounded-lg border border-blue-200 bg-blue-50 px-4 py-3">
              <p className="text-sm text-blue-800">
                Puedes generar una contraseña automática segura o establecer una contraseña personalizada.
              </p>
            </div>

            <div className="space-y-3">
              <Input
                label="Contraseña Personalizada (opcional)"
                type="text"
                value={customPassword}
                onChange={(e) => setCustomPassword(e.target.value)}
                placeholder="Dejar vacío para generar automáticamente"
                helperText="Mínimo 6 caracteres. Si está vacío, se generará automáticamente."
              />
            </div>

            {resetPasswordMutation.error && (
              <div className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
                {getErrorMessage(resetPasswordMutation.error)}
              </div>
            )}

            <div className="flex gap-3">
              <Button
                onClick={() => handleResetPassword(false)}
                disabled={resetPasswordMutation.isPending}
                className="flex-1"
              >
                {resetPasswordMutation.isPending ? 'Generando...' : '🎲 Generar Automática'}
              </Button>
              <Button
                onClick={() => handleResetPassword(true)}
                disabled={resetPasswordMutation.isPending || !customPassword}
                className="flex-1 bg-purple-600 hover:bg-purple-700"
              >
                {resetPasswordMutation.isPending ? 'Cambiando...' : '✓ Usar Personalizada'}
              </Button>
            </div>

            <Button
              onClick={() => setShowPasswordModal(false)}
              variant="secondary"
              className="w-full"
            >
              Cancelar
            </Button>
          </div>
        </Modal>
      )}

      {/* Modal de Nueva Contraseña */}
      {showPasswordReset && newPassword && (
        <Modal
          onClose={() => setShowPasswordReset(false)}
          title="🔑 Contraseña Cambiada"
        >
          <div className="space-y-4">
            <div className="rounded-lg border border-green-200 bg-green-50 px-4 py-3">
              <p className="text-sm font-medium text-green-800">
                La contraseña del propietario ha sido cambiada exitosamente.
              </p>
            </div>

            <div className="rounded-lg border border-slate-200 bg-slate-50 p-4">
              <h3 className="font-semibold text-slate-900">Nueva Contraseña:</h3>
              <div className="mt-3 flex items-center justify-between gap-2 rounded bg-white px-3 py-2">
                <code className="text-sm font-mono font-bold text-slate-900">
                  {newPassword}
                </code>
                <button
                  onClick={() => copyToClipboard(newPassword)}
                  className="text-purple-600 hover:text-purple-700"
                  title="Copiar"
                >
                  📋
                </button>
              </div>
            </div>

            <div className="text-sm text-slate-600">
              ⚠️ Asegúrate de compartir esta contraseña con el propietario de forma segura.
            </div>

            <Button
              onClick={() => setShowPasswordReset(false)}
              variant="secondary"
              className="w-full"
            >
              Cerrar
            </Button>
          </div>
        </Modal>
      )}
    </div>
  )
}

function Modal({
  children,
  onClose,
  title,
}: {
  children: React.ReactNode
  onClose: () => void
  title: string
}) {
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4">
      <div
        className="relative w-full max-w-lg rounded-xl bg-white shadow-2xl"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="flex items-center justify-between border-b border-slate-200 px-6 py-4">
          <h2 className="text-lg font-semibold text-slate-900">{title}</h2>
          <button
            onClick={onClose}
            className="text-slate-400 hover:text-slate-600"
          >
            ✕
          </button>
        </div>
        <div className="p-6">{children}</div>
      </div>
    </div>
  )
}
