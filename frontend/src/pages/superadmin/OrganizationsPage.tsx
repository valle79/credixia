import { useEffect, useMemo, useState } from 'react'
import type { FormEvent, ReactNode } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { platformApi } from '../../api/platform.api'
import { Button } from '../../components/ui/Button'
import { getErrorMessage } from '../../api/client'
import type { CreateTenantResponse } from '../../api/types'

type StatusFilter = 'all' | 'Active' | 'Suspended'
type SortOption = 'newest' | 'oldest' | 'name'
type Tenant = NonNullable<Awaited<ReturnType<typeof platformApi.listTenants>>>[number]
type PendingStatusChange = { tenant: Tenant; suspend: boolean }

export function OrganizationsPage() {
  const queryClient = useQueryClient()
  const navigate = useNavigate()
  const [searchParams, setSearchParams] = useSearchParams()

  const [showCreateModal, setShowCreateModal] = useState(() => searchParams.get('action') === 'create')
  const [createdTenant, setCreatedTenant] = useState<CreateTenantResponse | null>(null)
  const [phoneForWhatsApp, setPhoneForWhatsApp] = useState('')
  const [generatedPassword, setGeneratedPassword] = useState('')
  const [search, setSearch] = useState('')
  const [statusFilter, setStatusFilter] = useState<StatusFilter>('all')
  const [sortBy, setSortBy] = useState<SortOption>('newest')
  const [pendingStatusChange, setPendingStatusChange] = useState<PendingStatusChange | null>(null)
  const [copiedField, setCopiedField] = useState<'email' | 'password' | null>(null)
  const [copyError, setCopyError] = useState('')

  useEffect(() => {
    if (searchParams.get('action') === 'create') {
      const nextParams = new URLSearchParams(searchParams)
      nextParams.delete('action')
      setSearchParams(nextParams, { replace: true })
    }
  }, [searchParams, setSearchParams])

  const {
    data: tenants = [],
    isLoading: isLoadingTenants,
    isError: tenantsHasError,
    error: tenantsError,
    refetch: refetchTenants,
    isFetching,
  } = useQuery({
    queryKey: ['platform', 'tenants'],
    queryFn: () => platformApi.listTenants(),
  })

  const createTenantMutation = useMutation({
    mutationFn: (data: {
      slug: string
      name: string
      ownerEmail: string
      ownerFullName: string
      ownerPhone: string
    }) => platformApi.createTenant(data),
    onSuccess: (created) => {
      setCreatedTenant(created)
      setPhoneForWhatsApp(created.ownerPhone)
      setGeneratedPassword(created.generatedPassword)
      setShowCreateModal(false)
      void queryClient.invalidateQueries({ queryKey: ['platform'] })
    },
  })

  const toggleStatusMutation = useMutation({
    mutationFn: ({ id, suspend }: { id: string; suspend: boolean }) =>
      platformApi.updateTenantStatus(id, suspend),
    onSuccess: () => {
      setPendingStatusChange(null)
      void queryClient.invalidateQueries({ queryKey: ['platform'] })
    },
  })

  const sendWhatsAppMutation = useMutation({
    mutationFn: ({ id, phoneNumber, password }: { id: string; phoneNumber: string; password: string }) =>
      platformApi.sendCredentialsViaWhatsApp(id, phoneNumber, password),
    onSuccess: (response) => {
      window.open(response.whatsAppUrl, '_blank', 'noopener,noreferrer')
    },
  })

  const activeCount = tenants.filter((tenant) => tenant.status === 'Active').length
  const suspendedCount = tenants.filter((tenant) => tenant.status === 'Suspended').length
  const totalMembers = tenants.reduce((total, tenant) => total + (tenant.activeMembers ?? 0), 0)

  const filteredTenants = useMemo(() => {
    const normalizedSearch = search.trim().toLocaleLowerCase('es-PE')
    const result = tenants.filter((tenant) => {
      const matchesSearch =
        !normalizedSearch ||
        tenant.name.toLocaleLowerCase('es-PE').includes(normalizedSearch) ||
        tenant.slug.toLocaleLowerCase('es-PE').includes(normalizedSearch)
      const matchesStatus = statusFilter === 'all' || tenant.status === statusFilter
      return matchesSearch && matchesStatus
    })

    return result.sort((a, b) => {
      if (sortBy === 'name') return a.name.localeCompare(b.name, 'es-PE')
      const dateDifference = new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()
      return sortBy === 'newest' ? dateDifference : -dateDifference
    })
  }, [tenants, search, statusFilter, sortBy])

  const handleCreateTenant = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    const formData = new FormData(event.currentTarget)
    createTenantMutation.mutate({
      slug: String(formData.get('slug') ?? '').trim(),
      name: String(formData.get('name') ?? '').trim(),
      ownerEmail: String(formData.get('ownerEmail') ?? '').trim(),
      ownerFullName: String(formData.get('ownerFullName') ?? '').trim(),
      ownerPhone: String(formData.get('ownerPhone') ?? '').trim(),
    })
  }

  const copyToClipboard = async (text: string, field: 'email' | 'password') => {
    setCopyError('')
    try {
      await navigator.clipboard.writeText(text)
      setCopiedField(field)
      window.setTimeout(() => setCopiedField((current) => (current === field ? null : current)), 1800)
    } catch {
      setCopyError('No se pudo copiar automáticamente. Selecciona y copia el contenido manualmente.')
    }
  }

  const closeCreatedModal = () => {
    setCreatedTenant(null)
    setPhoneForWhatsApp('')
    setGeneratedPassword('')
    setCopiedField(null)
    setCopyError('')
  }

  const formatDate = (value: string) => {
    const date = new Date(value)
    if (Number.isNaN(date.getTime())) return 'Fecha no disponible'
    return new Intl.DateTimeFormat('es-PE', {
      day: '2-digit',
      month: 'short',
      year: 'numeric',
    }).format(date)
  }

  return (
    <div className="min-h-full space-y-6 pb-8">
      <header className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div className="min-w-0">
          <div className="mb-2 flex items-center gap-2 text-xs font-semibold uppercase tracking-[0.16em] text-slate-500">
            <span className="h-1.5 w-1.5 rounded-full bg-indigo-500" />
            Administración de plataforma
          </div>
          <h1 className="text-2xl font-bold tracking-tight text-slate-950 sm:text-3xl">Organizaciones</h1>
          <p className="mt-1.5 max-w-2xl text-sm leading-6 text-slate-600">
            Administra las empresas registradas, consulta su actividad y controla su acceso a la plataforma.
          </p>
        </div>
        <Button
          onClick={() => {
            createTenantMutation.reset()
            setShowCreateModal(true)
          }}
          className="inline-flex shrink-0 items-center justify-center gap-2 bg-slate-950 text-white shadow-sm transition hover:bg-slate-800"
        >
          <Icon name="plus" />
          Nueva organización
        </Button>
      </header>

      <section aria-label="Resumen de organizaciones" className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
        <SummaryCard label="Total de organizaciones" value={tenants.length} detail="Registradas en la plataforma" icon="building" tone="slate" />
        <SummaryCard label="Organizaciones activas" value={activeCount} detail="Con acceso habilitado" icon="check" tone="emerald" />
        <SummaryCard label="Organizaciones suspendidas" value={suspendedCount} detail="Con acceso restringido" icon="pause" tone="amber" />
        <SummaryCard label="Miembros activos" value={totalMembers} detail="En todas las organizaciones" icon="users" tone="indigo" />
      </section>

      <section className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm">
        <div className="flex flex-col gap-4 border-b border-slate-100 p-4 sm:p-5 lg:flex-row lg:items-center lg:justify-between">
          <div>
            <div className="flex items-center gap-2">
              <h2 className="text-base font-semibold text-slate-900">Directorio de organizaciones</h2>
              {isFetching && !isLoadingTenants && <span className="text-xs font-medium text-slate-400">Actualizando…</span>}
            </div>
            <p className="mt-1 text-sm text-slate-500">
              {filteredTenants.length} {filteredTenants.length === 1 ? 'resultado' : 'resultados'}
              {search || statusFilter !== 'all' ? ' con los filtros aplicados' : ' en total'}
            </p>
          </div>
          <div className="flex flex-col gap-2 sm:flex-row sm:items-center">
            <label className="relative block min-w-0 sm:min-w-[260px]">
              <span className="sr-only">Buscar organización</span>
              <Icon name="search" className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-slate-400" />
              <input
                value={search}
                onChange={(event) => setSearch(event.target.value)}
                placeholder="Buscar por nombre o identificador…"
                className="h-10 w-full rounded-xl border border-slate-200 bg-white pl-9 pr-9 text-sm text-slate-900 outline-none transition placeholder:text-slate-400 hover:border-slate-300 focus:border-indigo-500 focus:ring-4 focus:ring-indigo-500/10"
              />
              {search && (
                <button
                  type="button"
                  onClick={() => setSearch('')}
                  aria-label="Limpiar búsqueda"
                  className="absolute right-2.5 top-1/2 -translate-y-1/2 rounded-md p-1 text-slate-400 hover:bg-slate-100 hover:text-slate-700"
                >
                  <Icon name="x" />
                </button>
              )}
            </label>
            <label className="relative block">
              <span className="sr-only">Filtrar por estado</span>
              <select
                value={statusFilter}
                onChange={(event) => setStatusFilter(event.target.value as StatusFilter)}
                className="h-10 w-full appearance-none rounded-xl border border-slate-200 bg-white py-2 pl-3 pr-9 text-sm text-slate-700 outline-none transition hover:border-slate-300 focus:border-indigo-500 focus:ring-4 focus:ring-indigo-500/10 sm:min-w-[170px]"
              >
                <option value="all">Todos los estados</option>
                <option value="Active">Activas</option>
                <option value="Suspended">Suspendidas</option>
              </select>
              <Icon name="chevron-down" className="pointer-events-none absolute right-3 top-1/2 -translate-y-1/2 text-slate-400" />
            </label>
            <label className="relative block">
              <span className="sr-only">Ordenar organizaciones</span>
              <select
                value={sortBy}
                onChange={(event) => setSortBy(event.target.value as SortOption)}
                className="h-10 w-full appearance-none rounded-xl border border-slate-200 bg-white py-2 pl-3 pr-9 text-sm text-slate-700 outline-none transition hover:border-slate-300 focus:border-indigo-500 focus:ring-4 focus:ring-indigo-500/10 sm:min-w-[170px]"
              >
                <option value="newest">Más recientes</option>
                <option value="oldest">Más antiguas</option>
                <option value="name">Nombre A–Z</option>
              </select>
              <Icon name="chevron-down" className="pointer-events-none absolute right-3 top-1/2 -translate-y-1/2 text-slate-400" />
            </label>
          </div>
        </div>

        <div className="p-4 sm:p-5">
          {isLoadingTenants ? (
            <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
              {Array.from({ length: 6 }).map((_, index) => <OrganizationSkeleton key={index} />)}
            </div>
          ) : tenantsHasError ? (
            <StatePanel
              icon="alert"
              title="No se pudieron cargar las organizaciones"
              description={getErrorMessage(tenantsError)}
              action={<Button variant="secondary" onClick={() => void refetchTenants()}><Icon name="refresh" /> Reintentar</Button>}
            />
          ) : tenants.length === 0 ? (
            <StatePanel
              icon="building"
              title="Todavía no hay organizaciones"
              description="Registra la primera empresa para comenzar a administrar las organizaciones de tu plataforma."
              action={<Button onClick={() => setShowCreateModal(true)} className="bg-slate-950 text-white hover:bg-slate-800"><Icon name="plus" /> Crear organización</Button>}
            />
          ) : filteredTenants.length === 0 ? (
            <StatePanel
              icon="search"
              title="No encontramos resultados"
              description="Prueba con otro nombre o identificador, o cambia el filtro de estado."
              action={<Button variant="secondary" onClick={() => { setSearch(''); setStatusFilter('all') }}>Limpiar filtros</Button>}
            />
          ) : (
            <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
              {filteredTenants.map((tenant) => (
                <OrganizationCard
                  key={tenant.id}
                  tenant={tenant}
                  formatDate={formatDate}
                  onView={() => navigate(`/admin/organizations/${tenant.id}`)}
                  onToggleStatus={() => setPendingStatusChange({ tenant, suspend: tenant.status === 'Active' })}
                  isChangingStatus={toggleStatusMutation.isPending && toggleStatusMutation.variables?.id === tenant.id}
                />
              ))}
            </div>
          )}

          {toggleStatusMutation.isError && (
            <div role="alert" className="mt-4 flex items-start gap-2 rounded-xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700">
              <Icon name="alert" className="mt-0.5 shrink-0" />
              <span>{getErrorMessage(toggleStatusMutation.error)}</span>
            </div>
          )}
        </div>
      </section>

      {showCreateModal && (
        <Modal title="Registrar organización" subtitle="Crea la empresa y configura su cuenta propietaria." onClose={() => setShowCreateModal(false)}>
          <form onSubmit={handleCreateTenant} className="space-y-5">
            <div className="rounded-xl border border-indigo-100 bg-indigo-50/70 p-3.5">
              <div className="flex gap-3">
                <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg bg-white text-indigo-600 shadow-sm"><Icon name="building" /></div>
                <div>
                  <p className="text-sm font-semibold text-indigo-950">Datos de la organización</p>
                  <p className="mt-0.5 text-xs leading-5 text-indigo-800/80">El identificador se utiliza internamente para reconocer a la empresa en la plataforma.</p>
                </div>
              </div>
            </div>

            <div className="grid gap-4 sm:grid-cols-2">
              <Field label="Nombre de la organización" hint="Nombre comercial o razón social.">
                <input name="name" placeholder="Ej. Financiera Andina S.A.C." required maxLength={160} className={fieldClass} />
              </Field>
              <Field label="Identificador (slug)" hint="Minúsculas, números y guiones.">
                <input name="slug" placeholder="financiera-andina" required pattern="[a-z0-9-]+" maxLength={80} title="Solo letras minúsculas, números y guiones" className={fieldClass} />
              </Field>
            </div>

            <div className="border-t border-slate-100 pt-4">
              <h3 className="text-sm font-semibold text-slate-900">Cuenta del propietario</h3>
              <p className="mt-1 text-xs text-slate-500">Se creará el usuario responsable de administrar esta organización.</p>
            </div>
            <div className="grid gap-4 sm:grid-cols-2">
              <Field label="Nombre completo">
                <input name="ownerFullName" placeholder="Nombre y apellidos" required maxLength={160} className={fieldClass} />
              </Field>
              <Field label="Correo electrónico">
                <input name="ownerEmail" type="email" placeholder="propietario@empresa.com" required maxLength={254} className={fieldClass} />
              </Field>
              <div className="sm:col-span-2">
                <Field label="Teléfono del propietario" hint="Incluye el código de país si corresponde. Para Perú, ingresa los 9 dígitos del celular.">
                  <input name="ownerPhone" type="tel" placeholder="987654321" required pattern="[0-9]{9,15}" title="Ingrese entre 9 y 15 dígitos, sin espacios ni símbolos" className={fieldClass} />
                </Field>
              </div>
            </div>

            {createTenantMutation.isError && (
              <div role="alert" className="flex gap-2 rounded-xl border border-rose-200 bg-rose-50 px-3.5 py-3 text-sm text-rose-700">
                <Icon name="alert" className="mt-0.5 shrink-0" />
                <span>{getErrorMessage(createTenantMutation.error)}</span>
              </div>
            )}
            <div className="flex flex-col-reverse gap-2 border-t border-slate-100 pt-4 sm:flex-row sm:justify-end">
              <Button type="button" variant="secondary" onClick={() => setShowCreateModal(false)} disabled={createTenantMutation.isPending}>Cancelar</Button>
              <Button type="submit" disabled={createTenantMutation.isPending} className="min-w-[175px] bg-slate-950 text-white hover:bg-slate-800">
                {createTenantMutation.isPending ? <><Spinner /> Creando organización…</> : <><Icon name="plus" /> Crear organización</>}
              </Button>
            </div>
          </form>
        </Modal>
      )}

      {createdTenant && (
        <Modal title="Organización creada" subtitle="La organización y la cuenta de propietario se registraron correctamente." onClose={closeCreatedModal}>
          <div className="space-y-4">
            <div className="flex gap-3 rounded-xl border border-emerald-200 bg-emerald-50 p-4">
              <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-emerald-100 text-emerald-700"><Icon name="check" size="lg" /></div>
              <div className="min-w-0">
                <p className="font-semibold text-emerald-950">{createdTenant.name}</p>
                <p className="mt-0.5 text-sm text-emerald-800">La organización está lista para su configuración inicial.</p>
              </div>
            </div>

            <div className="rounded-xl border border-slate-200 bg-slate-50 p-4">
              <div className="mb-3 flex items-center justify-between gap-3">
                <h3 className="text-sm font-semibold text-slate-900">Credenciales iniciales</h3>
                <span className="rounded-full bg-amber-100 px-2.5 py-1 text-[11px] font-semibold text-amber-800">Información sensible</span>
              </div>
              <CredentialRow label="Correo" value={createdTenant.ownerEmail} copied={copiedField === 'email'} onCopy={() => void copyToClipboard(createdTenant.ownerEmail, 'email')} />
              <div className="my-3 border-t border-slate-200" />
              <CredentialRow label="Contraseña temporal" value={createdTenant.generatedPassword} copied={copiedField === 'password'} onCopy={() => void copyToClipboard(createdTenant.generatedPassword, 'password')} secret />
              <p className="mt-3 text-xs leading-5 text-slate-500">Comparte estas credenciales únicamente con el propietario por un canal seguro y recomienda cambiar la contraseña al iniciar sesión.</p>
              {copyError && <p role="alert" className="mt-2 text-xs text-rose-600">{copyError}</p>}
            </div>

            {sendWhatsAppMutation.isError && (
              <div role="alert" className="rounded-xl border border-rose-200 bg-rose-50 px-3.5 py-3 text-sm text-rose-700">{getErrorMessage(sendWhatsAppMutation.error)}</div>
            )}
            <div className="flex flex-col gap-2 sm:flex-row">
              <Button
                onClick={() => {
                  if (phoneForWhatsApp && generatedPassword) {
                    sendWhatsAppMutation.mutate({ id: createdTenant.id, phoneNumber: phoneForWhatsApp, password: generatedPassword })
                  }
                }}
                disabled={sendWhatsAppMutation.isPending || !phoneForWhatsApp || !generatedPassword}
                className="flex-1 bg-emerald-600 text-white hover:bg-emerald-700"
              >
                {sendWhatsAppMutation.isPending ? <><Spinner /> Preparando WhatsApp…</> : <><Icon name="message" /> Compartir por WhatsApp</>}
              </Button>
              <Button variant="secondary" onClick={closeCreatedModal}>Finalizar</Button>
            </div>
          </div>
        </Modal>
      )}

      {pendingStatusChange && (
        <Modal
          title={pendingStatusChange.suspend ? 'Suspender organización' : 'Reactivar organización'}
          subtitle={pendingStatusChange.suspend ? 'La organización perderá temporalmente el acceso a la plataforma.' : 'La organización volverá a tener acceso a la plataforma.'}
          onClose={() => setPendingStatusChange(null)}
          compact
        >
          <div className="flex items-start gap-3 rounded-xl border border-slate-200 bg-slate-50 p-4">
            <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-white text-slate-600 shadow-sm"><Icon name="building" /></div>
            <div className="min-w-0">
              <p className="font-semibold text-slate-900">{pendingStatusChange.tenant.name}</p>
              <p className="mt-0.5 text-sm text-slate-500">@{pendingStatusChange.tenant.slug}</p>
            </div>
          </div>
          <div className="mt-5 flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
            <Button variant="secondary" onClick={() => setPendingStatusChange(null)} disabled={toggleStatusMutation.isPending}>Cancelar</Button>
            <Button
              onClick={() => toggleStatusMutation.mutate({ id: pendingStatusChange.tenant.id, suspend: pendingStatusChange.suspend })}
              disabled={toggleStatusMutation.isPending}
              className={pendingStatusChange.suspend ? 'bg-rose-600 text-white hover:bg-rose-700' : 'bg-emerald-600 text-white hover:bg-emerald-700'}
            >
              {toggleStatusMutation.isPending ? <><Spinner /> Procesando…</> : pendingStatusChange.suspend ? 'Sí, suspender' : 'Sí, reactivar'}
            </Button>
          </div>
        </Modal>
      )}
    </div>
  )
}

