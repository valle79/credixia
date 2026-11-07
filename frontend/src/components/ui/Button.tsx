import { forwardRef } from 'react'
import type { ButtonHTMLAttributes } from 'react'
import { LoaderCircle } from 'lucide-react'
import { cn } from '../../lib/cn'

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: 'primary' | 'secondary' | 'ghost' | 'danger'
  size?: 'sm' | 'md' | 'lg'
  isLoading?: boolean
  loadingText?: string
}

export const Button = forwardRef<HTMLButtonElement, ButtonProps>(
  ({ className, variant = 'primary', size = 'md', isLoading, loadingText, disabled, children, ...props }, ref) => {
    return (
      <button
        ref={ref}
        disabled={disabled || isLoading}
        aria-busy={isLoading || undefined}
        className={cn(
          'inline-flex items-center justify-center gap-2 rounded-lg font-medium transition-colors focus:outline-none focus:ring-2 focus:ring-offset-2 disabled:opacity-50 disabled:pointer-events-none',
          {
            'bg-brand-600 text-white hover:bg-brand-700 focus:ring-brand-500': variant === 'primary',
            'bg-slate-100 text-slate-900 hover:bg-slate-200 focus:ring-slate-500': variant === 'secondary',
            'bg-transparent text-slate-700 hover:bg-slate-100 focus:ring-slate-500': variant === 'ghost',
            'bg-red-600 text-white hover:bg-red-700 focus:ring-red-500': variant === 'danger',
            'px-3 py-1.5 text-sm': size === 'sm',
            'px-4 py-2 text-sm': size === 'md',
            'px-6 py-3 text-base': size === 'lg',
          },
          className
        )}
        {...props}
      >
        {isLoading && <LoaderCircle className="h-4 w-4 shrink-0 animate-spin" aria-hidden="true" />}
        {isLoading && loadingText ? loadingText : children}
      </button>
    )
  }
)

Button.displayName = 'Button'
