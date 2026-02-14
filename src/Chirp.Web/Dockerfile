FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS base

# Build
FROM base AS build
WORKDIR /src

COPY src/Chirp.Core ./Chirp.Core
COPY src/Chirp.Infrastructure ./Chirp.Infrastructure
COPY src/Chirp.Web ./Chirp.Web

RUN dotnet restore Chirp.Web

RUN dotnet publish -c Release -o /app/publish Chirp.Web

# Run
FROM base AS runner
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Chirp.Web.dll"]