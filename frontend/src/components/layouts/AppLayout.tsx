import { useEffect, useState } from 'react'
import type { ReactNode } from 'react'
import { Link, Navigate, NavLink, Outlet, useLocation, useNavigate } from 'react-router-dom'
import {
  ArrowUpRight,
  Boxes,
  Building2,
  Clock,
  FileText,
  LayoutDashboard,
  LogOut,
  Menu,
  PhoneCall,
  Settings,
  ShieldCheck,
  Users,
  Wallet,
  X,
} from 'lucide-react'
import type { LucideIcon } from 'lucide-react'
import { useAuth } from '../../hooks/useAuth'
import { cn } from '../../lib/cn'
import { Button } from '../ui/Button'

interface NavItem {
  name: string
  href: string
  icon: LucideIcon
  end?: boolean
  permission?: string | string[]
}

interface NavGroup {
  label: string
  items: NavItem[]
}

const timeFormatter = new Intl.DateTimeFormat('es-PE', {
  hour: '2-digit',
  minute: '2-digit',
  second: '2-digit',
  hour12: false,
})

const dateFormatter = new Intl.DateTimeFormat('es-PE', {
  weekday: 'short',
  day: '2-digit',
  month: 'short',
})

function useClock() {
  const [now, setNow] = useState(() => new Date())

  useEffect(() => {
    const interval = window.setInterval(() => setNow(new Date()), 1000)
    return () => window.clearInterval(interval)
  }, [])

  return now
}

const platformNavigation: NavItem[] = [
  { name: 'Dashboard', href: '/admin', icon: LayoutDashboard, end: true },
  { name: 'Organizaciones', href: '/admin/organizations', icon: Building2 },
  { name: 'Configuración', href: '/admin/settings', icon: Settings },
]

const operationsNavigation: NavItem[] = [
  { name: 'Dashboard', href: '/', icon: LayoutDashboard, end: true },
  { name: 'Clientes', href: '/customers', icon: Users, permission: 'customers.view' },
  { name: 'Productos', href: '/loan-products', icon: Boxes, permission: 'loans.view' },
  { name: 'Préstamos', href: '/loans', icon: FileText, permission: 'loans.view' },
  { name: 'Caja', href: '/cash', icon: Wallet, permission: 'cash.manage' },
  { name: 'Cobranza', href: '/collections', icon: PhoneCall, permission: 'collections.manage' },
]

function matchesPath(item: NavItem, pathname: string): boolean {
  if (item.end) {
    return pathname === item.href
  }

  return pathname === item.href || pathname.startsWith(`${item.href}/`)
}

function canAccess(item: NavItem, hasPermission: (permission: string) => boolean): boolean {
  if (!item.permission) return true

  const permissions = Array.isArray(item.permission) ? item.permission : [item.permission]
  return permissions.some(hasPermission)
}

function visibleItems(items: NavItem[], hasPermission: (permission: string) => boolean): NavItem[] {
  return items.filter((item) => canAccess(item, hasPermission))
}

interface SidebarProps {
  groups: NavGroup[]
  contextLabel: string
  contextName: string
  isPlatformContext: boolean
  logoUrl: string | null
  userFullName: string
  userEmail: string
  isPlatformAdmin: boolean
  onNavigate: (href: string) => void
  onLogout: () => void
}

