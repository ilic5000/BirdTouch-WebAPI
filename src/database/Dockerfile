FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build-env
WORKDIR /src

# Default language
ENV LC_ALL en_US.UTF-8
ENV LANG en_US.UTF-8

# Build context must be {root-of-repo}/src/database
COPY . .

RUN ls -l

RUN dotnet build BirdTouch.DatabaseMigrator.csproj -c Release -o /app

FROM build-env AS publish-env
RUN dotnet publish BirdTouch.DatabaseMigrator.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/runtime:5.0 AS runtime-env
WORKDIR /app
COPY --from=publish-env /app .

RUN ls -l

ENTRYPOINT ["dotnet", "BirdTouch.DatabaseMigrator.dll"]