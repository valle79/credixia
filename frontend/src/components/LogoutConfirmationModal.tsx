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
    <Modal isOpen={isOpen} onClose={onClose} title="" size="sm">
      <div className="space-y-6">
        <div className="flex flex-col items-center text-center">
          <div className="flex h-12 w-12 items-center justify-center rounded-full bg-amber-100">
            <LogOut className="h-6 w-6 text-amber-600" />
          </div>
          <h3 className="mt-4 text-lg font-semibold text-slate-900">
            ¿Cerrar sesión?
          </h3>
          <p className="mt-2 text-sm text-slate-600">
            Estás a punto de abandonar tu sesión. Tendrás que iniciar sesión nuevamente para
            acceder a Credixia.
          </p>
        </div>

        <div className="flex flex-col-reverse gap-3 sm:flex-row sm:justify-end">
          <Button
            type="button"
            variant="ghost"
            onClick={onClose}
            disabled={isLoading}
          >
            Cancelar
          </Button>
          <Button
            onClick={onConfirm}
            isLoading={isLoading}
            className="bg-red-600 hover:bg-red-700 focus:ring-red-500"
          >
            Sí, cerrar sesión
          </Button>
        </div>
      </div>
    </Modal>
  )
}
