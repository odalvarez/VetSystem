# Etapa 1: compilar el Blazor WASM con el SDK de .NET
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["VetSystem.Frontend.csproj", "."]
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release -o /app/publish --no-restore

# Etapa 2: servir los archivos estáticos con nginx
# Blazor WASM es puramente estático una vez publicado; nginx es suficiente
FROM nginx:1.27-alpine AS final
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html
COPY nginx/default.conf nginx/cache.conf /etc/nginx/conf.d/
COPY nginx/security-headers.inc /etc/nginx/snippets/
# El entrypoint oficial pasa por envsubst lo que hay en /etc/nginx/templates/ y deja el resultado en conf.d,
# reemplazando ${INTERNAL_KEY}; el filtro impide que otra variable de entorno del contenedor se cuele en la configuración
COPY nginx/app.inc.template /etc/nginx/templates/
ENV NGINX_ENVSUBST_FILTER=^INTERNAL_KEY$
EXPOSE 80 443
