FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Core/Core.csproj", "Core/"]
RUN dotnet restore "Core/Core.csproj"

COPY . .
RUN dotnet build "Core/Core.csproj" -c Release -o /app/build

FROM mcr.microsoft.com/dotnet/aspnet:10.0
RUN apt-get update \
    && apt-get install -y --no-install-recommends dbus \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/build .

EXPOSE 5000
ENTRYPOINT ["dotnet", "Core.dll"]