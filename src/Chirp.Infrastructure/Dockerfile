FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS base

# Build
FROM base AS build
WORKDIR /src

COPY src/Chirp.Core ./Chirp.Core
COPY src/Chirp.Infrastructure ./Chirp.Infrastructure

RUN dotnet restore Chirp.Infrastructure
RUN dotnet publish -c Release -o /app/publish Chirp.Infrastructure

# Run
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runner
RUN apk update && apk upgrade --no-cache
WORKDIR /app
COPY --from=build /app/publish .

USER $APP_UID

ENTRYPOINT ["dotnet", "Chirp.Infrastructure.dll"]