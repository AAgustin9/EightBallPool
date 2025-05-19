FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["8-ball-pool/8-ball-pool.csproj", "8-ball-pool/"]
RUN dotnet restore "8-ball-pool/8-ball-pool.csproj"

# Copy everything else and build
COPY . .
RUN dotnet --list-sdks
# Build the application
RUN dotnet publish "8-ball-pool/8-ball-pool.csproj" -c Release -o /app/publish

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .

# Install PostgreSQL client for database connection checks
RUN apt-get update && apt-get install -y postgresql-client curl

RUN dotnet tool install --global dotnet-ef --version 9.*

# Setup entrypoint script
COPY ./docker-entrypoint.sh /app/docker-entrypoint.sh
RUN chmod +x /app/docker-entrypoint.sh

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

ENTRYPOINT ["/app/docker-entrypoint.sh"]