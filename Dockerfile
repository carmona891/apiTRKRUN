# Dockerfile optimizado para estructura TRKRUN/TRKRUN/
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE $PORT
ENV ASPNETCORE_URLS=http://0.0.0.0:$PORT

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar archivo de proyecto desde la subcarpeta TRKRUN
COPY ["TRKRUN/TRKRUN.csproj", "TRKRUN/"]
RUN dotnet restore "TRKRUN/TRKRUN.csproj"

# Copiar el resto de archivos y compilar
COPY . .
WORKDIR "/src/TRKRUN"
RUN dotnet build "TRKRUN.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "TRKRUN.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TRKRUN.dll"]