function OrganizationCard({
  tenant,
  formatDate,
  onView,
  onToggleStatus,
  isChangingStatus,
}: {
  tenant: Tenant
  formatDate: (value: string) => string
  onView: () => void
  onToggleStatus: () => void
  isChangingStatus: boolean
}) {
  const isActive = tenant.status === 'Active'
  const isSuspended = tenant.status === 'Suspended'
  const statusLabel = isActive ? 'Activa' : isSuspended ? 'Suspendida' : tenant.status

  return (
    <article className="group flex min-w-0 flex-col rounded-2xl border border-slate-200 bg-white transition duration-200 hover:-translate-y-0.5 hover:border-slate-300 hover:shadow-lg hover:shadow-slate-200/60">
      <div className="p-4 sm:p-5">
        <div className="flex items-start gap-3">
          <div className="flex h-12 w-12 shrink-0 items-center justify-center rounded-xl bg-gradient-to-br from-indigo-50 to-slate-100 text-base font-bold text-indigo-700 ring-1 ring-inset ring-indigo-100">
            {getInitials(tenant.name)}
          </div>
          <div className="min-w-0 flex-1 pt-0.5">
            <h3 className="truncate text-base font-semibold text-slate-950" title={tenant.name}>{tenant.name}</h3>
            <p className="mt-1 truncate font-mono text-xs text-slate-500" title={tenant.slug}>@{tenant.slug}</p>
          </div>
          <StatusPill status={tenant.status} label={statusLabel} />
        </div>

        <div className="mt-5 grid grid-cols-2 gap-3 rounded-xl bg-slate-50 px-3.5 py-3">
          <div>
            <div className="flex items-center gap-1.5 text-xs text-slate-500"><Icon name="users" size="sm" /> Miembros activos</div>
            <p className="mt-1 text-xl font-semibold tabular-nums text-slate-900">{tenant.activeMembers ?? 0}</p>
          </div>
          <div className="border-l border-slate-200 pl-3">
            <div className="flex items-center gap-1.5 text-xs text-slate-500"><Icon name="calendar" size="sm" /> Fecha de registro</div>
            <p className="mt-1 text-sm font-semibold text-slate-800">{formatDate(tenant.createdAt)}</p>
          </div>
        </div>
      </div>

      <div className="mt-auto flex items-center gap-2 border-t border-slate-100 px-4 py-3 sm:px-5">
        <Button size="sm" onClick={onView} className="flex-1 justify-center bg-slate-950 text-white hover:bg-slate-800">
          <Icon name="eye" /> Ver organización
        </Button>
        {(isActive || isSuspended) && (
          <Button
            size="sm"
            variant="secondary"
            onClick={onToggleStatus}
            disabled={isChangingStatus}
            className="min-w-10 justify-center px-3"
            title={isActive ? 'Suspender organización' : 'Reactivar organización'}
            aria-label={isActive ? `Suspender ${tenant.name}` : `Reactivar ${tenant.name}`}
          >
            {isChangingStatus ? <Spinner /> : <Icon name={isActive ? 'pause' : 'play'} />}
          </Button>
        )}
      </div>
    </article>
  )
}

