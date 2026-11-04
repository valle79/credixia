import { Card } from '../../components/ui/Card'
import { useAuth } from '../../hooks/useAuth'

export function SettingsPage() {
  const { user } = useAuth()

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-slate-900">Configuración</h1>
        <p className="mt-1 text-sm text-slate-600">
          Configuración del sistema y preferencias
        </p>
      </div>

      <div className="grid gap-6 lg:grid-cols-2">
        <Card>
          <div className="border-b border-slate-200 px-6 py-4">
            <h2 className="text-lg font-semibold text-slate-900">
              Perfil de Administrador
            </h2>
          </div>
          <div className="p-6 space-y-4">
            <div>
              <label className="text-xs font-medium text-slate-500 uppercase">
                Nombre Completo
              </label>
              <p className="mt-1 text-sm text-slate-900">{user?.fullName}</p>
            </div>
            <div>
              <label className="text-xs font-medium text-slate-500 uppercase">
                Email
              </label>
              <p className="mt-1 text-sm text-slate-900">{user?.email}</p>
            </div>
            <div>
              <label className="text-xs font-medium text-slate-500 uppercase">
                Rol
              </label>
              <p className="mt-1">
                <span className="inline-flex items-center gap-1.5 rounded-full bg-purple-100 px-2.5 py-1 text-xs font-medium text-purple-700">
                  Super Administrador
                </span>
              </p>
            </div>
          </div>
        </Card>

        <Card>
          <div className="border-b border-slate-200 px-6 py-4">
            <h2 className="text-lg font-semibold text-slate-900">
              Información del Sistema
            </h2>
          </div>
          <div className="p-6 space-y-4">
            <div className="flex items-center justify-between">
              <span className="text-sm text-slate-600">Versión</span>
              <span className="text-sm font-medium text-slate-900">1.0.0</span>
            </div>
            <div className="flex items-center justify-between">
              <span className="text-sm text-slate-600">Ambiente</span>
              <span className="inline-flex items-center gap-1.5 rounded-full bg-amber-100 px-2.5 py-1 text-xs font-medium text-amber-700">
                Desarrollo
              </span>
            </div>
            <div className="flex items-center justify-between">
              <span className="text-sm text-slate-600">Estado</span>
              <span className="inline-flex items-center gap-1.5 rounded-full bg-emerald-100 px-2.5 py-1 text-xs font-medium text-emerald-700">
                <span className="size-1.5 rounded-full bg-emerald-600" />
                Operativo
              </span>
            </div>
          </div>
        </Card>
      </div>

      <Card>
        <div className="border-b border-slate-200 px-6 py-4">
          <h2 className="text-lg font-semibold text-slate-900">
            Configuración de Email
          </h2>
        </div>
        <div className="p-6">
          <p className="text-sm text-slate-600">
            Configuración del servicio de envío de correos electrónicos para notificaciones
            y credenciales de organizaciones.
          </p>
          <div className="mt-4 rounded-lg border border-blue-200 bg-blue-50 px-4 py-3">
            <p className="text-sm text-blue-800">
              ℹ️ El sistema está configurado para enviar correos automáticos al crear nuevas organizaciones.
            </p>
          </div>
        </div>
      </Card>
    </div>
  )
}
