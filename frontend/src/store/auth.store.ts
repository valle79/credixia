
import { create } from 'zustand'
import { persist } from 'zustand/middleware'
import type { SessionUser, TenantSummary } from '../api/types'

interface AuthState {
  user: SessionUser | null
  tenant: TenantSummary | null
  permissions: string[]
  accessToken: string | null
  isAuthenticated: boolean

  setSession: (
    user: SessionUser,
    tenant: TenantSummary | null,
    permissions: string[],
    accessToken: string
  ) => void

  setAccessToken: (accessToken: string) => void

  clearSession: () => void

  updateTenant: (
    tenant: TenantSummary,
    permissions: string[]
  ) => void

  hasPermission: (permission: string) => boolean

  hasAnyPermission: (permissions: string[]) => boolean
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set, get) => ({
      // Estado inicial
      user: null,
      tenant: null,
      permissions: [],
      accessToken: null,
      isAuthenticated: false,

      // Guardar una sesión después del login
      setSession: (user, tenant, permissions, accessToken) => {
        localStorage.setItem('accessToken', accessToken)

        set({
          user,
          tenant,
          permissions,
          accessToken,
          isAuthenticated: true,
        })
      },

      // Actualizar el access token después de renovarlo
      setAccessToken: (accessToken) => {
        localStorage.setItem('accessToken', accessToken)

        set({
          accessToken,
        })
      },

      // Limpiar completamente la sesión
      clearSession: () => {
        localStorage.removeItem('accessToken')

        set({
          user: null,
          tenant: null,
          permissions: [],
          accessToken: null,
          isAuthenticated: false,
        })
      },

      // Actualizar el tenant seleccionado
      updateTenant: (tenant, permissions) => {
        set({
          tenant,
          permissions,
        })
      },

      // Comprobar si el usuario tiene un permiso específico
      hasPermission: (permission) => {
        const { permissions } = get()

        return permissions.includes(permission)
      },

      // Comprobar si tiene al menos uno de los permisos indicados
      hasAnyPermission: (perms) => {
        const { permissions } = get()

        return perms.some((permission) =>
          permissions.includes(permission)
        )
      },
    }),
    {
      name: 'auth-storage',

      // Persistir los datos de sesión, pero no el accessToken.
      // El token se administra por separado en localStorage.
      partialize: (state) => ({
        user: state.user,
        tenant: state.tenant,
        permissions: state.permissions,
        isAuthenticated: state.isAuthenticated,
      }),
    }
  )
)
