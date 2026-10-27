# API image: build and publish with the .NET 8 SDK, run on the smaller ASP.NET Core 8 runtime.
# Build context: backend/ (see docker-compose.yml).

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore first, from the project files only, so the restore layer is cached until a .csproj changes.
COPY src/Requests.Api/Requests.Api.csproj src/Requests.Api/
COPY src/Requests.BL/Requests.BL.csproj src/Requests.BL/
COPY src/Requests.DAL/Requests.DAL.csproj src/Requests.DAL/
RUN dotnet restore src/Requests.Api/Requests.Api.csproj

COPY src/ src/
RUN dotnet publish src/Requests.Api/Requests.Api.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# curl for the container healthcheck (GET /api/health).
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

# Non-root user provided by the .NET 8 images.
USER $APP_UID

ENTRYPOINT ["dotnet", "Requests.Api.dll"]
