# Standalone AirTunnel Server

This deployment runs the lean `air.cloud.airtunnel` host. It does not load Ocelot, Consul, or airGateway plugins.

## Ports

- `18083 -> 8080`: Gateway-native dynamic HTTP tunnel routing.
- `18084 -> 8081`: AirTunnel management page and management API.
- `18443 -> 18443`: Agent mTLS/gRPC connection endpoint.
- `15671/tcp` and `15671/udp`: TCP/UDP tunnel listeners.

## Prepare

```bash
cp runtime/appsettings.json.example runtime/appsettings.json
cp secrets/server.env.example secrets/server.env
mkdir -p data/airtunnel data/airtunnel-config data/airtunnel-config-backups logs
```

Place the matching Server CA and certificate material in `data/airtunnel/`:

```text
ca.pfx
ca.crt
server.crt
server.key
```

The Agent-derived HTTP route catalog is loaded from `data/airtunnel-config/`. The Agent must be connected and its desired configuration must contain the public path and local service mapping before a request can be forwarded.

## Build and run

```bash
docker build -t air-cloud-airtunnel-server:latest .
docker compose -f docker-compose.server.yml up -d
```

Check the management page at `http://<server-host>:18084/_airtunnel/admin` and send tunnel HTTP requests to `http://<server-host>:18083/<public-path>`.
