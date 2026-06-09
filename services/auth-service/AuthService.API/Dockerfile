FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
# curl se necesita para el healthcheck de docker-compose
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["AuthService.API/AuthService.API.csproj",           "AuthService.API/"]
COPY ["AuthService.Application/AuthService.Application.csproj", "AuthService.Application/"]
COPY ["AuthService.Infrastructure/AuthService.Infrastructure.csproj", "AuthService.Infrastructure/"]
COPY ["AuthService.Domain/AuthService.Domain.csproj",     "AuthService.Domain/"]
RUN dotnet restore "AuthService.API/AuthService.API.csproj"

COPY . .
WORKDIR "/src/AuthService.API"
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AuthService.API.dll"]
