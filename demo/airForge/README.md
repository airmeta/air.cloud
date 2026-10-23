# AirForge

AirForge is the standalone DynamicApp runtime product demo. It hosts the control panel, Manager API, gateway forwarding, process hosting, package storage, resource monitoring, and lifecycle governance for DynamicApp Mods.

The framework library remains in `src/dynamicapp/Air.Cloud.DynamicApp`. AirForge only provides the product host and built-in management panel.

## Run

```powershell
dotnet run --project .\demo\airForge\air.cloud.airforge\air.cloud.airforge.csproj --urls http://127.0.0.1:5110
```

Standalone panel:

```text
http://127.0.0.1:5110/dynamicapp-panel
```

The standalone panel can be opened directly. When `DynamicApp.Manager.RequireTicket` is disabled, the page and Manager API can run without a ControlPlaneAuth session. When Manager API ticket validation is enabled, direct access shows the panel sign-in gate and protected API calls return unauthorized until the operator signs in through ControlPlaneAuth.

Control-plane sign-in and initialization:

```text
http://127.0.0.1:5110/_airforge/control-auth
```

Control-plane H5 shell:

```text
http://127.0.0.1:5110/_airforge/control-plane
```

AirForge loads capability configuration from the runtime `config` directory. The default ControlPlaneAuth file is:

```text
demo/airForge/air.cloud.airforge/config/control-plane-auth.json
```

The browser panel no longer asks operators to enter the DynamicApp Manager Ticket. Operators sign in through ControlPlaneAuth first; AirForge then validates the control-plane session and injects the internal Manager API Ticket inside the server pipeline before requests reach `/dynamicapp-manager/*`.

AirForge uses two ControlPlaneAuth pages:

- `ControlPlaneAuth:AuthPath` is the initialization and sign-in page.
- `ControlPlaneAuth:PlanePath` is the authenticated H5 console shell.
- `ControlPlaneAuth:Shell:Title` is the top-left system name in the H5 shell.
- `ControlPlaneAuth:Shell:DefaultMenuKey` controls which shell menu opens by default.
- `ControlPlaneAuth:Shell:Frames` controls iframe menu entries. `Title` is the left menu text and `Url` is the iframe target path.

`ControlPlaneAuth:DefaultReturnUrl` is set to `/_airforge/control-plane` for AirForge. After sign-in, operators enter the H5 shell. The shell provides the top bar, left menu, language switch, logout action, and built-in service-ticket management. The AirForge Manager page is loaded as a same-origin iframe through `ControlPlaneAuth:Shell:Frames`.

When the shell opens the AirForge Manager iframe, it appends `embedded=1` and the ControlPlaneAuth session query parameter configured by `ControlPlaneAuth:Session:QueryName`. The iframe converts that value to the `X-Air-ControlPlane-Ticket` request header for Manager API calls. The server validates that control-plane ticket and injects the internal DynamicApp Manager Ticket; the internal Manager Ticket is not exposed to the browser.

The deployment configuration registers AirForge to Consul with:

```json
{
  "DynamicApp": {
    "ServiceRegistry": {
      "Provider": "Consul",
      "Enabled": true,
      "EnableHealthCheck": true
    }
  },
  "ConsulServiceOptions": {
    "ConsulAddress": "http://fcj-consul:8500",
    "ServiceAddress": "http://air.forge:8080",
    "RegistryServiceName": "air.forge",
    "HealthCheckRoute": "/Health"
  }
}
```

## Build

```powershell
dotnet build .\demo\airForge\air.cloud.airforge\air.cloud.airforge.csproj -c Release
```