function Sidebar({
  groups,
  contextLabel,
  contextName,
  isPlatformContext,
  logoUrl,
  userFullName,
  userEmail,
  isPlatformAdmin,
  onNavigate,
  onLogout,
}: SidebarProps) {
  const [failedLogoUrl, setFailedLogoUrl] = useState<string | null>(null)

  return (
    <div className="flex h-full flex-col bg-white">
      <Link
        to={isPlatformContext ? '/admin' : '/'}
        onClick={() => onNavigate(isPlatformContext ? '/admin' : '/')}
        className="flex h-16 shrink-0 items-center gap-3 border-b border-slate-200 px-5"
      >
        {logoUrl && failedLogoUrl !== logoUrl ? (
          <img
            src={logoUrl}
            alt={contextName}
            onError={() => setFailedLogoUrl(logoUrl)}
            className="h-10 w-10 shrink-0 rounded-xl border border-slate-200 bg-white object-contain p-1"
          />
        ) : isPlatformContext ? (
          <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-gradient-to-br from-brand-300 to-brand-600 shadow-sm">
            <span className="text-sm font-bold text-white">LP</span>
          </div>
        ) : (
          <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-slate-100 text-slate-500">
            <Building2 className="h-5 w-5" />
          </div>
        )}
        <div className="min-w-0">
          <p className="truncate text-sm font-semibold text-slate-900">
            {isPlatformContext ? 'Portal Credixia' : contextName}
          </p>
          {isPlatformContext && (
            <p className="truncate text-xs text-slate-500">Panel de plataforma</p>
          )}
        </div>
      </Link>

      {isPlatformContext && (
        <div className="mx-4 mt-4 rounded-xl border border-slate-200 bg-slate-50/80 p-3">
          <div className="flex items-center gap-2.5">
            <span className="flex h-8 w-8 shrink-0 items-center justify-center rounded-lg bg-indigo-100 text-indigo-600">
              <ShieldCheck className="h-4 w-4" />
            </span>
            <div className="min-w-0">
              <p className="text-[10px] font-semibold uppercase tracking-[0.14em] text-slate-400">
                {contextLabel}
              </p>
              <p className="truncate text-sm font-semibold text-slate-800">{contextName}</p>
            </div>
          </div>
        </div>
      )}

      <nav className="flex-1 overflow-y-auto px-3 py-2">
        {groups.map((group) => (
          <div key={group.label} className="pt-3 first:pt-1">
            <p className="px-3 pb-2 text-[10px] font-semibold uppercase tracking-[0.16em] text-slate-400">
              {group.label}
            </p>
            <div className="space-y-1">
              {group.items.map((item) => (
                <NavLink
                  key={item.href}
                  to={item.href}
                  end={item.end === true}
                  onClick={() => onNavigate(item.href)}
                  className={({ isActive }) =>
                    cn(
                      'group flex items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium transition-colors',
                      isActive
                        ? 'bg-brand-50 text-brand-700'
                        : 'text-slate-600 hover:bg-slate-100 hover:text-slate-900'
                    )
                  }
                >
                  {({ isActive }) => (
                    <>
                      <item.icon
                        className={cn(
                          'h-4.5 w-4.5 shrink-0 transition-colors',
                          isActive ? 'text-brand-600' : 'text-slate-400 group-hover:text-slate-600'
                        )}
                      />
                      <span className="truncate">{item.name}</span>
                    </>
                  )}
                </NavLink>
              ))}
            </div>
          </div>
        ))}
      </nav>

      <div className="shrink-0 border-t border-slate-200 p-3">
        <div className="flex items-center gap-3 rounded-lg px-2 py-2">
          <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-gradient-to-br from-brand-100 to-brand-200 text-sm font-semibold text-brand-800">
            {userFullName.charAt(0).toUpperCase()}
          </div>
          <div className="min-w-0 flex-1">
            <p className="truncate text-sm font-medium text-slate-900">{userFullName}</p>
            <p className="truncate text-xs text-slate-500">{userEmail}</p>
          </div>
        </div>

        {isPlatformAdmin && (
          <button
            type="button"
            onClick={() => onNavigate('/select-tenant')}
            className="mt-1 flex w-full items-center gap-2 rounded-lg px-3 py-2 text-left text-xs font-medium text-slate-600 transition-colors hover:bg-slate-100 hover:text-slate-900"
          >
            <Building2 className="h-3.5 w-3.5" />
            Cambiar organización
          </button>
        )}

        <Button variant="ghost" size="sm" className="mt-1 w-full justify-start" onClick={onLogout}>
          <LogOut className="h-4 w-4" />
          Cerrar sesión
        </Button>
      </div>
    </div>
  )
}

interface GuardProps {
  children: ReactNode
}

export function RequirePlatformAdmin({ children }: GuardProps) {
  const { user } = useAuth()

  if (!user?.isPlatformAdmin) {
    return <Navigate to="/" replace />
  }

  return <>{children}</>
}

export function RequirePermission({ permission, children }: GuardProps & { permission: string | string[] }) {
  const { hasPermission } = useAuth()
  const permissions = Array.isArray(permission) ? permission : [permission]

  if (!permissions.some(hasPermission)) {
    return <Navigate to="/" replace />
  }

  return <>{children}</>
}

