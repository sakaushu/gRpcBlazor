FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
RUN apk add --no-cache gcompat
WORKDIR /src

COPY ["src/GATEWAY_Launcher/GATEWAY_Launcher.csproj", "GATEWAY_Launcher/"]
COPY ["src/GATEWAY_Core/GATEWAY_Core.csproj", "GATEWAY_Core/"]
RUN dotnet restore "GATEWAY_Launcher/GATEWAY_Launcher.csproj"

COPY src/GATEWAY_Launcher/. ./GATEWAY_Launcher/
COPY src/GATEWAY_Core/. ./GATEWAY_Core/
RUN dotnet publish "GATEWAY_Launcher/GATEWAY_Launcher.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 5001
ENTRYPOINT ["dotnet", "GATEWAY_Launcher.dll"]