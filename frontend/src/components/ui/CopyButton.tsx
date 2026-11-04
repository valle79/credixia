import { useEffect, useState } from 'react'
import { CheckIcon, CopyIcon } from '../icons'
import { Button } from './Button'

interface CopyButtonProps {
  value: string
  label?: string
}

export function CopyButton({ value, label = 'Copiar' }: CopyButtonProps) {
  const [copied, setCopied] = useState(false)

  useEffect(() => {
    if (!copied) {
      return
    }

    const timer = window.setTimeout(() => setCopied(false), 1_800)
    return () => window.clearTimeout(timer)
  }, [copied])

  async function handleCopy() {
    try {
      await navigator.clipboard.writeText(value)
      setCopied(true)
    } catch {
      setCopied(false)
    }
  }

  return (
    <Button
      size="sm"
      variant="ghost"
      onClick={() => void handleCopy()}
      aria-label={`${label}: ${value}`}
    >
      {copied ? (
        <CheckIcon className="size-3.5" />
      ) : (
        <CopyIcon className="size-3.5" />
      )}
      {copied ? 'Copiado' : label}
    </Button>
  )
}
