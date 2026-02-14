FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS base

# Build
FROM base AS build
WORKDIR /src

COPY src/Chirp.Core ./Chirp.Core
COPY src/Chirp.Infrastructure ./Chirp.Infrastructure
COPY src/Chirp.Api ./Chirp.Api

RUN dotnet restore Chirp.Api

RUN dotnet publish -c Release -o /app/publish Chirp.Api

# Run
FROM base AS runner
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Chirp.Api.dll"]