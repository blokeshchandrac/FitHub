FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY backend/FitHub.sln backend/
COPY backend/src/FitHub.Api/FitHub.Api.csproj backend/src/FitHub.Api/
COPY backend/src/FitHub.Application/FitHub.Application.csproj backend/src/FitHub.Application/
COPY backend/src/FitHub.Domain/FitHub.Domain.csproj backend/src/FitHub.Domain/
COPY backend/src/FitHub.Infrastructure/FitHub.Infrastructure.csproj backend/src/FitHub.Infrastructure/
COPY backend/tests/FitHub.UnitTests/FitHub.UnitTests.csproj backend/tests/FitHub.UnitTests/
COPY backend/tests/FitHub.IntegrationTests/FitHub.IntegrationTests.csproj backend/tests/FitHub.IntegrationTests/
RUN dotnet restore backend/src/FitHub.Api/FitHub.Api.csproj
COPY backend/ backend/
RUN dotnet publish backend/src/FitHub.Api/FitHub.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "FitHub.Api.dll"]
