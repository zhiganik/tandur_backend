# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

COPY src/Api/*.csproj ./src/Api/
COPY src/Core/*.csproj ./src/Core/
COPY src/Infrastructure/*.csproj ./src/Infrastructure/
COPY src/Cli/*.csproj ./src/Cli/
RUN dotnet restore src/Api/Api.csproj && dotnet restore src/Cli/Cli.csproj

COPY src/ ./src/
RUN dotnet publish src/Api/Api.csproj -c Release -o /app/api --no-restore
RUN dotnet publish src/Cli/Cli.csproj -c Release -o /app/cli --no-restore

# Stage 2: API runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS api
WORKDIR /app
COPY --from=build /app/api .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Api.dll"]

# Stage 3: CLI runtime
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS cli
WORKDIR /app
COPY --from=build /app/cli .
ENTRYPOINT ["dotnet", "Cli.dll"]