function SummaryCard({
  label,
  value,
  detail,
  icon,
  tone,
}: {
  label: string
  value: number
  detail: string
  icon: IconName
  tone: 'slate' | 'emerald' | 'amber' | 'indigo'
}) {
  const tones = {
    slate: 'bg-slate-100 text-slate-700',
    emerald: 'bg-emerald-50 text-emerald-700',
    amber: 'bg-amber-50 text-amber-700',
    indigo: 'bg-indigo-50 text-indigo-700',
  }
  return (
    <div className="rounded-2xl border border-slate-200 bg-white p-4 shadow-sm sm:p-5">
      <div className="flex items-start justify-between gap-3">
        <div className="min-w-0">
          <p className="text-sm font-medium text-slate-500">{label}</p>
          <p className="mt-2 text-2xl font-bold tracking-tight text-slate-950 tabular-nums">{value.toLocaleString('es-PE')}</p>
        </div>
        <div className={`flex h-10 w-10 shrink-0 items-center justify-center rounded-xl ${tones[tone]}`}><Icon name={icon} size="lg" /></div>
      </div>
      <p className="mt-2 text-xs text-slate-500">{detail}</p>
    </div>
  )
}

function StatusPill({ status, label }: { status: string; label: string }) {
  const className = status === 'Active'
    ? 'border-emerald-200 bg-emerald-50 text-emerald-700'
    : status === 'Suspended'
      ? 'border-amber-200 bg-amber-50 text-amber-700'
      : 'border-slate-200 bg-slate-100 text-slate-600'
  return <span className={`inline-flex shrink-0 items-center gap-1.5 rounded-full border px-2.5 py-1 text-[11px] font-semibold ${className}`}><span className="h-1.5 w-1.5 rounded-full bg-current" />{label}</span>
}

