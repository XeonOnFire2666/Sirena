FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base

RUN apt-get update && apt-get install -y --no-install-recommends \
    libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*

USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["web/SirenaAPI.API.csproj", "web/"]
COPY ["web.Adapters/SirenaAPI.Adapters.csproj", "web.Adapters/"]
COPY ["web.Logic/SirenaAPI.Logic.csproj", "web.Logic/"]
COPY ["web.Application/SirenaAPI.Application.csproj", "web.Application/"]
COPY ["web.Infrastructure/SirenaAPI.Infrastructure.csproj", "web.Infrastructure/"]

RUN dotnet restore "web/SirenaAPI.API.csproj"
COPY . .

WORKDIR "/src/web"
RUN dotnet build "./SirenaAPI.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./SirenaAPI.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SirenaAPI.API.dll"]