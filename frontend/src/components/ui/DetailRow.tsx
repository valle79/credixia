import { cn } from '../../lib/cn'

interface DetailRowProps {
  label: string
  value: string
  mono?: boolean
}

export function DetailRow({ label, value, mono = false }: DetailRowProps) {
  return (
    <div className="flex items-center justify-between gap-4 text-xs">
      <span className="shrink-0 text-slate-500">{label}</span>
      <span
        className={cn(
          'min-w-0 truncate text-right font-medium text-slate-800',
          mono && 'font-mono text-[11px]',
        )}
        title={value}
      >
        {value}
      </span>
    </div>
  )
}
