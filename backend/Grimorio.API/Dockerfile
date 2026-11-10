FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY backend/Grimorio.API/Grimorio.API.csproj backend/Grimorio.API/
COPY backend/Grimorio.Application/Grimorio.Application.csproj backend/Grimorio.Application/
COPY backend/Grimorio.Domain/Grimorio.Domain.csproj backend/Grimorio.Domain/
COPY backend/Grimorio.Infrastructure/Grimorio.Infrastructure.csproj backend/Grimorio.Infrastructure/
COPY backend/Grimorio.SharedKernel/Grimorio.SharedKernel.csproj backend/Grimorio.SharedKernel/

RUN dotnet restore backend/Grimorio.API/Grimorio.API.csproj

COPY backend/ backend/
COPY docs/manual/ docs/manual/
WORKDIR /src/backend/Grimorio.API
RUN dotnet publish Grimorio.API.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends libatomic1 libdrm2 libgtk-3-0 \
    && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_URLS=http://+:8080

RUN mkdir -p /app/logs /app/secrets

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "Grimorio.API.dll"]
