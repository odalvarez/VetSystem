FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["AppointmentsService.API/AppointmentsService.API.csproj",                 "AppointmentsService.API/"]
COPY ["AppointmentsService.Application/AppointmentsService.Application.csproj", "AppointmentsService.Application/"]
COPY ["AppointmentsService.Infrastructure/AppointmentsService.Infrastructure.csproj", "AppointmentsService.Infrastructure/"]
COPY ["AppointmentsService.Domain/AppointmentsService.Domain.csproj",           "AppointmentsService.Domain/"]
RUN dotnet restore "AppointmentsService.API/AppointmentsService.API.csproj"

COPY . .
WORKDIR "/src/AppointmentsService.API"
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AppointmentsService.API.dll"]
