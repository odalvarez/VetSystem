FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["PatientsService.API/PatientsService.API.csproj",             "PatientsService.API/"]
COPY ["PatientsService.Application/PatientsService.Application.csproj", "PatientsService.Application/"]
COPY ["PatientsService.Infrastructure/PatientsService.Infrastructure.csproj", "PatientsService.Infrastructure/"]
COPY ["PatientsService.Domain/PatientsService.Domain.csproj",       "PatientsService.Domain/"]
RUN dotnet restore "PatientsService.API/PatientsService.API.csproj"

COPY . .
WORKDIR "/src/PatientsService.API"
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "PatientsService.API.dll"]
