# DePaso

Aplicación web de viajes compartidos y envíos. La solución tiene cinco proyectos: Web, Api, Logica, Entidades y Tests.

## Requisitos

- .NET 10 SDK
- SQL Server. En esta máquina se usó la instancia `localhost\SQLEXPRESS` y la base `DePasoApp`.

## Base de datos

La cadena de conexión no va en el repositorio. Se guarda en los secretos de usuario de cada proyecto que abre la base, con la clave `ConnectionStrings:Conexion`.

En la API:

```
dotnet user-secrets set "ConnectionStrings:Conexion" "Server=localhost\SQLEXPRESS;Database=DePasoApp;Trusted_Connection=True;TrustServerCertificate=True" --project DePaso.Api
```

La web también abre la base para el inicio y el potencial mensual. La misma clave va en `DePaso.Web`.

Para crear las tablas:

```
dotnet tool install --global dotnet-ef
dotnet ef database update --project DePaso.Entidades --startup-project DePaso.Web
```

## Datos iniciales

Si la base no tiene usuarios, la API carga dos cuentas, un viaje de lunes y viernes, una parada, un envío entregado y uno que busca conductor.

Las dos usan la misma contraseña, `Demo1234`.

- `demo.conductor@depaso.test`
- `demo.cliente@depaso.test`

Si la base ya tiene usuarios, no se agrega nada.

## Cómo ejecutar

Hacen falta los dos procesos. En Visual Studio, propiedades de la solución, varios proyectos de inicio: Iniciar en `DePaso.Web` y en `DePaso.Api`.

- Web: https://localhost:7201
- API: http://localhost:5234 y https://localhost:7224

La web llama a la API con `ApiUrl` en `DePaso.Web/appsettings.json`, que apunta a `http://localhost:5234/`.

## Swagger

Con la API en Development, la página está en http://localhost:5234/swagger y en https://localhost:7224/swagger. El documento OpenAPI sigue en `/openapi/v1.json`.

## Geoapify

Sin la clave, el cálculo de rutas usa el servicio simulado. La clave se configura en los secretos de la API, con la clave `Geoapify:ApiKey`. No va en el repositorio.

## Tests

```
dotnet test DePaso.Tests/DePaso.Tests.csproj
```
