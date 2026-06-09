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
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
