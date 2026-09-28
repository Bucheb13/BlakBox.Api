# ============================================================
# BlakBox API - .NET 10
# Dockerfile para Render
# ============================================================

# ------------------------------------------------------------
# ETAPA 1 - BUILD
# ------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

# Copia o projeto primeiro para aproveitar o cache do Docker
COPY ["BlakBox.Api.csproj", "./"]

# Restaura as dependências
RUN dotnet restore "BlakBox.Api.csproj"

# Copia o restante do projeto
COPY . .

# Compila e publica em modo Release
RUN dotnet publish "BlakBox.Api.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore


# ------------------------------------------------------------
# ETAPA 2 - RUNTIME
# ------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app

# Porta utilizada pelo Render
ENV ASPNETCORE_URLS=http://0.0.0.0:10000

# Ambiente de produção
ENV ASPNETCORE_ENVIRONMENT=Production

# Copia os arquivos publicados
COPY --from=build /app/publish .

# Expõe a porta utilizada pelo Render
EXPOSE 10000

# Inicia a BlakBox API
ENTRYPOINT ["dotnet", "BlakBox.Api.dll"]