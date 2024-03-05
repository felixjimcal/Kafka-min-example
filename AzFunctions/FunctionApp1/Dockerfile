#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/azure-functions/dotnet:4 AS base
WORKDIR /home/site/wwwroot
EXPOSE 80

# Instalar librdkafka-dev
USER root
RUN apt-get update \
    && apt-get install -y --no-install-recommends librdkafka-dev \
    && rm -rf /var/lib/apt/lists/* \
    # Asegúrate de que el usuario 'appuser' exista
    && groupadd -r appuser && useradd --no-log-init -r -g appuser appuser
# Cambiar de vuelta al usuario appuser
# USER appuser

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["AzFunctions/FunctionApp1/AzureFunctionStarWars.csproj", "AzFunctions/FunctionApp1/"]
RUN dotnet restore "./AzFunctions/FunctionApp1/AzureFunctionStarWars.csproj"
COPY . .
WORKDIR "/src/AzFunctions/FunctionApp1"
RUN dotnet build "./AzureFunctionStarWars.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./AzureFunctionStarWars.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /home/site/wwwroot
COPY --from=publish /app/publish .
ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true