FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/GATEWAY_Core/GATEWAY_Core.csproj", "GATEWAY_Core/"]
RUN dotnet restore "GATEWAY_Core/GATEWAY_Core.csproj"

COPY src/GATEWAY_Core/. ./GATEWAY_Core/
RUN dotnet build "GATEWAY_Core/GATEWAY_Core.csproj" -c Release -o /app/build

FROM mcr.microsoft.com/dotnet/aspnet:10.0
RUN apt-get update \
    && apt-get install -y --no-install-recommends dbus \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/build .

EXPOSE 5000
ENTRYPOINT ["dotnet", "GATEWAY_Core.dll"]