export type EndpointGroup = 'Sistema' | 'Salud'

export interface ApiEndpoint {
  method: 'GET'
  path: string
  title: string
  description: string
  group: EndpointGroup
  anonymous: boolean
}

export const API_ENDPOINTS: readonly ApiEndpoint[] = [
  {
    method: 'GET',
    path: '/api/v1/ping',
    title: 'Ping del servicio',
    description:
      'Identidad del servicio, entorno, versión y hora UTC del proceso.',
    group: 'Sistema',
    anonymous: true,
  },
  {
    method: 'GET',
    path: '/health/live',
    title: 'Liveness',
    description: 'Confirma que el proceso HTTP está vivo y responde.',
    group: 'Salud',
    anonymous: true,
  },
  {
    method: 'GET',
    path: '/health/ready',
    title: 'Readiness',
    description:
      'Verifica las dependencias críticas, incluida la conexión a PostgreSQL.',
    group: 'Salud',
    anonymous: true,
  },
]

export const OPENAPI_PATHS = {
  document: '/openapi/v1.json',
  reference: '/scalar',
} as const
