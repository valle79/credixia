import { useState, type FormEvent, type ReactNode } from 'react'
import { Link } from 'react-router-dom'
import { useAuth } from '../hooks/useAuth'
import { Button } from '../components/ui/Button'
import { Input } from '../components/ui/Input'

export function LoginPage() {
  const { login, isLoggingIn, loginError } = useAuth()

  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')

  const handleSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()

    if (isLoggingIn) return

    login({
      email: email.trim(),
      password,
    })
  }

  return (
    <main className="min-h-screen bg-slate-50 text-slate-900">
      <div className="mx-auto grid min-h-screen max-w-[1600px] lg:grid-cols-[1.05fr_0.95fr]">
        {/* Panel de presentación */}
        <section className="relative hidden min-h-screen flex-col justify-between overflow-hidden bg-slate-950 px-12 py-10 text-white lg:flex xl:px-16 2xl:px-20">
          {/*
            Fotografía de apoyo relacionada con la gestión de préstamos y finanzas.
            Fuente: Pexels — https://www.pexels.com/photo/person-using-a-calculator-and-a-laptop-8296991/
            La imagen es decorativa; el contenido y la legibilidad no dependen de ella.
          */}
          <img
            src="https://images.pexels.com/photos/8296991/pexels-photo-8296991.jpeg?auto=compress&cs=tinysrgb&w=1600"
            alt=""
            aria-hidden="true"
            loading="eager"
            decoding="async"
            className="absolute inset-0 h-full w-full object-cover object-center"
          />
          <div
            aria-hidden="true"
            className="pointer-events-none absolute inset-0 bg-gradient-to-br from-slate-950/95 via-slate-950/80 to-blue-950/85"
          />
          <div
            aria-hidden="true"
            className="pointer-events-none absolute -right-40 top-1/4 h-[28rem] w-[28rem] rounded-full bg-blue-500/[0.08] blur-3xl"
          />
          <div
            aria-hidden="true"
            className="pointer-events-none absolute -bottom-48 -left-40 h-[30rem] w-[30rem] rounded-full bg-cyan-400/[0.06] blur-3xl"
          />

          <Brand className="relative z-10" />

          <div className="relative z-10 max-w-xl py-16">
            <p className="mb-5 text-xs font-semibold uppercase tracking-[0.2em] text-cyan-300">
              Plataforma de gestión financiera
            </p>

            <h1 className="max-w-lg text-4xl font-semibold leading-[1.15] tracking-tight text-white xl:text-5xl">
              Administra tus operaciones de préstamo desde un solo lugar.
            </h1>

            <p className="mt-6 max-w-lg text-base leading-7 text-slate-300">
              Organiza tus empresas, usuarios y operaciones con una visión clara
              de la actividad de tu negocio.
            </p>

            <div className="mt-11 space-y-5">
              <Feature
                title="Gestión multiempresa"
                description="Administra distintas organizaciones desde una misma plataforma."
                icon={<BuildingIcon />}
              />
              <Feature
                title="Operaciones centralizadas"
                description="Mantén clientes, préstamos y pagos organizados."
                icon={<LayersIcon />}
              />
              <Feature
                title="Información organizada"
                description="Consulta la actividad de tu organización desde un mismo espacio."
                icon={<ChartIcon />}
              />
            </div>
          </div>

          <div className="relative z-10 border-t border-white/10 pt-6">
            <p className="text-xs text-slate-400">
              Una plataforma para administrar tu operación financiera.
            </p>
          </div>
        </section>

        {/* Panel de acceso */}
        <section className="flex min-h-screen items-center justify-center px-5 py-10 sm:px-8 lg:px-12 xl:px-16">
          <div className="w-full max-w-[440px]">
            <div className="mb-8 flex justify-center lg:hidden">
              <Brand />
            </div>

            <div className="rounded-2xl border border-slate-200 bg-white p-7 shadow-sm shadow-slate-200/60 sm:p-9">
              <div className="mb-8">
                <p className="text-sm font-semibold text-blue-700">
                  Acceso a la plataforma
                </p>
                <h2 className="mt-2 text-3xl font-semibold tracking-tight text-slate-950">
                  Inicia sesión
                </h2>
                <p className="mt-3 text-sm leading-6 text-slate-500">
                  Ingresa tus credenciales para continuar a tu espacio de trabajo.
                </p>
              </div>

              <form onSubmit={handleSubmit} className="space-y-5">
                <div className="[&_label]:!text-sm [&_label]:!font-medium [&_label]:!text-slate-700 [&_input]:!h-12 [&_input]:!rounded-lg [&_input]:!border-slate-300 [&_input]:!bg-white [&_input]:!text-slate-900 [&_input]:placeholder:!text-slate-400 [&_input]:focus:!border-blue-500 [&_input]:focus:!ring-blue-500/20">
                  <Input
                    label="Correo electrónico"
                    type="email"
                    value={email}
                    onChange={(event) => setEmail(event.target.value)}
                    placeholder="nombre@empresa.com"
                    required
                    autoComplete="username"
                    autoFocus
                  />
                </div>

                <div className="[&_label]:!text-sm [&_label]:!font-medium [&_label]:!text-slate-700 [&_input]:!h-12 [&_input]:!rounded-lg [&_input]:!border-slate-300 [&_input]:!bg-white [&_input]:!text-slate-900 [&_input]:placeholder:!text-slate-400 [&_input]:focus:!border-blue-500 [&_input]:focus:!ring-blue-500/20">
                  <Input
                    label="Contraseña"
                    type="password"
                    value={password}
                    onChange={(event) => setPassword(event.target.value)}
                    placeholder="Ingresa tu contraseña"
                    required
                    autoComplete="current-password"
                  />
                </div>

                {loginError && (
                  <div
                    role="alert"
                    aria-live="polite"
                    className="flex items-start gap-3 rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700"
                  >
                    <AlertIcon />
                    <p className="min-w-0 leading-5">{loginError}</p>
                  </div>
                )}

                <Button
                  type="submit"
                  size="lg"
                  isLoading={isLoggingIn}
                  disabled={isLoggingIn || !email.trim() || !password}
                  className="!h-12 w-full !rounded-lg !border-0 !bg-blue-700 !font-semibold !text-white shadow-sm transition-colors hover:!bg-blue-800 disabled:cursor-not-allowed disabled:opacity-60"
                >
                  Iniciar sesión
                </Button>

                <div className="flex justify-center pt-1">
                  <Link
                    to="/olvide-password"
                    className="text-sm font-medium text-blue-700 transition-colors hover:text-blue-900 focus:outline-none focus-visible:rounded focus-visible:ring-2 focus-visible:ring-blue-600 focus-visible:ring-offset-2"
                  >
                    ¿Olvidaste tu contraseña?
                  </Link>
                </div>
              </form>

              <div className="mt-8 border-t border-slate-100 pt-5">
                <div className="flex items-center justify-center gap-2 text-xs text-slate-500">
                  <ShieldIcon />
                  <span>Acceso exclusivo para usuarios autorizados.</span>
                </div>
              </div>
            </div>

            <footer className="mt-7 space-y-1 text-center">
              <p className="text-xs text-slate-500">
                © {new Date().getFullYear()} Credixia. Todos los derechos reservados.
              </p>
              <p className="text-[10px] uppercase tracking-[0.14em] text-slate-400">
                Plataforma de gestión de préstamos
              </p>
            </footer>
          </div>
        </section>
      </div>
    </main>
  )
}

