
import { LogOut } from 'lucide-react'
import { Modal } from './ui/Modal'
import { Button } from './ui/Button'

interface LogoutConfirmationModalProps {
  isOpen: boolean
  onClose: () => void
  onConfirm: () => void
  isLoading?: boolean
}

export function LogoutConfirmationModal({
  isOpen,
  onClose,
  onConfirm,
  isLoading = false,
}: LogoutConfirmationModalProps) {
  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title=""
      size="sm"
      ariaLabel="Cerrar sesión"
      dismissible={!isLoading}
    >
      <div className="space-y-6 py-2">
        <div className="flex flex-col items-center text-center">
          <div className="flex h-14 w-14 items-center justify-center rounded-full bg-red-50 ring-8 ring-red-50/60">
            <LogOut
              size={25}
              strokeWidth={1.8}
              className="text-red-600"
            />
          </div>

          <h3 className="mt-4 text-lg font-semibold tracking-tight text-slate-900">
            ¿Cerrar sesión?
          </h3>

          <p className="mt-2 max-w-xs text-sm leading-6 text-slate-500">
            Estás a punto de abandonar tu sesión. Tendrás que iniciar sesión
            nuevamente para acceder a Credixia.
          </p>
        </div>

        <div className="flex flex-col-reverse gap-2 sm:flex-row sm:justify-center">
          <Button
            type="button"
            variant="ghost"
            onClick={onClose}
            disabled={isLoading}
            className="w-full sm:flex-1"
          >
            Cancelar
          </Button>

          <Button
            type="button"
            onClick={onConfirm}
            isLoading={isLoading}
            loadingText="Cerrando sesión..."
            disabled={isLoading}
            className="w-full bg-red-600 text-white hover:bg-red-700 focus:ring-red-500 sm:flex-1"
          >
            Sí, cerrar sesión
          </Button>
        </div>
      </div>
    </Modal>
  )
}
