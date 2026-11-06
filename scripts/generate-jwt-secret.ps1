# Script para generar una clave JWT segura
# Uso: .\scripts\generate-jwt-secret.ps1

Write-Host "🔐 Generando clave JWT segura..." -ForegroundColor Cyan
Write-Host ""

# Generar 32 bytes aleatorios y convertir a Base64
$bytes = New-Object byte[] 32
$rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
$rng.GetBytes($bytes)
$secret = [Convert]::ToBase64String($bytes)

Write-Host "✅ Clave JWT generada:" -ForegroundColor Green
Write-Host ""
Write-Host $secret -ForegroundColor Yellow
Write-Host ""
Write-Host "📋 Usa esta clave en Render como variable de entorno:" -ForegroundColor Cyan
Write-Host "   Jwt__SigningKey=$secret" -ForegroundColor White
Write-Host ""
Write-Host "⚠️  IMPORTANTE: Guarda esta clave de forma segura!" -ForegroundColor Red
Write-Host "   No la compartas ni la subas a Git." -ForegroundColor Red
