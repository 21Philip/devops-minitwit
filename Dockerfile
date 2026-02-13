FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS base

# Build
FROM base AS build
WORKDIR /src

COPY . .
RUN dotnet restore

RUN dotnet publish -c Release -o /app/publish

# Run
FROM base AS runner
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS="http://+:8080"
ENV DOTNET_RUNNING_IN_CONTAINER="true"

# Expose port for http
EXPOSE 8080

ENTRYPOINT ["dotnet", "Chirp.Web.dll"]