# ADR 0003: Sesión y autenticación

- Estado: aceptado (implementación en fase 2)
- Fecha: 2026-10-29

## Contexto

El portal maneja información financiera sensible y debe resistir robo de tokens por XSS
y reutilización de refresh tokens.

## Decisión

- El **access token** se mantiene únicamente en memoria del navegador.
- El **refresh token** viaja en una cookie `httpOnly`, `Secure` y `SameSite` adecuada,
  con rotación en cada uso y detección de reutilización.
- El backend valida el tenant desde la sesión y la membresía activa del usuario.
- Los permisos se evalúan por rol y recurso; el ocultamiento en la UI no sustituye la
  autorización del servidor.
- El acceso de soporte de plataforma usa un mecanismo **break-glass** explícito,
  auditado y con expiración.

## Consecuencias

- El cliente no persiste tokens en `localStorage` ni en `sessionStorage`.
- La revocación requiere estado del lado servidor para refresh tokens.
- La fase 2 debe incluir pruebas de expiración, rotación, reutilización y acceso cruzado.
