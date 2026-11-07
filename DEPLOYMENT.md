# 🚀 Guía de Despliegue - Credixia

<!-- Last updated: 2026-11-06 -->

Esta guía detalla cómo desplegar el proyecto en producción usando:
- **Frontend**: Netlify (hosting estático + CDN)
- **Backend**: Render (contenedor Docker desde Docker Hub)
- **Base de datos**: Neon (PostgreSQL serverless)

---

## 📋 Prerequisitos

1. **Cuentas necesarias:**
   - ✅ GitHub (repositorio del código)
   - ✅ Docker Hub (usuario: `luisvalle1`)
   - ✅ Netlify (hosting frontend)
   - ✅ Render (hosting backend)
   - ✅ Neon (base de datos PostgreSQL)

2. **Información de la base de datos:**
   ```
   Connection String: postgresql://neondb_owner:npg_TNaLm8rZYt3j@ep-floral-rice-b5hpfjuk-pooler.c-7.us-east-2.aws.neon.tech/neondb?sslmode=require&channel_binding=require
   ```

---

## 🔧 Parte 1: Configuración de GitHub Secrets

Para que GitHub Actions pueda construir y subir la imagen Docker, necesitas configurar estos secrets:

1. Ve a tu repositorio en GitHub
2. Settings → Secrets and variables → Actions
3. Agrega estos secrets:

| Secret Name | Valor | Descripción |
|------------|-------|-------------|
| `DOCKERHUB_USERNAME` | `luisvalle1` | Tu usuario de Docker Hub |
| `DOCKERHUB_TOKEN` | `(tu token)` | Token de acceso de Docker Hub |
| `RENDER_DEPLOY_HOOK` | `(se configura después)` | Webhook de Render para deploy automático |

### Cómo obtener DOCKERHUB_TOKEN:

1. Ir a https://hub.docker.com/settings/security
2. Click en "New Access Token"
3. Nombre: `github-actions-lending-api`
4. Access permissions: Read, Write, Delete
5. Copiar el token (solo se muestra una vez)

---

## 🐳 Parte 2: Configuración de Docker Hub

1. Ir a https://hub.docker.com
2. Login con `luisvalle1`
3. Create Repository → Create
   - Name: `lending-api`
   - Visibility: Public (o Private si prefieres)
   - Description: "Credixia - Multi-tenant Loan Management API"

**Nota:** El workflow ya está configurado para usar `luisvalle1/lending-api:latest`

---

## 🖥️ Parte 3: Desplegar Backend en Render

### 3.1 Crear el servicio

1. Ir a https://render.com/
2. Dashboard → New → Web Service
3. Conectar tu repositorio de GitHub
4. Configuración:

| Campo | Valor |
|-------|-------|
| **Name** | `lending-api` (o el nombre que prefieras) |
| **Region** | `Oregon (US West)` (o el más cercano) |
| **Branch** | `main` |
| **Runtime** | `Docker` |
| **Docker Command** | *(dejar vacío)* |
| **Plan** | `Free` (para empezar) |

### 3.2 Variables de entorno

En la sección "Environment Variables", agregar:

```bash
# Base de datos Neon
ConnectionStrings__Postgres=postgresql://neondb_owner:npg_TNaLm8rZYt3j@ep-floral-rice-b5hpfjuk-pooler.c-7.us-east-2.aws.neon.tech/neondb?sslmode=require

# JWT (generar una clave segura única)
Jwt__SigningKey=TU_CLAVE_SUPER_SECRETA_MINIMO_32_CARACTERES_AQUI

# URL del frontend (actualizar después de crear en Netlify)
Auth__PublicUrl=https://tu-app.netlify.app

# CORS - Permitir el frontend de Netlify
Cors__AllowedOrigins__0=https://tu-app.netlify.app

# Migraciones automáticas en primera ejecución
Database__MigrateOnStartup=true

# Usuario administrador inicial
Bootstrap__Email=admin@credixia.com
Bootstrap__Password=Credixia2024!
Bootstrap__FullName=Administrador Sistema

# OpenAPI (opcional, para ver documentación)
Api__ExposeOpenApi=false
```

**⚠️ IMPORTANTE:** 
- Reemplazar `TU_CLAVE_SUPER_SECRETA_MINIMO_32_CARACTERES_AQUI` con una clave aleatoria de al menos 32 caracteres
- Puedes generar una con: `openssl rand -base64 32` (en Linux/Mac) o usar un generador online

### 3.3 Configurar deploy desde Docker Hub

1. En la configuración del servicio, ir a **Settings**
2. Scroll hasta **Image URL**
3. Cambiar a: `luisvalle1/lending-api:latest`
4. Save Changes

### 3.4 Obtener Deploy Hook (para CI/CD automático)

1. En el servicio de Render, ir a **Settings**
2. Scroll hasta **Deploy Hook**
3. Copiar la URL (algo como: `https://api.render.com/deploy/srv-xxxxx?key=yyyyy`)
4. Agregar este valor como secret `RENDER_DEPLOY_HOOK` en GitHub

### 3.5 Primera migración

Después de crear el servicio:
1. Render construirá e iniciará el contenedor
2. Las migraciones se ejecutarán automáticamente (`Database__MigrateOnStartup=true`)
3. Se creará el usuario administrador con las credenciales de `Bootstrap__*`
4. Verificar en Logs que todo inició correctamente

