FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY global.json Directory.Build.props Directory.Packages.props ./
COPY backend/ backend/

RUN dotnet restore backend/src/Lending.Api/Lending.Api.csproj
RUN dotnet publish backend/src/Lending.Api/Lending.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Variables de entorno por defecto (se sobrescriben en Render)
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_FORWARDEDHEADERS_ENABLED=true

EXPOSE 8080

COPY --from=build /app/publish .

# Render requiere usar el puerto $PORT si está definido
CMD ASPNETCORE_URLS=http://+:${PORT:-8080} dotnet Lending.Api.dll
