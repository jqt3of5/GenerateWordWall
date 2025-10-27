FROM mcr.microsoft.com/dotnet/runtime:5.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["WordWallGenerator/WordWallGenerator.csproj", "WordWallGenerator/"]
RUN dotnet restore "WordWallGenerator/WordWallGenerator.csproj"
COPY . .
WORKDIR "/src/WordWallGenerator"
RUN dotnet build "WordWallGenerator.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "WordWallGenerator.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WordWallGenerator.dll"]
