import { useMemo, useState } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { platformApi } from '../api/platform.api'
import { useAuth } from '../hooks/useAuth'
import { Button } from '../components/ui/Button'
import { Input } from '../components/ui/Input'
import { Badge } from '../components/ui/Badge'
import { Card } from '../components/ui/Card'
import { LogoutConfirmationModal } from '../components/LogoutConfirmationModal'
import type { TenantOption } from '../api/types'

export function SelectTenantPage() {
  const location = useLocation()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const { user, tenant, switchTenant, logout } = useAuth()
  const [showLogoutModal, setShowLogoutModal] = useState(false)

  const stateTenants = useMemo(() => {
    const state = location.state as { tenants?: TenantOption[] } | null
    return state?.tenants ?? []
  }, [location.state])

  const isPlatformAdmin = !!user?.isPlatformAdmin

  const { data: platformTenants, isLoading } = useQuery({
    queryKey: ['platform', 'tenants'],
    queryFn: () => platformApi.listTenants(),
    enabled: isPlatformAdmin && user !== null,
    retry: false,
  })

  const tenants = stateTenants.length > 0 ? stateTenants : (platformTenants ?? [])

  const createTenantMutation = useMutation({
    mutationFn: (data: { slug: string; name: string; ownerEmail: string }) =>
      platformApi.createTenant({
        ...data,
        ownerFullName: data.name,
        ownerPhone: '',
      }),
    onSuccess: (created) => {
      queryClient.invalidateQueries({ queryKey: ['platform', 'tenants'] })
      switchTenant(created.id)
    },
  })

  const toggleStatusMutation = useMutation({
    mutationFn: ({ id, suspend }: { id: string; suspend: boolean }) =>
      platformApi.updateTenantStatus(id, suspend),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['platform', 'tenants'] })
    },
  })

  const handleCreateTenant = (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault()
    const formData = new FormData(e.currentTarget)
    createTenantMutation.mutate({
      slug: String(formData.get('slug') ?? ''),
      name: String(formData.get('name') ?? ''),
      ownerEmail: String(formData.get('ownerEmail') ?? ''),
    })
  }

  return (
    <main className="min-h-screen bg-slate-50 px-6 py-10">
      <div className="mx-auto max-w-4xl space-y-6">
        <div className="flex items-start justify-between gap-4">
          <div>
            <h1 className="text-2xl font-semibold text-slate-900">
              {stateTenants.length > 0 ? 'Selecciona una organización' : 'Organizaciones'}
            </h1>
            <p className="mt-1 text-sm text-slate-600">
              {tenant
                ? `Organización actual: ${tenant.name}`
                : 'Elige una organización para continuar o crea una nueva.'}
            </p>
          </div>
          <Button variant="ghost" onClick={() => setShowLogoutModal(true)}>
            Cerrar sesión
          </Button>
        </div>

        <LogoutConfirmationModal
          isOpen={showLogoutModal}
          onClose={() => setShowLogoutModal(false)}
          onConfirm={logout}
          isLoading={false}
        />

        {stateTenants.length > 0 && (
          <Card>
            <h2 className="mb-4 text-lg font-semibold">Tus organizaciones</h2>
            <div className="space-y-3">
              {tenants.map((option) => (
                <div
                  key={option.id}
                  className="flex items-center justify-between rounded-lg border p-4"
                >
                  <div>
                    <p className="font-medium text-slate-900">{option.name}</p>
                    <p className="text-sm text-slate-500">{option.slug}</p>
                  </div>
                  <Button onClick={() => switchTenant(option.id)}>Entrar</Button>
                </div>
              ))}
            </div>
          </Card>
        )}

        {isPlatformAdmin && stateTenants.length === 0 && (
          <>
            <Card>
              <h2 className="mb-4 text-lg font-semibold">Organizaciones existentes</h2>
              {isLoading ? (
                <p className="text-sm text-slate-500">Cargando...</p>
              ) : tenants.length > 0 ? (
                <div className="space-y-3">
                  {platformTenants?.map((item) => (
                    <div
                      key={item.id}
                      className="flex flex-wrap items-center justify-between gap-3 rounded-lg border p-4"
                    >
                      <div>
                        <div className="flex items-center gap-2">
                          <p className="font-medium text-slate-900">{item.name}</p>
                          <Badge variant={item.status === 'Active' ? 'success' : 'warning'}>
                            {item.status}
                          </Badge>
                        </div>
                        <p className="text-sm text-slate-500">
                          {item.slug} · {item.activeMembers} miembro(s)
                        </p>
                      </div>
                      <div className="flex gap-2">
                        <Button
                          variant="secondary"
                          size="sm"
                          onClick={() =>
                            toggleStatusMutation.mutate({
                              id: item.id,
                              suspend: item.status === 'Active',
                            })
                          }
                          disabled={toggleStatusMutation.isPending}
                        >
                          {item.status === 'Active' ? 'Suspender' : 'Reactivar'}
                        </Button>
                        <Button size="sm" onClick={() => switchTenant(item.id)}>
                          Entrar
                        </Button>
                      </div>
                    </div>
                  ))}
                </div>
              ) : (
                <p className="text-sm text-slate-500">No hay organizaciones registradas.</p>
              )}
            </Card>

            <Card>
              <h2 className="mb-4 text-lg font-semibold">Crear organización</h2>
              <form onSubmit={handleCreateTenant} className="space-y-4">
                <div className="grid gap-4 md:grid-cols-2">
                  <Input
                    label="Identificador (slug)"
                    name="slug"
                    placeholder="mi-empresa"
                    required
                  />
                  <Input
                    label="Nombre"
                    name="name"
                    placeholder="Mi Empresa S.A.C."
                    required
                  />
                </div>
                <Input
                  label="Email del propietario"
                  name="ownerEmail"
                  type="email"
                  defaultValue={user?.email}
                  required
                />
                <div className="flex gap-3">
                  <Button type="submit" disabled={createTenantMutation.isPending}>
                    {createTenantMutation.isPending ? 'Creando...' : 'Crear y entrar'}
                  </Button>
                  <Button
                    type="button"
                    variant="secondary"
                    onClick={() => navigate('/')}
                  >
                    Volver
                  </Button>
                </div>
                {createTenantMutation.error && (
                  <p className="text-sm text-red-600">
                    No se pudo crear la organización. Verifica que el identificador no esté en uso.
                  </p>
                )}
              </form>
            </Card>
          </>
        )}

        {!isPlatformAdmin && stateTenants.length === 0 && (
          <Card>
            <p className="text-sm text-slate-600">
              No tienes organizaciones disponibles. Solicita una invitación a un administrador.
            </p>
          </Card>
        )}
      </div>
    </main>
  )
}
