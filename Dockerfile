# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

COPY src/Api/*.csproj ./src/Api/
COPY src/Core/*.csproj ./src/Core/
COPY src/Infrastructure/*.csproj ./src/Infrastructure/
RUN dotnet restore src/Api/Api.csproj

COPY src/ ./src/
RUN dotnet publish src/Api/Api.csproj -c Release -o /app --no-restore

# Stage 2: Runtime only
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Api.dll"]