**Tu backend estará en:** `https://lending-api.onrender.com` (o el nombre que elegiste)

---

## 🌐 Parte 4: Desplegar Frontend en Netlify

### 4.1 Actualizar URL del backend

1. Editar `frontend/.env.production`:
   ```bash
   VITE_API_BASE_URL=https://lending-api.onrender.com
   ```
   (Reemplazar con la URL real de Render)

2. Commit y push:
   ```bash
   git add frontend/.env.production
   git commit -m "chore: configure production API URL"
   git push
   ```

### 4.2 Crear el sitio en Netlify

1. Ir a https://app.netlify.com/
2. Add new site → Import an existing project
3. Conectar con GitHub y seleccionar tu repositorio
4. Configuración de build:

| Campo | Valor |
|-------|-------|
| **Branch** | `main` |
| **Build command** | `npm run build` |
| **Publish directory** | `frontend/dist` |
| **Base directory** | `frontend` |

5. Click en "Deploy site"

### 4.3 Configurar dominio personalizado (opcional)

1. Site settings → Domain management
2. Add custom domain → Ingresar tu dominio
3. Seguir instrucciones para configurar DNS

### 4.4 Actualizar CORS en Render

Una vez que tengas la URL de Netlify (ej: `https://credixia.netlify.app`):

1. Ir a Render → Tu servicio → Environment
2. Actualizar variables:
   ```bash
   Auth__PublicUrl=https://credixia.netlify.app
   Cors__AllowedOrigins__0=https://credixia.netlify.app
   ```
3. Save Changes (el servicio se reiniciará automáticamente)

---

## 🔄 Flujo de Deploy Automático

### Backend (cada push a main que modifique backend):
1. GitHub Actions detecta cambios en `backend/**`
2. Construye la imagen Docker
3. Sube a Docker Hub como `luisvalle1/lending-api:latest`
4. Trigger del webhook de Render
5. Render descarga la nueva imagen y redeploy (~2 min)

### Frontend (cada push a main):
1. Netlify detecta el push
2. Ejecuta `npm run build` en `frontend/`
3. Publica el resultado en CDN (~1 min)

---

## 🧪 Verificación Post-Despliegue

### Backend Health Check:
```bash
curl https://lending-api.onrender.com/health/ready
# Debe responder: "Healthy"
```

### Frontend:
1. Abrir `https://tu-app.netlify.app`
2. Ir a Login
3. Usar credenciales del admin:
   - Email: `admin@credixia.com`
   - Password: `Credixia2024!`

### Verificar logs:
- **Render**: Dashboard → lending-api → Logs
- **Netlify**: Site → Deploys → (último deploy) → Deploy log

---

## ⚡ Sobre Cold Starts en Render (Plan Free)

En el plan gratuito de Render:
- El servicio se "duerme" después de 15 minutos sin actividad
- Primera petición después de dormir: **30-60 segundos**
- Peticiones subsecuentes: instantáneas

**Soluciones:**
1. **Plan Starter ($7/mes)**: Elimina cold starts
2. **Cron job gratuito**: Hacer ping cada 10 min para mantener vivo
3. **Uptime monitoring**: Servicios como UptimeRobot (gratis)

---

## 🔐 Seguridad Post-Despliegue

1. **Cambiar credenciales del admin** después del primer login
2. **Rotar `Jwt__SigningKey`** periódicamente
3. **Habilitar 2FA** en Docker Hub, GitHub, Netlify y Render
4. **Monitorear logs** de accesos sospechosos
5. **Configurar SSL/TLS** (automático en Netlify y Render)

---

## 📊 Costos Mensuales

| Servicio | Plan | Costo |
|----------|------|-------|
| Netlify | Starter | $0 (100GB bandwidth) |
| Render | Free | $0 (750 horas/mes) |
| Neon | Free | $0 (0.5GB storage) |
| Docker Hub | Free | $0 (1 repo privado) |
| **TOTAL** | | **$0/mes** |

**Para escalar:**
- Render Starter: $7/mes (sin cold starts)
- Neon Pro: $19/mes (más storage y compute)
- Netlify Pro: $19/mes (más bandwidth)

---

## 🆘 Troubleshooting

### Backend no inicia:
- Verificar logs en Render
- Confirmar que `ConnectionStrings__Postgres` es correcto
- Verificar que `Jwt__SigningKey` tiene al menos 32 caracteres

### Frontend no conecta al backend:
- Verificar `VITE_API_BASE_URL` en `.env.production`
- Confirmar CORS en Render: `Cors__AllowedOrigins__0`
- Verificar Network tab en DevTools del navegador

### Error de CORS:
```bash
# Verificar que ambos valores coincidan:
# En Render Environment:
Cors__AllowedOrigins__0=https://tu-app.netlify.app

# En frontend/.env.production:
VITE_API_BASE_URL=https://tu-backend.onrender.com
```

### Migraciones no se ejecutan:
```bash
# En Render Environment, verificar:
Database__MigrateOnStartup=true
```

---

## 📚 Referencias

- [Netlify Docs](https://docs.netlify.com/)
- [Render Docs](https://render.com/docs)
- [Docker Hub](https://docs.docker.com/docker-hub/)
- [Neon Docs](https://neon.tech/docs)
- [GitHub Actions](https://docs.github.com/en/actions)

---

¿Preguntas? Revisa los logs primero, luego contacta al equipo de DevOps.
