import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { platformApi } from '../api/platform.api'
import { Button } from '../components/ui/Button'
import { Input } from '../components/ui/Input'
import { Card } from '../components/ui/Card'
import { Badge } from '../components/ui/Badge'
import { getErrorMessage } from '../api/client'
import type { CreateTenantResponse } from '../api/types'

export function PlatformAdminDashboard() {
  const queryClient = useQueryClient()
  const [showCreateModal, setShowCreateModal] = useState(false)
  const [createdTenant, setCreatedTenant] = useState<CreateTenantResponse | null>(null)
  const [phoneForWhatsApp, setPhoneForWhatsApp] = useState<string>('')

  const { data: dashboard, isLoading: isLoadingDashboard } = useQuery({
    queryKey: ['platform', 'dashboard'],
    queryFn: () => platformApi.getDashboard(),
  })

  const { data: tenants, isLoading: isLoadingTenants } = useQuery({
    queryKey: ['platform', 'tenants'],
    queryFn: () => platformApi.listTenants(),
  })

  const createTenantMutation = useMutation({
    mutationFn: (data: {
      slug: string
      name: string
      ownerEmail: string
      ownerFullName: string
      ownerPhone: string
    }) => platformApi.createTenant(data),
    onSuccess: (created) => {
      setCreatedTenant(created)
      setPhoneForWhatsApp(created.ownerPhone) // Guardar el teléfono
      setShowCreateModal(false)
      queryClient.invalidateQueries({ queryKey: ['platform'] })
    },
  })

  const toggleStatusMutation = useMutation({
    mutationFn: ({ id, suspend }: { id: string; suspend: boolean }) =>
      platformApi.updateTenantStatus(id, suspend),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['platform'] })
    },
  })

  const sendWhatsAppMutation = useMutation({
    mutationFn: ({ id, phoneNumber, password }: { id: string; phoneNumber: string; password?: string }) =>
      platformApi.sendCredentialsViaWhatsApp(id, phoneNumber, password ?? ''),
    onSuccess: (response) => {
      window.open(response.whatsAppUrl, '_blank')
    },
  })

  const handleCreateTenant = (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault()
    const formData = new FormData(e.currentTarget)
    createTenantMutation.mutate({
      slug: String(formData.get('slug') ?? ''),
      name: String(formData.get('name') ?? ''),
      ownerEmail: String(formData.get('ownerEmail') ?? ''),
      ownerFullName: String(formData.get('ownerFullName') ?? ''),
      ownerPhone: String(formData.get('ownerPhone') ?? ''),
    })
  }

  const copyToClipboard = (text: string) => {
    navigator.clipboard.writeText(text)
  }

  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-50 via-blue-50 to-cyan-50">
      {/* Header */}
      <div className="border-b border-slate-200 bg-white/80 backdrop-blur-sm">
        <div className="mx-auto max-w-7xl px-6 py-6">
          <div className="flex items-center justify-between">
            <div>
              <h1 className="text-3xl font-bold text-slate-900">
                Panel de Administración
              </h1>
              <p className="mt-1 text-sm text-slate-600">
                Gestión de organizaciones y plataforma
              </p>
            </div>
            <Button
              onClick={() => setShowCreateModal(true)}
              className="bg-gradient-to-r from-cyan-500 to-blue-600 hover:from-cyan-600 hover:to-blue-700"
            >
              + Nueva Organización
            </Button>
          </div>
        </div>
      </div>

      <div className="mx-auto max-w-7xl px-6 py-8">
        {/* Stats Cards */}
        {isLoadingDashboard ? (
          <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-4">
            {[...Array(4)].map((_, i) => (
              <Card key={i} className="animate-pulse">
                <div className="h-24 bg-slate-200 rounded"></div>
              </Card>
            ))}
          </div>
        ) : dashboard ? (
          <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-4">
            <StatCard
              title="Organizaciones"
              value={dashboard.totalTenants}
              subtitle={`${dashboard.activeTenants} activas`}
              icon="🏢"
              color="blue"
            />
            <StatCard
              title="Usuarios Totales"
              value={dashboard.totalUsers}
              subtitle={`${dashboard.activeUsers} activos`}
              icon="👥"
              color="cyan"
            />
            <StatCard
              title="Miembros"
              value={dashboard.totalMembers}
              subtitle="En organizaciones"
              icon="👤"
              color="indigo"
            />
            <StatCard
              title="Suspendidas"
              value={dashboard.suspendedTenants}
              subtitle="Organizaciones"
              icon="⏸️"
              color="amber"
            />
          </div>
        ) : null}

        {/* Organizations List */}
        <div className="mt-8">
          <Card>
            <div className="border-b border-slate-200 px-6 py-4">
              <h2 className="text-lg font-semibold text-slate-900">
                Organizaciones Registradas
              </h2>
            </div>

            {isLoadingTenants ? (
              <div className="px-6 py-12 text-center text-slate-500">
                Cargando...
              </div>
            ) : tenants && tenants.length > 0 ? (
              <div className="divide-y divide-slate-200">
                {tenants.map((tenant) => (
                  <div
                    key={tenant.id}
                    className="px-6 py-4 hover:bg-slate-50 transition-colors"
                  >
                    <div className="flex items-center justify-between gap-4">
                      <div className="flex-1">
                        <div className="flex items-center gap-3">
                          <h3 className="font-semibold text-slate-900">
                            {tenant.name}
                          </h3>
                          <Badge
                            variant={
                              tenant.status === 'Active' ? 'success' : 'warning'
                            }
                          >
                            {tenant.status === 'Active' ? 'Activa' : 'Suspendida'}
                          </Badge>
                        </div>
                        <div className="mt-1 flex items-center gap-4 text-sm text-slate-600">
                          <span>@{tenant.slug}</span>
                          <span>•</span>
                          <span>{tenant.activeMembers} miembro(s)</span>
                          <span>•</span>
                          <span>
                            {new Date(tenant.createdAt).toLocaleDateString('es-ES')}
                          </span>
                        </div>
                      </div>
                      <div className="flex gap-2">
                        <Button
                          variant="secondary"
                          size="sm"
                          onClick={() => {
                            const phone = prompt('Ingresa el número de WhatsApp (con código de país, ej: +51987654321):')
                            if (phone) {
                              sendWhatsAppMutation.mutate({ id: tenant.id, phoneNumber: phone })
                            }
                          }}
                          disabled={sendWhatsAppMutation.isPending}
                        >
                          📱 WhatsApp
                        </Button>
                        <Button
                          variant="secondary"
                          size="sm"
                          onClick={() =>
                            toggleStatusMutation.mutate({
                              id: tenant.id,
                              suspend: tenant.status === 'Active',
                            })
                          }
                          disabled={toggleStatusMutation.isPending}
                        >
                          {tenant.status === 'Active' ? 'Suspender' : 'Activar'}
                        </Button>
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            ) : (
              <div className="px-6 py-12 text-center">
                <p className="text-slate-600">No hay organizaciones registradas</p>
                <Button
                  onClick={() => setShowCreateModal(true)}
                  className="mt-4"
                >
                  Crear Primera Organización
                </Button>
              </div>
            )}
          </Card>
        </div>
      </div>

      {/* Create Modal */}
      {showCreateModal && (
        <Modal
          onClose={() => setShowCreateModal(false)}
          title="Crear Nueva Organización"
        >
          <form onSubmit={handleCreateTenant} className="space-y-4">
            <Input
              label="Identificador (slug)"
              name="slug"
              placeholder="mi-empresa"
              required
              pattern="[a-z0-9-]+"
              title="Solo letras minúsculas, números y guiones"
            />
            <Input
              label="Nombre de la Organización"
              name="name"
              placeholder="Mi Empresa S.A.C."
              required
            />
            <Input
              label="Nombre Completo del Propietario"
              name="ownerFullName"
              placeholder="Juan Pérez García"
              required
            />
            <Input
              label="Email del Propietario"
              name="ownerEmail"
              type="email"
              placeholder="juan@ejemplo.com"
              required
            />
            <Input
              label="Teléfono del Propietario"
              name="ownerPhone"
              placeholder="987654321"
              required
              pattern="[0-9]{9,15}"
              title="Ingrese solo números (9 dígitos para Perú, se agregará +51 automáticamente)"
              helperText="Solo números. Se agregará +51 automáticamente para Perú"
            />

            {createTenantMutation.error && (
              <div className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
                {getErrorMessage(createTenantMutation.error)}
              </div>
            )}

            <div className="flex gap-3 pt-2">
              <Button
                type="submit"
                disabled={createTenantMutation.isPending}
                className="flex-1"
              >
                {createTenantMutation.isPending ? 'Creando...' : 'Crear Organización'}
              </Button>
              <Button
                type="button"
                variant="secondary"
                onClick={() => setShowCreateModal(false)}
              >
                Cancelar
              </Button>
            </div>
          </form>
        </Modal>
      )}

      {/* Success Modal */}
      {createdTenant && (
        <Modal
          onClose={() => setCreatedTenant(null)}
          title="✅ Organización Creada"
        >
          <div className="space-y-4">
            <div className="rounded-lg border border-green-200 bg-green-50 px-4 py-3">
              <p className="text-sm font-medium text-green-800">
                La organización <strong>{createdTenant.name}</strong> ha sido creada
                exitosamente.
              </p>
            </div>

            <div className="space-y-3 rounded-lg border border-slate-200 bg-slate-50 p-4">
              <h3 className="font-semibold text-slate-900">
                Credenciales Generadas:
              </h3>
              <div className="space-y-2">
                <div className="flex items-center justify-between gap-2 rounded bg-white px-3 py-2">
                  <span className="text-sm text-slate-600">Email:</span>
                  <div className="flex items-center gap-2">
                    <code className="text-sm font-mono text-slate-900">
                      {createdTenant.ownerEmail}
                    </code>
                    <button
                      onClick={() => copyToClipboard(createdTenant.ownerEmail)}
                      className="text-cyan-600 hover:text-cyan-700"
                      title="Copiar"
                    >
                      📋
                    </button>
                  </div>
                </div>
                <div className="flex items-center justify-between gap-2 rounded bg-white px-3 py-2">
                  <span className="text-sm text-slate-600">Contraseña:</span>
                  <div className="flex items-center gap-2">
                    <code className="text-sm font-mono font-bold text-slate-900">
                      {createdTenant.generatedPassword}
                    </code>
                    <button
                      onClick={() => copyToClipboard(createdTenant.generatedPassword)}
                      className="text-cyan-600 hover:text-cyan-700"
                      title="Copiar"
                    >
                      📋
                    </button>
                  </div>
                </div>
              </div>
            </div>

            <div className="text-sm text-slate-600">
              ✉️ Las credenciales también han sido enviadas al correo del propietario.
            </div>

            <Button
              onClick={() => {
                if (phoneForWhatsApp && createdTenant) {
                  sendWhatsAppMutation.mutate({
                    id: createdTenant.id,
                    phoneNumber: phoneForWhatsApp,
                    password: createdTenant.generatedPassword,
                  })
                }
              }}
              className="w-full bg-green-600 hover:bg-green-700"
              disabled={sendWhatsAppMutation.isPending || !phoneForWhatsApp}
            >
              📱 Enviar por WhatsApp
            </Button>

            <Button
              onClick={() => setCreatedTenant(null)}
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

function StatCard({
  title,
  value,
  subtitle,
  icon,
  color,
}: {
  title: string
  value: number
  subtitle: string
  icon: string
  color: 'blue' | 'cyan' | 'indigo' | 'amber'
}) {
  const colorClasses = {
    blue: 'from-blue-500 to-cyan-500',
    cyan: 'from-cyan-500 to-teal-500',
    indigo: 'from-indigo-500 to-purple-500',
    amber: 'from-amber-500 to-orange-500',
  }

  return (
    <Card className="overflow-hidden">
      <div
        className={`h-2 w-full bg-gradient-to-r ${colorClasses[color]}`}
      ></div>
      <div className="p-6">
        <div className="flex items-center justify-between">
          <span className="text-4xl">{icon}</span>
          <div className="text-right">
            <div className="text-3xl font-bold text-slate-900">{value}</div>
            <div className="text-sm text-slate-600">{title}</div>
          </div>
        </div>
        <div className="mt-3 text-xs text-slate-500">{subtitle}</div>
      </div>
    </Card>
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
      <div className="w-full max-w-lg rounded-xl border border-slate-200 bg-white shadow-2xl">
        <div className="border-b border-slate-200 px-6 py-4">
          <div className="flex items-center justify-between">
            <h2 className="text-xl font-semibold text-slate-900">{title}</h2>
            <button
              onClick={onClose}
              className="text-slate-400 hover:text-slate-600"
            >
              ✕
            </button>
          </div>
        </div>
        <div className="px-6 py-6">{children}</div>
      </div>
    </div>
  )
}
