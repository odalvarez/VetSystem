FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["NotificationsService.API/NotificationsService.API.csproj",                   "NotificationsService.API/"]
COPY ["NotificationsService.Application/NotificationsService.Application.csproj",   "NotificationsService.Application/"]
COPY ["NotificationsService.Infrastructure/NotificationsService.Infrastructure.csproj", "NotificationsService.Infrastructure/"]
COPY ["NotificationsService.Domain/NotificationsService.Domain.csproj",             "NotificationsService.Domain/"]
RUN dotnet restore "NotificationsService.API/NotificationsService.API.csproj"

COPY . .
WORKDIR "/src/NotificationsService.API"
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "NotificationsService.API.dll"]
