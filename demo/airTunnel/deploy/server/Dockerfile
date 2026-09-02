FROM mcr.microsoft.com/dotnet/aspnet:10.0

WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    TZ=Asia/Shanghai

EXPOSE 8080/tcp 8081/tcp 18443/tcp 15671/tcp 15671/udp

COPY publish/ ./

RUN mkdir -p /app/config /app/configs /app/logs /app/data \
    && ln -snf /usr/share/zoneinfo/Asia/Shanghai /etc/localtime \
    && echo Asia/Shanghai > /etc/timezone

ENTRYPOINT ["dotnet", "air.cloud.airtunnel.dll"]
