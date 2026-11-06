import { useState } from 'react'
import { cn } from '../../lib/cn'

type AvatarSize = 'sm' | 'md' | 'lg' | 'xl'

interface CustomerAvatarProps {
  name: string
  photoUrl?: string | null | undefined
  size?: AvatarSize
  className?: string
}

const sizeClasses: Record<AvatarSize, string> = {
  sm: 'h-8 w-8 text-[11px]',
  md: 'h-10 w-10 text-sm',
  lg: 'h-14 w-14 text-lg',
  xl: 'h-20 w-20 text-2xl',
}

function getInitials(name: string): string {
  const parts = name.trim().split(/\s+/).filter(Boolean)
  if (parts.length === 0) return '?'
  if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase()
  return `${parts[0][0]}${parts[1][0]}`.toUpperCase()
}

export function CustomerAvatar({
  name,
  photoUrl,
  size = 'md',
  className,
}: CustomerAvatarProps) {
  const [failedUrl, setFailedUrl] = useState<string | null>(null)
  const showImage = !!photoUrl && failedUrl !== photoUrl

  return (
    <div
      className={cn(
        'flex shrink-0 items-center justify-center overflow-hidden rounded-full bg-slate-100 font-semibold text-slate-500 ring-1 ring-slate-200',
        sizeClasses[size],
        className
      )}
      aria-label={`Foto de ${name}`}
    >
      {showImage ? (
        <img
          src={photoUrl}
          alt={`Foto de ${name}`}
          loading="lazy"
          decoding="async"
          className="h-full w-full object-cover"
          onError={() => setFailedUrl(photoUrl ?? null)}
        />
      ) : (
        <span aria-hidden="true">{getInitials(name)}</span>
      )}
    </div>
  )
}
