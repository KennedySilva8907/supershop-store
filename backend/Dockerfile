FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /source

COPY SuperShop.slnx ./
COPY src/SuperShop.Domain/*.csproj src/SuperShop.Domain/
COPY src/SuperShop.Application/*.csproj src/SuperShop.Application/
COPY src/SuperShop.Infrastructure/*.csproj src/SuperShop.Infrastructure/
COPY src/SuperShop.Api/*.csproj src/SuperShop.Api/
COPY tests/SuperShop.UnitTests/*.csproj tests/SuperShop.UnitTests/
COPY tests/SuperShop.IntegrationTests/*.csproj tests/SuperShop.IntegrationTests/
RUN dotnet restore src/SuperShop.Api/SuperShop.Api.csproj

COPY src/ src/
RUN dotnet publish src/SuperShop.Api/SuperShop.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app

RUN adduser --disabled-password --no-create-home --uid 1001 supershop
USER supershop

COPY --from=build --chown=supershop /app ./

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
    CMD wget --spider --quiet http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "SuperShop.Api.dll"]
