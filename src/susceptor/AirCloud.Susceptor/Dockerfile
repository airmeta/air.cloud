#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/susceptor/AirCloud.Susceptor/AirCloud.Susceptor.csproj", "src/susceptor/AirCloud.Susceptor/"]
RUN dotnet restore "./src/susceptor/AirCloud.Susceptor/AirCloud.Susceptor.csproj"
COPY . .
WORKDIR "/src/src/susceptor/AirCloud.Susceptor"
RUN dotnet build "./AirCloud.Susceptor.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./AirCloud.Susceptor.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AirCloud.Susceptor.dll"]