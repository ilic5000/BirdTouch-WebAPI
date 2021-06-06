FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build-env
WORKDIR /src

# Default language
ENV LC_ALL en_US.UTF-8
ENV LANG en_US.UTF-8

# Build context must be {root-of-repo}/src/api
COPY . .

RUN ls -l

RUN dotnet build BirdTouch.WebAPI.csproj -c Release -o /app

FROM build-env AS publish-env
RUN dotnet publish BirdTouch.WebAPI.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS runtime-env
WORKDIR /app
COPY --from=publish-env /app .

RUN ls -l

ENTRYPOINT ["dotnet", "BirdTouch.WebAPI.dll"]