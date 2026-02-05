FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Copy everything
COPY ./src ./app/

# Set working directory
WORKDIR /app/src/Chirp.Web

# Build
RUN dotnet build

# Run the project
ENTRYPOINT ["dotnet", "run"]