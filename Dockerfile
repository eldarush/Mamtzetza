FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY NuGet.config .
COPY packages/ packages/
COPY src/ src/
COPY tests/ tests/
COPY OmegaFireflyComponent.slnx .

RUN dotnet restore OmegaFireflyComponent.slnx --configfile NuGet.config
RUN dotnet publish src/OmegaFireflyComponent/OmegaFireflyComponent.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "OmegaFireflyComponent.dll"]
