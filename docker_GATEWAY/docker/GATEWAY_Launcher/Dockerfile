FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/GATEWAY_Launcher/GATEWAY_Launcher.csproj", "Launcher/"]
COPY ["src/GATEWAY_Core/GATEWAY_Core.csproj", "GATEWAY_Core/"]
RUN dotnet restore "Launcher/GATEWAY_Launcher.csproj"

COPY src/GATEWAY_Launcher/. ./Launcher/
COPY src/GATEWAY_Core/. ./GATEWAY_Core/
RUN dotnet publish "Launcher/GATEWAY_Launcher.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 5001
ENV ASPNETCORE_URLS=http://+:5001
ENTRYPOINT ["dotnet", "GATEWAY_Launcher.dll"]