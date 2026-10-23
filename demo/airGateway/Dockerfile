FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src
COPY . .

RUN dotnet restore ./demo/airGateway/air.gateway/air.gateway.csproj
RUN dotnet publish ./demo/airGateway/air.gateway/air.gateway.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    TZ=Asia/Shanghai

EXPOSE 8080

COPY --from=build /app/publish/ ./

RUN mkdir -p /app/config /app/plugins /app/logs /app/data \
    && ln -snf /usr/share/zoneinfo/Asia/Shanghai /etc/localtime \
    && echo Asia/Shanghai > /etc/timezone

ENTRYPOINT ["dotnet", "air.gateway.dll"]