function Brand({ className = '' }: { className?: string }) {
  return (
    <div className={`flex items-center gap-3 ${className}`}>
      <div className="flex h-11 w-11 shrink-0 items-center justify-center rounded-xl bg-blue-600 text-white shadow-sm shadow-blue-950/20">
        <BrandIcon />
      </div>
      <div>
        <p className="text-lg font-bold tracking-tight text-white">
          LENDING<span className="text-cyan-300">PORTAL</span>
        </p>
        <p className="mt-0.5 text-[9px] font-medium uppercase tracking-[0.19em] text-slate-400">
          Financial management
        </p>
      </div>
    </div>
  )
}

function Feature({
  title,
  description,
  icon,
}: {
  title: string
  description: string
  icon: ReactNode
}) {
  return (
    <div className="flex items-start gap-4">
      <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg border border-white/10 bg-white/[0.06] text-cyan-300">
        {icon}
      </div>
      <div className="pt-0.5">
        <h3 className="text-sm font-semibold text-white">{title}</h3>
        <p className="mt-1 max-w-sm text-sm leading-5 text-slate-400">{description}</p>
      </div>
    </div>
  )
}

function BrandIcon() {
  return (
    <svg viewBox="0 0 24 24" fill="none" className="h-6 w-6" aria-hidden="true">
      <path
        d="M4 8.2 12 4l8 4.2v7.6L12 20l-8-4.2V8.2Z"
        stroke="currentColor"
        strokeWidth="1.7"
        strokeLinejoin="round"
      />
      <path
        d="m8.2 12.1 2.4 2.4 5.2-5.2"
        stroke="currentColor"
        strokeWidth="1.7"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  )
}

