# Etapa 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar archivos de proyecto y restaurar dependencias
COPY ["DataNath.ApiMetadatos.csproj", "metamodelo/"]

RUN dotnet restore "metamodelo/DataNath.ApiMetadatos.csproj"

# Copiar el resto del código
COPY . metamodelo/

# Build del proyecto
WORKDIR "/src/metamodelo"
RUN dotnet build "DataNath.ApiMetadatos.csproj" -c Release -o /app/build

# Etapa 2: Publish
FROM build AS publish
RUN dotnet publish "DataNath.ApiMetadatos.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Etapa 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Crear directorio para logs
RUN mkdir -p /app/logs

# Copiar archivos publicados
COPY --from=publish /app/publish .

# Exponer puertos
EXPOSE 5223

# Variables de entorno por defecto (pueden sobrescribirse)
ENV ASPNETCORE_URLS=http://+:5223
ENV ASPNETCORE_ENVIRONMENT=Production

# Punto de entrada
ENTRYPOINT ["dotnet", "DataNath.ApiMetadatos.dll"]
