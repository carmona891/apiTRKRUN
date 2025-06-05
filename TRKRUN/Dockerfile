# Dockerfile optimizado para Railway
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE $PORT
ENV ASPNETCORE_URLS=http://0.0.0.0:$PORT

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar archivo de proyecto primero (para cache de Docker)
COPY *.csproj ./
RUN dotnet restore

# Copiar el resto de archivos y compilar
COPY . ./
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "TRKRUN.dll"]
