FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["EnterpriseDashboard.Api/EnterpriseDashboard.Api.csproj", "EnterpriseDashboard.Api/"]
RUN dotnet restore "EnterpriseDashboard.Api/EnterpriseDashboard.Api.csproj"
COPY . .
WORKDIR "/src/EnterpriseDashboard.Api"
RUN dotnet build "EnterpriseDashboard.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "EnterpriseDashboard.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EnterpriseDashboard.Api.dll"]