function StatePanel({
  icon,
  title,
  description,
  action,
}: {
  icon: IconName
  title: string
  description: string
  action?: ReactNode
}) {
  return (
    <div className="flex min-h-[280px] flex-col items-center justify-center px-5 py-12 text-center">
      <div className="flex h-14 w-14 items-center justify-center rounded-2xl bg-slate-100 text-slate-500"><Icon name={icon} size="xl" /></div>
      <h3 className="mt-4 text-base font-semibold text-slate-900">{title}</h3>
      <p className="mt-1.5 max-w-md text-sm leading-6 text-slate-500">{description}</p>
      {action && <div className="mt-5">{action}</div>}
    </div>
  )
}

function OrganizationSkeleton() {
  return (
    <div className="animate-pulse rounded-2xl border border-slate-200 p-5">
      <div className="flex items-center gap-3">
        <div className="h-12 w-12 rounded-xl bg-slate-100" />
        <div className="flex-1 space-y-2"><div className="h-3.5 w-3/4 rounded bg-slate-100" /><div className="h-3 w-1/2 rounded bg-slate-100" /></div>
        <div className="h-6 w-16 rounded-full bg-slate-100" />
      </div>
      <div className="mt-5 h-16 rounded-xl bg-slate-100" />
      <div className="mt-5 flex gap-2"><div className="h-9 flex-1 rounded-lg bg-slate-100" /><div className="h-9 w-10 rounded-lg bg-slate-100" /></div>
    </div>
  )
}

