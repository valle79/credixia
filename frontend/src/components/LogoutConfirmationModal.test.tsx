import { fireEvent, render, screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { LogoutConfirmationModal } from './LogoutConfirmationModal'

describe('LogoutConfirmationModal', () => {
  it('shows the confirmation copy and confirms logout', () => {
    const onConfirm = vi.fn()

    render(
      <LogoutConfirmationModal
        isOpen
        onClose={vi.fn()}
        onConfirm={onConfirm}
      />,
    )

    expect(screen.getByText('¿Cerrar sesión?')).toBeInTheDocument()
    expect(
      screen.getByText(/Estás a punto de abandonar tu sesión/),
    ).toBeInTheDocument()

    fireEvent.click(screen.getByRole('button', { name: 'Sí, cerrar sesión' }))
    expect(onConfirm).toHaveBeenCalledOnce()
  })

  it('shows a spinner state and blocks double confirmation', () => {
    render(
      <LogoutConfirmationModal
        isOpen
        onClose={vi.fn()}
        onConfirm={vi.fn()}
        isLoading
      />,
    )

    const loadingButton = screen.getByRole('button', { name: 'Cerrando sesión...' })
    expect(loadingButton).toBeDisabled()
    expect(screen.getByRole('button', { name: 'Cancelar' })).toBeDisabled()
  })

  it('closes only the modal when cancel is pressed', () => {
    const onClose = vi.fn()
    const onConfirm = vi.fn()

    render(
      <LogoutConfirmationModal
        isOpen
        onClose={onClose}
        onConfirm={onConfirm}
      />,
    )

    fireEvent.click(screen.getByRole('button', { name: 'Cancelar' }))

    expect(onClose).toHaveBeenCalledOnce()
    expect(onConfirm).not.toHaveBeenCalled()
  })
})
