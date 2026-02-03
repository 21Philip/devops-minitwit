FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy everything
COPY . ./

# Set working directory
WORKDIR /app/src/Chirp.Web

# Run the project
ENTRYPOINT ["dotnet", "run"]