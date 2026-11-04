
import axios, {
  type AxiosError,
  type InternalAxiosRequestConfig,
} from 'axios'

import type { ApiError } from './types'
import { useAuthStore } from '../store/auth.store'

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || ''

// Cliente principal de la aplicación.
export const apiClient = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json; charset=utf-8',
    Accept: 'application/json',
  },
  withCredentials: true,
})

// Cliente independiente para renovar el token.
// No utiliza el interceptor de respuestas del cliente principal.
const refreshClient = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json; charset=utf-8',
    Accept: 'application/json',
  },
  withCredentials: true,
})

interface RetryableRequestConfig extends InternalAxiosRequestConfig {
  _retry?: boolean
}

interface RefreshResponse {
  accessToken: string
  expiresAt: string
}

// Compartir una única renovación entre todas las solicitudes.
let refreshPromise: Promise<string> | null = null

// Evitar múltiples redirecciones al login.
let isRedirectingToLogin = false

// Rutas que no deben iniciar una renovación automática.
function shouldSkipRefresh(url?: string): boolean {
  const normalizedUrl = (url || '').toLowerCase()

  const excludedRoutes = [
    '/api/v1/auth/login',
    '/api/v1/auth/refresh',
    '/api/v1/auth/forgot-password',
    '/api/v1/auth/reset-password',
    '/api/v1/auth/logout',
  ]

  // También saltar refresh si estamos en select-tenant
  const isInSelectTenant = window.location.pathname === '/select-tenant'

  return isInSelectTenant || excludedRoutes.some((route) =>
    normalizedUrl.includes(route)
  )
}

// Renovar el access token.
function refreshAccessToken(): Promise<string> {
  if (!refreshPromise) {
    refreshPromise = refreshClient
      .post<RefreshResponse>('/api/v1/auth/refresh', {})
      .then(({ data }) => {
        if (!data?.accessToken) {
          throw new Error(
            'El servidor no devolvió un access token válido.'
          )
        }

        // Actualizar el token en Zustand y localStorage.
        useAuthStore.getState().setAccessToken(data.accessToken)

        return data.accessToken
      })
      .finally(() => {
        refreshPromise = null
      })
  }

  return refreshPromise
}

// Limpiar la sesión cuando el refresh token deja de ser válido.
function handleInvalidSession(): void {
  // No limpiar la sesión si estamos en select-tenant (aún no hay sesión completa)
  if (window.location.pathname === '/select-tenant') {
    return
  }

  useAuthStore.getState().clearSession()

  if (
    window.location.pathname !== '/login' &&
    !isRedirectingToLogin
  ) {
    isRedirectingToLogin = true
    window.location.replace('/login')
  }
}

// Interceptor de solicitudes.
apiClient.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('accessToken')

    if (token) {
      config.headers.Authorization = `Bearer ${token}`
    }

    return config
  },
  (error) => Promise.reject(error)
)

// Interceptor de respuestas.
apiClient.interceptors.response.use(
  (response) => response,

  async (error: AxiosError<ApiError>) => {
    // Comprobar errores de conexión.
    if (!error.response) {
      return Promise.reject(
        new Error(
          'Network Error: No se pudo conectar con el servidor.'
        )
      )
    }

    const originalRequest =
      error.config as RetryableRequestConfig | undefined

    // Solo procesar respuestas 401 que puedan renovarse.
    if (
      error.response.status !== 401 ||
      !originalRequest ||
      originalRequest._retry ||
      shouldSkipRefresh(originalRequest.url)
    ) {
      return Promise.reject(error)
    }

    // Evitar repetir indefinidamente la misma solicitud.
    originalRequest._retry = true

    try {
      // Todas las solicitudes esperan la misma renovación.
      const newAccessToken = await refreshAccessToken()

      // Colocar el nuevo token en la solicitud original.
      originalRequest.headers.set(
        'Authorization',
        `Bearer ${newAccessToken}`
      )

      // Reintentar la solicitud original una sola vez.
      return apiClient(originalRequest)

    } catch (refreshError) {
      const refreshStatus = axios.isAxiosError(refreshError)
        ? refreshError.response?.status
        : undefined

      // Limpiar la sesión únicamente cuando el servidor
      // rechaza el refresh token o la respuesta no contiene token.
      if (
        refreshStatus === 401 ||
        refreshStatus === 403 ||
        (
          !axios.isAxiosError(refreshError) &&
          refreshError instanceof Error
        )
      ) {
        handleInvalidSession()
      }

      return Promise.reject(refreshError)
    }
  }
)

// Extraer mensajes de error de la API.
export function getErrorMessage(error: unknown): string {
  if (axios.isAxiosError(error)) {
    // Error de conexión.
    if (!error.response) {
      return 'No se pudo conectar con el servidor. Verifica tu conexión.'
    }

    const apiError = error.response.data as ApiError

    // Errores de validación.
    if (apiError?.errors) {
      const firstError = Object.values(apiError.errors)[0]

      if (Array.isArray(firstError) && firstError.length > 0) {
        return firstError[0]
      }
    }

    return (
      apiError?.detail ||
      apiError?.title ||
      error.message ||
      'Error desconocido'
    )
  }

  if (error instanceof Error) {
    return error.message
  }

  return 'Error desconocido'
}