function Modal({
  children,
  onClose,
  title,
  subtitle,
  compact = false,
}: {
  children: ReactNode
  onClose: () => void
  title: string
  subtitle?: string
  compact?: boolean
}) {
  useEffect(() => {
    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') onClose()
    }
    window.addEventListener('keydown', handleKeyDown)
    return () => window.removeEventListener('keydown', handleKeyDown)
  }, [onClose])

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center overflow-y-auto bg-slate-950/55 p-3 backdrop-blur-sm sm:p-6"
      onMouseDown={(event) => { if (event.target === event.currentTarget) onClose() }}
    >
      <div role="dialog" aria-modal="true" aria-labelledby="organization-modal-title" className={`my-auto w-full ${compact ? 'max-w-md' : 'max-w-2xl'} overflow-hidden rounded-2xl bg-white shadow-2xl shadow-slate-950/20`}>
        <div className="flex items-start justify-between gap-4 border-b border-slate-100 px-5 py-4 sm:px-6">
          <div className="min-w-0">
            <h2 id="organization-modal-title" className="text-lg font-semibold tracking-tight text-slate-950">{title}</h2>
            {subtitle && <p className="mt-1 text-sm leading-5 text-slate-500">{subtitle}</p>}
          </div>
          <button type="button" onClick={onClose} aria-label="Cerrar ventana" className="rounded-lg p-2 text-slate-400 transition hover:bg-slate-100 hover:text-slate-700 focus:outline-none focus:ring-4 focus:ring-indigo-500/10">
            <Icon name="x" size="lg" />
          </button>
        </div>
        <div className="max-h-[calc(100vh-8rem)] overflow-y-auto p-5 sm:p-6">{children}</div>
      </div>
    </div>
  )
}