function BuildingIcon() {
  return (
    <svg viewBox="0 0 24 24" fill="none" className="h-5 w-5" aria-hidden="true">
      <path d="M4 20V5.5L12 3l8 2.5V20H4Z" stroke="currentColor" strokeWidth="1.6" strokeLinejoin="round" />
      <path d="M8 8h.01M12 8h.01M16 8h.01M8 12h.01M12 12h.01M16 12h.01M10 20v-4h4v4" stroke="currentColor" strokeWidth="2" strokeLinecap="round" />
    </svg>
  )
}

function LayersIcon() {
  return (
    <svg viewBox="0 0 24 24" fill="none" className="h-5 w-5" aria-hidden="true">
      <path d="m12 3 9 5-9 5-9-5 9-5Z" stroke="currentColor" strokeWidth="1.6" strokeLinejoin="round" />
      <path d="m3 12 9 5 9-5M3 16l9 5 9-5" stroke="currentColor" strokeWidth="1.6" strokeLinejoin="round" />
    </svg>
  )
}

function ChartIcon() {
  return (
    <svg viewBox="0 0 24 24" fill="none" className="h-5 w-5" aria-hidden="true">
      <path d="M4 19V5M4 19h16" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" />
      <path d="m7 15 4-4 3 2 5-6" stroke="currentColor" strokeWidth="1.7" strokeLinecap="round" strokeLinejoin="round" />
    </svg>
  )
}

function ShieldIcon() {
  return (
    <svg viewBox="0 0 24 24" fill="none" className="h-4 w-4 shrink-0 text-emerald-600" aria-hidden="true">
      <path d="M12 22s8-4 8-11V5l-8-3-8 3v6c0 7 8 11 8 11Z" stroke="currentColor" strokeWidth="1.7" strokeLinejoin="round" />
      <path d="m9 12 2 2 4-4" stroke="currentColor" strokeWidth="1.7" strokeLinecap="round" strokeLinejoin="round" />
    </svg>
  )
}

function AlertIcon() {
  return (
    <svg viewBox="0 0 24 24" fill="none" className="mt-0.5 h-4 w-4 shrink-0" aria-hidden="true">
      <circle cx="12" cy="12" r="9" stroke="currentColor" strokeWidth="1.7" />
      <path d="M12 8v4m0 4h.01" stroke="currentColor" strokeWidth="1.7" strokeLinecap="round" />
    </svg>
  )
}
