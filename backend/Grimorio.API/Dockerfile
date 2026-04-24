FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Grimorio.API/Grimorio.API.csproj Grimorio.API/
COPY Grimorio.Application/Grimorio.Application.csproj Grimorio.Application/
COPY Grimorio.Domain/Grimorio.Domain.csproj Grimorio.Domain/
COPY Grimorio.Infrastructure/Grimorio.Infrastructure.csproj Grimorio.Infrastructure/
COPY Grimorio.SharedKernel/Grimorio.SharedKernel.csproj Grimorio.SharedKernel/

RUN dotnet restore Grimorio.API/Grimorio.API.csproj

COPY . .
WORKDIR /src/Grimorio.API
RUN dotnet publish Grimorio.API.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080

RUN mkdir -p /app/logs /app/secrets

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "Grimorio.API.dll"]
