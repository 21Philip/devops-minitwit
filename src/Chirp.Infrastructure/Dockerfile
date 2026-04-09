FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine

RUN dotnet tool install --global dotnet-ef
ENV PATH="/root/.dotnet/tools:${PATH}"

WORKDIR /src
COPY src/Chirp.Core ./Chirp.Core
COPY src/Chirp.Infrastructure ./Chirp.Infrastructure
COPY src/Chirp.Api ./Chirp.Api

RUN dotnet restore Chirp.Core
RUN dotnet restore Chirp.Infrastructure
RUN dotnet restore Chirp.Api

ENTRYPOINT ["dotnet-ef", "database", "update", "--project", "Chirp.Infrastructure", "--startup-project", "Chirp.Api"]