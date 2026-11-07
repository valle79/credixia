import { useMemo, useState } from 'react'
import { Navigate, useLocation } from 'react-router-dom'
import { useAuth } from '../hooks/useAuth'
import { Button } from '../components/ui/Button'
import { Card } from '../components/ui/Card'
import { LogoutConfirmationModal } from '../components/LogoutConfirmationModal'
import type { TenantOption } from '../api/types'

export function SelectTenantPage() {
  const location = useLocation()
  const { user, tenant, switchTenant, logout, isLoggingOut } = useAuth()
  const [showLogoutModal, setShowLogoutModal] = useState(false)

  const stateTenants = useMemo(() => {
    const state = location.state as { tenants?: TenantOption[] } | null
    return state?.tenants ?? []
  }, [location.state])

  if (user?.isPlatformAdmin) {
    return <Navigate to="/admin" replace />
  }

  return (
    <main className="min-h-screen bg-slate-50 px-6 py-10">
      <div className="mx-auto max-w-4xl space-y-6">
        <div className="flex items-start justify-between gap-4">
          <div>
            <h1 className="text-2xl font-semibold text-slate-900">
              Selecciona una organización
            </h1>
            <p className="mt-1 text-sm text-slate-600">
              {tenant
                ? `Organización actual: ${tenant.name}`
                : 'Elige una organización para continuar.'}
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
          isLoading={isLoggingOut}
        />

        <Card>
          <h2 className="mb-4 text-lg font-semibold">
            {stateTenants.length > 0 ? 'Tus organizaciones' : 'Organizaciones disponibles'}
          </h2>

          {stateTenants.length > 0 ? (
            <div className="space-y-3">
              {stateTenants.map((option) => (
                <div
                  key={option.id}
                  className="flex flex-wrap items-center justify-between gap-3 rounded-lg border border-slate-200 p-4"
                >
                  <div>
                    <p className="font-medium text-slate-900">{option.name}</p>
                    <p className="text-sm text-slate-500">{option.slug}</p>
                  </div>
                  <Button onClick={() => switchTenant(option.id)}>Entrar</Button>
                </div>
              ))}
            </div>
          ) : (
            <p className="text-sm text-slate-600">
              No tienes organizaciones disponibles. Solicita una invitación a un administrador.
            </p>
          )}
        </Card>
      </div>
    </main>
  )
}
