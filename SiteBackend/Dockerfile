FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 2124

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["SiteBackend/SiteBackend.csproj", "SiteBackend/"]
RUN dotnet restore "SiteBackend/SiteBackend.csproj"
# Copy only the backend project folder to avoid sending unrelated repo data into the build context
COPY SiteBackend/. SiteBackend/
WORKDIR "/src/SiteBackend"
RUN dotnet build "./SiteBackend.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./SiteBackend.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "SiteBackend.dll"]
