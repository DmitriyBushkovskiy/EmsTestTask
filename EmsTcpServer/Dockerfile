FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["EmsTcpServer/EmsTcpServer.csproj", "EmsTcpServer/"]
RUN dotnet restore "EmsTcpServer/EmsTcpServer.csproj"
COPY . .
WORKDIR "/src/EmsTcpServer"
RUN dotnet build "./EmsTcpServer.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./EmsTcpServer.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EmsTcpServer.dll"]
