# 🚀 Deploy Rápido - Credixia

Guía ultra-rápida para desplegar. Para detalles completos ver [DEPLOYMENT.md](./DEPLOYMENT.md)

## 📝 Checklist de Deploy

### 1️⃣ GitHub Secrets (5 min)
Ir a: `Settings → Secrets and variables → Actions`

Agregar:
- ✅ `DOCKERHUB_USERNAME` = `luisvalle1`
- ✅ `DOCKERHUB_TOKEN` = (crear en https://hub.docker.com/settings/security)
- ⏳ `RENDER_DEPLOY_HOOK` = (obtener después de crear servicio en Render)

---

### 2️⃣ Docker Hub (2 min)
1. Login en https://hub.docker.com
2. Create Repository
   - Name: `lending-api`
   - Visibility: Public

---

### 3️⃣ Backend en Render (10 min)

**Crear servicio:**
1. https://render.com → New → Web Service
2. Conectar repo de GitHub
3. Configurar:
   - Name: `lending-api`
   - Runtime: Docker
   - Plan: Free
   - Image URL: `luisvalle1/lending-api:latest`

**Variables de entorno:**
```bash
ConnectionStrings__Postgres=postgresql://neondb_owner:npg_TNaLm8rZYt3j@ep-floral-rice-b5hpfjuk-pooler.c-7.us-east-2.aws.neon.tech/neondb?sslmode=require

Jwt__SigningKey=GENERAR_CLAVE_ALEATORIA_MINIMO_32_CARACTERES

Auth__PublicUrl=https://CAMBIAR-DESPUES.netlify.app

Cors__AllowedOrigins__0=https://CAMBIAR-DESPUES.netlify.app

Database__MigrateOnStartup=true

Bootstrap__Email=admin@credixia.com
Bootstrap__Password=Credixia2024!
Bootstrap__FullName=Administrador Sistema
```

**⚠️ Generar clave JWT:**
```bash
# Linux/Mac:
openssl rand -base64 32

# Windows PowerShell:
[Convert]::ToBase64String((1..32 | ForEach-Object { Get-Random -Maximum 256 }))

# Online:
https://generate-random.org/api-token-generator
```

**Obtener Deploy Hook:**
1. Settings → Deploy Hook
2. Copiar URL
3. Agregar como secret `RENDER_DEPLOY_HOOK` en GitHub

**URL del backend:** `https://lending-api.onrender.com` (o tu nombre)

---

### 4️⃣ Frontend en Netlify (5 min)

**Actualizar URL del backend:**
```bash
# Editar frontend/.env.production
VITE_API_BASE_URL=https://lending-api.onrender.com
```

**Commit y push:**
```bash
git add frontend/.env.production
git commit -m "chore: configure production API URL"
git push
```

**Crear sitio:**
1. https://app.netlify.com → Add new site → Import from Git
2. Seleccionar repositorio
3. Configurar:
   - Branch: `main`
   - Base directory: `frontend`
   - Build command: `npm run build`
   - Publish directory: `frontend/dist`
4. Deploy site

**URL del frontend:** `https://tu-app.netlify.app`

---

### 5️⃣ Actualizar CORS (2 min)

Una vez que tengas la URL de Netlify:

1. Ir a Render → lending-api → Environment
2. Actualizar:
   ```bash
   Auth__PublicUrl=https://tu-app.netlify.app
   Cors__AllowedOrigins__0=https://tu-app.netlify.app
   ```
3. Save (se reiniciará automáticamente)

---

## ✅ Verificación

### Health Check:
```bash
curl https://lending-api.onrender.com/health/ready
# Respuesta esperada: "Healthy"
```

### Login:
1. Abrir `https://tu-app.netlify.app`
2. Login con:
   - Email: `admin@credixia.com`
   - Password: `Credixia2024!`

---

## 🔄 Deploys Futuros

**Backend:**
- Push a `main` → GitHub Actions construye → Sube a Docker Hub → Render redeploy (automático)

**Frontend:**
- Push a `main` → Netlify detecta → Build → Deploy (automático)

---

## ⚠️ Importante

### Cold Starts en Render (plan free):
- Después de 15 min sin actividad, el servicio se "duerme"
- Primera petición: 30-60 segundos
- Peticiones subsecuentes: instantáneas

**Solución:**
- Plan Starter ($7/mes): Sin cold starts
- Ping cada 10 min con cron job gratuito

### Cambiar después del deploy:
1. ✅ Password del admin
2. ✅ Rotar `Jwt__SigningKey`
3. ✅ Configurar dominio personalizado (opcional)

---

## 📊 Costos: $0/mes

- Netlify Free: 100GB bandwidth
- Render Free: 750 horas/mes
- Neon Free: 0.5GB storage
- Docker Hub Free: 1 repo privado

---

## 🆘 Problemas?

Ver troubleshooting completo en [DEPLOYMENT.md](./DEPLOYMENT.md)

**Más comunes:**
- CORS error → Verificar `Cors__AllowedOrigins__0` en Render
- 401 Unauthorized → Verificar `Auth__PublicUrl` en Render
- Migrations error → Verificar `Database__MigrateOnStartup=true`
