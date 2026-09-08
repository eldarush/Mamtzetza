FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY NuGet.config .
COPY packages/ packages/
COPY src/ src/
COPY tests/ tests/
COPY Mamtzetza.slnx .

RUN dotnet restore Mamtzetza.slnx --configfile NuGet.config
RUN dotnet publish src/Mamtzetza/Mamtzetza.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Mamtzetza.dll"]
