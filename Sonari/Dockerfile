FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Sonari/Sonari.csproj", "Sonari/"]
RUN dotnet restore "Sonari/Sonari.csproj"
COPY . .
WORKDIR "/src/Sonari"
RUN dotnet build "Sonari.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Sonari.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Sonari.dll"]
