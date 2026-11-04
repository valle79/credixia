import { Children, isValidElement } from 'react'
import type { HTMLAttributes, ReactNode } from 'react'
import { cn } from '../../lib/cn'

interface CardProps extends HTMLAttributes<HTMLDivElement> {
  title?: string
  subtitle?: string
  actions?: ReactNode
}

export function Card({ className, title, subtitle, actions, children, ...props }: CardProps) {
  const hasStructuredContent = Children.toArray(children).some(
    (child) => isValidElement(child) && (child.type === CardHeader || child.type === CardBody)
  )

  return (
    <div
      className={cn(
        'rounded-xl border border-slate-200 bg-white shadow-sm',
        className
      )}
      {...props}
    >
      {(title || subtitle || actions) && (
        <div className="flex items-center justify-between border-b border-slate-100 px-6 py-4">
          <div>
            {title && <h3 className="text-lg font-semibold text-slate-900">{title}</h3>}
            {subtitle && <p className="mt-0.5 text-sm text-slate-500">{subtitle}</p>}
          </div>
          {actions && <div className="flex items-center gap-2">{actions}</div>}
        </div>
      )}
      {hasStructuredContent ? children : <div className="p-6">{children}</div>}
    </div>
  )
}

interface CardHeaderProps {
  title: string
  description?: string
  action?: ReactNode
  className?: string
}

export function CardHeader({ title, description, action, className }: CardHeaderProps) {
  return (
    <div className={cn('flex items-start justify-between gap-4 border-b border-slate-100 px-6 py-4', className)}>
      <div>
        <h3 className="text-lg font-semibold text-slate-900">{title}</h3>
        {description && <p className="mt-0.5 text-sm text-slate-500">{description}</p>}
      </div>
      {action && <div className="flex shrink-0 items-center gap-2">{action}</div>}
    </div>
  )
}

export function CardBody({ className, children, ...props }: HTMLAttributes<HTMLDivElement>) {
  return (
    <div className={cn('p-6', className)} {...props}>
      {children}
    </div>
  )
}
