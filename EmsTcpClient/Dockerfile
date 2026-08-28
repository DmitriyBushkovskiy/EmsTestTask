FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["EmsTcpClient/EmsTcpClient.csproj", "EmsTcpClient/"]
RUN dotnet restore "EmsTcpClient/EmsTcpClient.csproj"
COPY . .
WORKDIR "/src/EmsTcpClient"
RUN dotnet build "./EmsTcpClient.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./EmsTcpClient.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EmsTcpClient.dll"]