export function AppLayout() {
  const { user, tenant, logout, hasPermission } = useAuth()
  const location = useLocation()
  const navigate = useNavigate()
  const [isMobileOpen, setIsMobileOpen] = useState(false)
  const now = useClock()

  const isPlatformArea = location.pathname === '/admin' || location.pathname.startsWith('/admin/')
  const isPlatformAdmin = !!user?.isPlatformAdmin

  if (isPlatformAdmin && !tenant && !isPlatformArea) {
    return <Navigate to="/admin" replace />
  }

  if (!user) {
    return <Navigate to="/login" replace />
  }

  const roleLabel = isPlatformAdmin
    ? 'Super Admin'
    : hasPermission('settings.manage') || hasPermission('members.manage')
      ? 'Administrador'
      : 'Usuario'

  const operationsItems = visibleItems(operationsNavigation, hasPermission)
  const platformItems = isPlatformAdmin ? platformNavigation : []

  const groups: NavGroup[] = isPlatformArea
    ? [
        { label: 'Plataforma', items: platformItems },
        ...(tenant
          ? [{ label: 'Operación', items: [{ name: 'Ir a la operación', href: '/', icon: ArrowUpRight, end: true }] }]
          : []),
      ]
    : [
        { label: 'Operación', items: operationsItems },
        ...(isPlatformAdmin
          ? [{ label: 'Plataforma', items: [{ name: 'Panel de plataforma', href: '/admin', icon: ShieldCheck }] }]
          : []),
      ]

  const allVisibleItems = groups.flatMap((group) => group.items)
  const activeItem =
    allVisibleItems.find((item) => matchesPath(item, location.pathname)) ??
    (isPlatformArea ? platformNavigation[0] : operationsNavigation[0])

  const handleNavigate = (href: string) => {
    setIsMobileOpen(false)
    navigate(href)
  }

  const handleLogout = () => {
    setIsMobileOpen(false)
    logout()
  }

  const sidebar = (
    <Sidebar
      groups={groups}
      contextLabel="Espacio de trabajo"
      contextName={isPlatformArea ? 'Plataforma' : tenant?.name || 'Sin organizaci��n'}
      isPlatformContext={isPlatformArea}
      logoUrl={isPlatformArea ? null : tenant?.logoUrl ?? null}
      userFullName={user.fullName}
      userEmail={user.email}
      isPlatformAdmin={isPlatformAdmin}
      onNavigate={handleNavigate}
      onLogout={handleLogout}
    />
  )

  return (
    <div className="flex h-screen overflow-hidden bg-slate-50">
      <aside className="hidden h-full w-64 shrink-0 border-r border-slate-200 lg:block">{sidebar}</aside>

      <div
        className={cn(
          'fixed inset-0 z-40 lg:hidden',
          isMobileOpen ? 'pointer-events-auto' : 'pointer-events-none'
        )}
        aria-hidden={!isMobileOpen}
      >
        <button
          type="button"
          aria-label="Cerrar menú"
          tabIndex={isMobileOpen ? 0 : -1}
          onClick={() => setIsMobileOpen(false)}
          className={cn(
            'absolute inset-0 bg-slate-950/40 backdrop-blur-sm transition-opacity duration-200',
            isMobileOpen ? 'opacity-100' : 'opacity-0'
          )}
        />
        <aside
          className={cn(
            'absolute inset-y-0 left-0 w-72 max-w-[85vw] border-r border-slate-200 shadow-2xl transition-transform duration-200 ease-out',
            isMobileOpen ? 'translate-x-0' : '-translate-x-full'
          )}
        >
          <button
            type="button"
            aria-label="Cerrar menú"
            onClick={() => setIsMobileOpen(false)}
            className="absolute right-3 top-4 z-10 rounded-lg p-2 text-slate-500 transition-colors hover:bg-slate-100 hover:text-slate-900"
          >
            <X className="h-5 w-5" />
          </button>
          {sidebar}
        </aside>
      </div>

      <div className="flex min-h-0 min-w-0 flex-1 flex-col">
        <header className="z-30 shrink-0 border-b border-slate-200 bg-white/80 backdrop-blur-sm">
          <div className="flex h-16 items-center justify-between gap-3 px-4 sm:px-6">
            <div className="flex min-w-0 items-center gap-3">
              <button
                type="button"
                aria-label="Abrir menú"
                onClick={() => setIsMobileOpen(true)}
                className="rounded-lg border border-slate-200 p-2 text-slate-600 transition-colors hover:bg-slate-100 hover:text-slate-900 lg:hidden"
              >
                <Menu className="h-5 w-5" />
              </button>
              <div className="min-w-0">
                <h2 className="truncate text-base font-semibold text-slate-900 sm:text-lg">
                  {activeItem.name}
                </h2>
                <p className="hidden truncate text-xs text-slate-500 sm:block">
                  {isPlatformArea ? 'Panel de control' : tenant?.name || 'Portal Credixia'}
                </p>
              </div>
            </div>

            <div className="flex shrink-0 items-center gap-2 sm:gap-3">
              <div
                className="hidden items-center gap-2 rounded-lg border border-slate-200 bg-white px-3 py-1.5 text-xs text-slate-500 md:flex"
                title={now.toLocaleString('es-PE')}
              >
                <Clock className="h-3.5 w-3.5 text-slate-400" />
                <span className="font-semibold tabular-nums text-slate-800">{timeFormatter.format(now)}</span>
                <span className="capitalize text-slate-400">{dateFormatter.format(now)}</span>
              </div>

              <span
                className={cn(
                  'inline-flex items-center gap-1.5 whitespace-nowrap rounded-full px-2.5 py-1 text-xs font-medium',
                  isPlatformAdmin
                    ? 'bg-indigo-50 text-indigo-700'
                    : roleLabel === 'Administrador'
                      ? 'bg-brand-50 text-brand-700'
                      : 'bg-slate-100 text-slate-600'
                )}
              >
                {isPlatformAdmin ? <ShieldCheck className="h-3.5 w-3.5" /> : <Users className="h-3.5 w-3.5" />}
                {roleLabel}
              </span>

              <div className="hidden h-9 w-9 items-center justify-center rounded-full bg-gradient-to-br from-brand-100 to-brand-200 text-sm font-semibold text-brand-800 sm:flex">
                {user.fullName.charAt(0).toUpperCase()}
              </div>
            </div>
          </div>
        </header>

        <main className="flex-1 overflow-y-auto p-4 sm:p-6">
          <div className="mx-auto w-full max-w-7xl">
            <Outlet />
          </div>
        </main>
      </div>
    </div>
  )
}
