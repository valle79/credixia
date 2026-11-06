import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useNavigate } from 'react-router-dom'
import { authApi } from '../api/auth.api'
import { useAuthStore } from '../store/auth.store'
import { getErrorMessage } from '../api/client'
import type { LoginRequest } from '../api/types'

export function useAuth() {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const { user, tenant, permissions, isAuthenticated, setSession, clearSession, hasPermission } = useAuthStore()

  const loginMutation = useMutation({
    mutationFn: (data: LoginRequest) => authApi.login(data),
    onSuccess: (response) => {
      if (response.requiresTenantSelection) {
        // Usuario tiene múltiples tenants, mostrar selector
        // Guardamos un estado temporal para permitir acceso a /select-tenant
        useAuthStore.setState({ 
          isAuthenticated: true,
          user: response.user || null,
        })
        navigate('/select-tenant', { state: { tenants: response.tenants }, replace: true })
      } else if (response.session) {
        // Login exitoso con un solo tenant o platform admin
        setSession(
          response.session.user,
          response.session.tenant || null,
          response.session.permissions,
          response.session.accessToken
        )
        
        // Redirigir según el tipo de usuario
        if (response.session.user.isPlatformAdmin && !response.session.tenant) {
          // Super Admin sin tenant → Dashboard de administración
          navigate('/admin', { replace: true })
        } else {
          // Usuario normal o admin con tenant → Dashboard normal
          navigate('/', { replace: true })
        }
      }
    },
  })

  const logoutMutation = useMutation({
    mutationFn: () => authApi.logout(),
    onSettled: () => {
      // Siempre limpiar la sesión, incluso si la API falla
      clearSession()
      queryClient.clear()
      navigate('/login', { replace: true })
    },
  })

  const switchTenantMutation = useMutation({
    mutationFn: (tenantId: string) => authApi.switchTenant(tenantId),
    onSuccess: (response) => {
      setSession(
        response.user,
        response.tenant || null,
        response.permissions,
        response.accessToken
      )
      queryClient.invalidateQueries()
      navigate('/')
    },
  })

  // Query para verificar sesión actual
  // Solo habilitado si hay un accessToken real (sesión completa)
  const { data: currentSession, isLoading: isLoadingSession } = useQuery({
    queryKey: ['auth', 'me'],
    queryFn: () => authApi.me(),
    enabled: isAuthenticated && !!localStorage.getItem('accessToken'),
    retry: false,
    staleTime: 5 * 60 * 1000, // 5 minutos
  })

  return {
    user,
    tenant,
    permissions,
    isAuthenticated,
    isLoadingSession,
    currentSession,
    hasPermission,
    login: loginMutation.mutate,
    logout: logoutMutation.mutate,
    isLoggingOut: logoutMutation.isPending,
    switchTenant: switchTenantMutation.mutate,
    isLoggingIn: loginMutation.isPending,
    loginError: loginMutation.error ? getErrorMessage(loginMutation.error) : null,
  }
}