function Field({ label, hint, children }: { label: string; hint?: string; children: ReactNode }) {
  return (
    <label className="block min-w-0">
      <span className="mb-1.5 block text-sm font-medium text-slate-700">{label}</span>
      {children}
      {hint && <span className="mt-1.5 block text-xs leading-5 text-slate-500">{hint}</span>}
    </label>
  )
}

const fieldClass = 'h-10 w-full rounded-xl border border-slate-200 bg-white px-3 text-sm text-slate-900 outline-none transition placeholder:text-slate-400 hover:border-slate-300 focus:border-indigo-500 focus:ring-4 focus:ring-indigo-500/10'

function CredentialRow({
  label,
  value,
  copied,
  onCopy,
  secret = false,
}: {
  label: string
  value: string
  copied: boolean
  onCopy: () => void
  secret?: boolean
}) {
  const [visible, setVisible] = useState(!secret)
  return (
    <div className="flex min-w-0 items-center justify-between gap-3">
      <div className="min-w-0 flex-1">
        <p className="text-xs font-medium text-slate-500">{label}</p>
        <p className="mt-1 break-all font-mono text-sm font-semibold text-slate-900">{visible ? value : '••••••••••••'}</p>
      </div>
      <div className="flex shrink-0 items-center gap-1">
        {secret && (
          <button type="button" onClick={() => setVisible((current) => !current)} aria-label={visible ? 'Ocultar contraseña' : 'Mostrar contraseña'} className="rounded-lg p-2 text-slate-500 hover:bg-white hover:text-slate-800">
            <Icon name={visible ? 'eyeOff' : 'eye'} />
          </button>
        )}
        <button type="button" onClick={onCopy} aria-label={`Copiar ${label}`} className="inline-flex items-center gap-1 rounded-lg px-2 py-2 text-xs font-medium text-indigo-700 hover:bg-white">
          <Icon name={copied ? 'check' : 'copy'} /> {copied ? 'Copiado' : 'Copiar'}
        </button>
      </div>
    </div>
  )
}

