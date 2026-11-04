import { Component, type ErrorInfo, type ReactNode } from 'react'
import { AlertIcon } from './icons'
import { buttonStyles } from './ui/buttonStyles'

interface ErrorBoundaryProps {
  children: ReactNode
}

interface ErrorBoundaryState {
  error: Error | null
}

export class ErrorBoundary extends Component<
  ErrorBoundaryProps,
  ErrorBoundaryState
> {
  override state: ErrorBoundaryState = { error: null }

  static getDerivedStateFromError(error: Error): ErrorBoundaryState {
    return { error }
  }

  override componentDidCatch(error: Error, info: ErrorInfo) {
    console.error('Unhandled UI error', error, info.componentStack)
  }

  override render() {
    if (this.state.error) {
      return (
        <div className="grid min-h-screen place-items-center bg-slate-50 p-6">
          <div className="w-full max-w-md rounded-2xl border border-slate-200 bg-white p-6 text-center shadow-card">
            <span className="mx-auto grid size-11 place-items-center rounded-2xl bg-rose-50 text-rose-600">
              <AlertIcon className="size-5" />
            </span>
            <h1 className="mt-4 text-lg font-semibold text-slate-950">
              Algo salió mal
            </h1>
            <p className="mt-1 text-sm leading-relaxed text-slate-500">
              La interfaz encontró un error inesperado. Recarga la página para
              continuar.
            </p>
            <button
              type="button"
              className={buttonStyles('primary', 'md', 'mt-5')}
              onClick={() => window.location.reload()}
            >
              Recargar
            </button>
          </div>
        </div>
      )
    }

    return this.props.children
  }
}
