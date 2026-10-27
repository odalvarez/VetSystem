# Imagen para correr la suite E2E: navegadores y dependencias de la imagen oficial de Playwright
# (misma versión que el paquete NuGet) más el SDK de .NET 9 que usa el proyecto.
FROM mcr.microsoft.com/playwright/dotnet:v1.49.0-noble

RUN curl -sSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh \
 && bash /tmp/dotnet-install.sh --channel 9.0 --install-dir /usr/share/dotnet \
 && rm /tmp/dotnet-install.sh \
 && dotnet --list-sdks
