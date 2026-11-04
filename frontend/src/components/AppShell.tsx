import { NavLink, Outlet, useLocation } from 'react-router-dom'
import { cn } from '../lib/cn'
import { formatTime } from '../lib/format'
import { useNow } from '../lib/useNow'
import { RefreshButton } from '../features/system/RefreshButton'
import { usePing } from '../features/system/hooks'
import { queryServiceState } from '../features/system/status'
import {
  ApiIcon,
  DashboardIcon,
  RoadmapIcon,
  SystemIcon,
} from './icons'

interface NavigationItem {
  to: string
  label: string
  icon: typeof DashboardIcon
  end?: boolean
}

interface NavigationGroup {
  section: string
  items: NavigationItem[]
}

const navigation: NavigationGroup[] = [
  {
    section: 'OperaciÃ³n',
    items: [
      { to: '/', label: 'Panel general', icon: DashboardIcon, end: true },
      { to: '/sistema', label: 'Estado del sistema', icon: SystemIcon },
    ],
  },
  {
    section: 'Recursos',
    items: [
      { to: '/api', label: 'API y recursos', icon: ApiIcon },
      { to: '/hoja-de-ruta', label: 'Hoja de ruta', icon: RoadmapIcon },
    ],
  },
]

const pageMeta: Record<string, { title: string; description: string }> = {
  '/': {
    title: 'Panel general',
    description: 'Estado operativo de la plataforma en tiempo real.',
  },
  '/sistema': {
    title: 'Estado del sistema',
    description: 'DiagnÃ³stico de servicios, latencia y dependencias.',
  },
  '/api': {
    title: 'API y recursos',
    description: 'Superficie pÃºblica, documentaciÃ³n y consola de pruebas.',
  },
  '/hoja-de-ruta': {
    title: 'Hoja de ruta',
    description: 'Alcance entregado y siguientes fases del producto.',
  },
}

const allItems = navigation.flatMap((group) => group.items)

function navLinkClass(isActive: boolean): string {
  return cn(
    'group relative flex items-center gap-3 rounded-xl px-3 py-2.5 text-sm font-medium transition',
    isActive
      ? 'bg-white/10 text-white ring-1 ring-white/10'
      : 'text-slate-400 hover:bg-white/5 hover:text-slate-100',
  )
}

