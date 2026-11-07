
import { useEffect, type ReactNode } from 'react'
import { X } from 'lucide-react'

interface ModalProps {
  isOpen: boolean
  onClose: () => void
  title: string
  children: ReactNode
  size?: 'sm' | 'md' | 'lg' | 'xl'
  ariaLabel?: string
  dismissible?: boolean
}

const sizes = {
  sm: 'max-w-sm',
  md: 'max-w-md',
  lg: 'max-w-lg',
  xl: 'max-w-xl',
}

export function Modal({
  isOpen,
  onClose,
  title,
  children,
  size = 'md',
  ariaLabel,
  dismissible = true,
}: ModalProps) {
  useEffect(() => {
    if (!isOpen) return

    const previousOverflow = document.body.style.overflow

    const handleEscape = (event: KeyboardEvent) => {
      if (event.key === 'Escape' && dismissible) onClose()
    }

    document.addEventListener('keydown', handleEscape)
    document.body.style.overflow = 'hidden'

    return () => {
      document.removeEventListener('keydown', handleEscape)
      document.body.style.overflow = previousOverflow
    }
  }, [isOpen, onClose, dismissible])

  if (!isOpen) return null

  return (
    <div className="fixed inset-0 z-50 overflow-y-auto">
      {/* Backdrop */}
      <div
        className="animate-modal-backdrop fixed inset-0 bg-slate-950/50 backdrop-blur-sm"
        onClick={() => dismissible && onClose()}
        aria-hidden="true"
      />

      {/* Modal */}
      <div className="flex min-h-full items-center justify-center p-4">
        <div
          role="dialog"
          aria-modal="true"
          aria-label={ariaLabel || title || 'Ventana de diálogo'}
          className={`animate-modal-panel relative w-full ${sizes[size]} rounded-xl bg-white shadow-2xl`}
          onClick={(event) => event.stopPropagation()}
        >
          {/* Close button */}
          {dismissible && (
            <button
              type="button"
              onClick={onClose}
              aria-label="Cerrar ventana"
              className="absolute right-4 top-4 z-10 flex h-8 w-8 items-center justify-center rounded-full text-slate-400 transition-all duration-200 hover:bg-slate-100 hover:text-slate-700 focus:outline-none focus:ring-2 focus:ring-brand-500"
            >
              <X size={18} strokeWidth={2} />
            </button>
          )}

          {/* Header */}
          {title && (
            <div className="px-6 pb-2 pt-6 pr-14">
              <h3 className="text-lg font-semibold tracking-tight text-slate-900">
                {title}
              </h3>
            </div>
          )}

          {/* Content */}
          <div className={title ? 'px-6 pb-6 pt-3' : 'p-6'}>
            {children}
          </div>
        </div>
      </div>
    </div>
  )
}
