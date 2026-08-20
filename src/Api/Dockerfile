FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Lumora.slnx ./
COPY src/Api/Lumora.Server.Api.csproj src/Api/
COPY src/Application/Lumora.Server.Application.csproj src/Application/
COPY src/Infrastructure/Lumora.Server.Infrastructure.csproj src/Infrastructure/
COPY src/Domain/Lumora.Server.Domain.csproj src/Domain/
COPY src/Contracts/Lumora.Contracts.csproj src/Contracts/
RUN dotnet restore src/Api/Lumora.Server.Api.csproj

COPY src/Api/ src/Api/
COPY src/Application/ src/Application/
COPY src/Infrastructure/ src/Infrastructure/
COPY src/Domain/ src/Domain/
COPY src/Contracts/ src/Contracts/
RUN dotnet publish src/Api/Lumora.Server.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Lumora.Server.Api.dll"]
