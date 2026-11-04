import { RefreshIcon } from '../../components/icons'
import { Button } from '../../components/ui/Button'
import { cn } from '../../lib/cn'
import { useRefreshSystem } from './hooks'

interface RefreshButtonProps {
  label?: string
  compact?: boolean
}

export function RefreshButton({
  label = 'Actualizar',
  compact = false,
}: RefreshButtonProps) {
  const { refresh, isFetching } = useRefreshSystem()

  return (
    <Button
      size="sm"
      onClick={refresh}
      disabled={isFetching}
      aria-label="Actualizar estado"
    >
      <RefreshIcon className={cn('size-3.5', isFetching && 'animate-spin')} />
      <span className={cn(compact && 'sr-only')}>{label}</span>
    </Button>
  )
}