function Spinner() {
  return <span aria-hidden="true" className="inline-block h-4 w-4 animate-spin rounded-full border-2 border-current border-r-transparent" />
}

type IconName = 'plus' | 'building' | 'users' | 'check' | 'pause' | 'play' | 'search' | 'x' | 'chevron-down' | 'calendar' | 'eye' | 'alert' | 'refresh' | 'message' | 'copy' | 'eyeOff'

function Icon({ name, className = '', size = 'md' }: { name: IconName; className?: string; size?: 'sm' | 'md' | 'lg' | 'xl' }) {
  const sizeClass = size === 'sm' ? 'h-3.5 w-3.5' : size === 'lg' ? 'h-5 w-5' : size === 'xl' ? 'h-7 w-7' : 'h-4 w-4'
  const common = { className: `${sizeClass} ${className}`, viewBox: '0 0 24 24', fill: 'none', stroke: 'currentColor', strokeWidth: 1.8, strokeLinecap: 'round' as const, strokeLinejoin: 'round' as const, 'aria-hidden': true as const }
  const paths: Record<IconName, ReactNode> = {
    plus: <><path d="M12 5v14M5 12h14" /></>,
    building: <><path d="M3 21h18M5 21V5a2 2 0 0 1 2-2h10a2 2 0 0 1 2 2v16M9 7h2m-2 4h2m4-4h2m-2 4h2m-6 10v-4h4v4" /></>,
    users: <><path d="M16 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2M10 11a4 4 0 1 0 0-8 4 4 0 0 0 0 8ZM20 8v6m3-3h-6" /></>,
    check: <><path d="m5 12 4 4L19 6" /></>,
    pause: <><path d="M8 5v14M16 5v14" /></>,
    play: <><path d="m8 5 12 7-12 7V5Z" /></>,
    search: <><circle cx="11" cy="11" r="7" /><path d="m20 20-4-4" /></>,
    x: <><path d="m18 6-12 12M6 6l12 12" /></>,
    'chevron-down': <><path d="m6 9 6 6 6-6" /></>,
    calendar: <><rect x="3" y="5" width="18" height="16" rx="2" /><path d="M16 3v4M8 3v4M3 10h18" /></>,
    eye: <><path d="M2 12s3.5-7 10-7 10 7 10 7-3.5 7-10 7-10-7-10-7Z" /><circle cx="12" cy="12" r="3" /></>,
    alert: <><path d="M12 9v4m0 4h.01M10.3 3.9 2.7 17a2 2 0 0 0 1.7 3h15.2a2 2 0 0 0 1.7-3L13.7 3.9a2 2 0 0 0-3.4 0Z" /></>,
    refresh: <><path d="M20 7v5h-5M4 17v-5h5" /><path d="M5.6 9A7 7 0 0 1 18 6l2 6M4 12l2 6a7 7 0 0 0 12.4-3" /></>,
    message: <><path d="M21 11.5a8.4 8.4 0 0 1-.9 3.8 8.5 8.5 0 0 1-7.6 4.7 8.4 8.4 0 0 1-3.8-.9L3 21l1.9-5.7a8.4 8.4 0 0 1-.9-3.8A8.5 8.5 0 0 1 8.7 3.9a8.4 8.4 0 0 1 3.8-.9h.5a8.5 8.5 0 0 1 8 8v.5Z" /></>,
    copy: <><rect x="8" y="8" width="13" height="13" rx="2" /><path d="M16 8V5a2 2 0 0 0-2-2H5a2 2 0 0 0-2 2v9a2 2 0 0 0 2 2h3" /></>,
    eyeOff: <><path d="m3 3 18 18M10.6 10.6a2 2 0 0 0 2.8 2.8" /><path d="M9.9 5.2A10.8 10.8 0 0 1 12 5c6.5 0 10 7 10 7a16 16 0 0 1-3.1 3.9M6.2 6.2C3.5 8 2 12 2 12s3.5 7 10 7a9.5 9.5 0 0 0 4.1-.9" /></>,
  }
  return <svg {...common}>{paths[name]}</svg>
}

function getInitials(name: string) {
  return name
    .trim()
    .split(/\s+/)
    .slice(0, 2)
    .map((part) => part[0]?.toUpperCase() ?? '')
    .join('') || 'OR'
}
