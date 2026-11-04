
import { useState, useCallback } from 'react'
import type { ToastProps } from '../components/ui/Toast'

let toastId = 0

export function useToast() {
  const [toasts, setToasts] = useState<ToastProps[]>([])

  const show = useCallback(
    (options: Omit<ToastProps, 'id' | 'onClose'>) => {
      const id = `toast-${++toastId}`

      const toast: ToastProps = {
        ...options,
        id,
        onClose: (toastId: string) => {
          setToasts((prev) =>
            prev.filter((toast) => toast.id !== toastId)
          )
        },
      }

      setToasts((prev) => [...prev, toast])

      return id
    },
    []
  )

  const success = useCallback(
    (title: string, message?: string) => {
      return show({
        type: 'success',
        title,
        ...(message !== undefined ? { message } : {}),
      })
    },
    [show]
  )

  const error = useCallback(
    (title: string, message?: string) => {
      return show({
        type: 'error',
        title,
        ...(message !== undefined ? { message } : {}),
      })
    },
    [show]
  )

  const warning = useCallback(
    (title: string, message?: string) => {
      return show({
        type: 'warning',
        title,
        ...(message !== undefined ? { message } : {}),
      })
    },
    [show]
  )

  const info = useCallback(
    (title: string, message?: string) => {
      return show({
        type: 'info',
        title,
        ...(message !== undefined ? { message } : {}),
      })
    },
    [show]
  )

  const close = useCallback((id: string) => {
    setToasts((prev) =>
      prev.filter((toast) => toast.id !== id)
    )
  }, [])

  return {
    toasts,
    show,
    success,
    error,
    warning,
    info,
    close,
  }
}