export function AppShell() {
  const location = useLocation()
  const now = useNow()
  const ping = usePing()
  const meta = pageMeta[location.pathname] ?? {
    title: 'Lending Portal',
    description: 'Plataforma de gestiÃ³n de crÃ©dito.',
  }
  const connectionState = queryServiceState(ping.isPending, ping.isError)

  return (
    <div className="min-h-screen bg-slate-50 lg:flex">
      <a
        href="#main"
        className="sr-only z-50 rounded-lg bg-slate-950 px-3 py-2 text-sm text-white focus:not-sr-only focus:fixed focus:top-3 focus:left-3"
      >
        Saltar al contenido
      </a>

      <aside className="hidden w-64 shrink-0 flex-col border-r border-white/5 bg-slate-950 lg:sticky lg:top-0 lg:flex lg:h-screen">
        <div className="flex items-center gap-3 px-5 py-6">
          <div className="relative grid size-10 place-items-center rounded-xl bg-gradient-to-br from-brand-300 to-brand-600 text-sm font-bold tracking-tight text-slate-950">
            LP
            <span className="absolute -right-0.5 -bottom-0.5 size-2.5 rounded-full border-2 border-slate-950 bg-brand-400" />
          </div>
          <div className="min-w-0">
            <p className="truncate text-sm font-semibold text-white">
              Lending Portal
            </p>
            <p className="truncate text-xs text-slate-500">OperaciÃ³n de crÃ©dito</p>
          </div>
        </div>

        <nav className="flex-1 space-y-6 overflow-y-auto px-3 pb-4">
          {navigation.map((group) => (
            <div key={group.section}>
              <p className="px-3 pb-2 text-[10px] font-semibold tracking-[0.16em] text-slate-600 uppercase">
                {group.section}
              </p>
              <div className="space-y-1">
                {group.items.map((item) => (
                  <NavLink
                    key={item.to}
                    to={item.to}
                    end={item.end === true}
                    className={({ isActive }) => navLinkClass(isActive)}
                  >
                    <item.icon className="size-4.5 shrink-0" />
                    <span>{item.label}</span>
                  </NavLink>
                ))}
              </div>
            </div>
          ))}
        </nav>

        <div className="m-3 rounded-2xl border border-white/10 bg-white/5 p-4">
          <div className="flex items-center justify-between gap-2">
            <span className="text-[10px] font-semibold tracking-[0.14em] text-slate-500 uppercase">
              ConexiÃ³n
            </span>
            <span
              className={cn(
                'size-1.5 rounded-full',
                connectionState === 'online' && 'bg-emerald-400',
                connectionState === 'loading' && 'animate-pulse bg-amber-400',
                connectionState === 'offline' && 'bg-rose-400',
                connectionState === 'degraded' && 'bg-amber-400',
              )}
            />
          </div>
          <p className="mt-2 truncate text-xs font-medium text-slate-200">
            {ping.data
              ? `${ping.data.environment} Â· v${ping.data.version}`
              : ping.isError
                ? 'Sin conexiÃ³n'
                : 'Verificandoâ€¦'}
          </p>
          <p className="mt-0.5 truncate text-[11px] text-slate-500">
            {ping.data ? ping.data.service : 'API de operaciones'}
          </p>
        </div>
      </aside>

      <div className="flex min-w-0 flex-1 flex-col">
        <header className="sticky top-0 z-20 border-b border-slate-200/80 bg-white/85 backdrop-blur">
          <div className="flex items-center justify-between gap-3 px-4 py-3 sm:px-6">
            <div className="flex min-w-0 items-center gap-3">
              <div className="grid size-8 shrink-0 place-items-center rounded-lg bg-slate-950 text-[10px] font-bold text-brand-300 lg:hidden">
                LP
              </div>
              <div className="min-w-0">
                <p className="truncate text-sm font-semibold text-slate-950">
                  {meta.title}
                </p>
                <p className="hidden truncate text-xs text-slate-500 sm:block">
                  {meta.description}
                </p>
              </div>
            </div>

            <div className="flex shrink-0 items-center gap-2">
              <span className="hidden font-mono text-[11px] text-slate-400 sm:inline">
                {formatTime(now)}
              </span>
              <span className="hidden items-center gap-1.5 rounded-full border border-slate-200 bg-white px-2.5 py-1 text-[11px] font-medium text-slate-600 md:inline-flex">
                <span
                  className={cn(
                    'size-1.5 rounded-full',
                    connectionState === 'online' && 'bg-emerald-500',
                    connectionState === 'loading' && 'animate-pulse bg-amber-400',
                    connectionState === 'offline' && 'bg-rose-500',
                    connectionState === 'degraded' && 'bg-amber-500',
                  )}
                />
                {ping.data ? ping.data.environment : 'API'}
              </span>
              <RefreshButton compact />
            </div>
          </div>

          <nav className="flex gap-1 overflow-x-auto border-t border-slate-100 px-3 py-2 lg:hidden">
            {allItems.map((item) => (
              <NavLink
                key={item.to}
                to={item.to}
                end={item.end === true}
                className={({ isActive }) =>
                  cn(
                    'flex shrink-0 items-center gap-1.5 rounded-lg px-2.5 py-1.5 text-xs font-medium transition',
                    isActive
                      ? 'bg-slate-950 text-white'
                      : 'text-slate-500 hover:bg-slate-100 hover:text-slate-900',
                  )
                }
              >
                <item.icon className="size-3.5" />
                {item.label}
              </NavLink>
            ))}
          </nav>
        </header>

        <main id="main" className="flex-1 px-4 py-6 sm:px-6 sm:py-8">
          <div className="mx-auto w-full max-w-6xl">
            <Outlet />
          </div>
        </main>

        <footer className="border-t border-slate-200 px-4 py-4 sm:px-6">
          <div className="mx-auto flex w-full max-w-6xl flex-col gap-1 text-[11px] text-slate-400 sm:flex-row sm:items-center sm:justify-between">
            <span>Lending Portal Â· OperaciÃ³n en PerÃº</span>
            <span>Fase 1 Â· Base tÃ©cnica</span>
          </div>
        </footer>
      </div>
    </div>
  )
}
