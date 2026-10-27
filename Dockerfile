
# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["Mango.Services.OrderAPI.csproj", "./"]

RUN --mount=type=secret,id=nugetconfig,target=/run/secrets/nuget.config,required=true \
    dotnet restore "Mango.Services.OrderAPI.csproj" \
    --configfile /run/secrets/nuget.config

COPY . .

RUN dotnet build "Mango.Services.OrderAPI.csproj" \
    -c $BUILD_CONFIGURATION \
    --no-restore \
    -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release

RUN dotnet publish "Mango.Services.OrderAPI.csproj" \
    -c $BUILD_CONFIGURATION \
    --no-restore \
    -o /app/publish \
    /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Mango.Services.OrderAPI.dll"]
