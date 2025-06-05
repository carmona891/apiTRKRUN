##############################
# 1. Etapa de runtime/base  #
##############################
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

#####################
# 2. Etapa de build #
#####################
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiamos el .csproj y restauramos dependencias
COPY ["TKRKUN/TKRKUN.csproj", "TKRKUN/"]
RUN dotnet restore "TKRKUN/TKRKUN.csproj"

# Copiamos el resto de archivos y compilamos
COPY . .
WORKDIR "/src/TKRKUN"
RUN dotnet build "TKRKUN.csproj" -c Release -o /app/build

#########################
# 3. Etapa de publish   #
#########################
FROM build AS publish
RUN dotnet publish "TKRKUN.csproj" -c Release -o /app/publish

############################
# 4. Etapa final (runtime) #
############################
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TKRKUN.dll"]
