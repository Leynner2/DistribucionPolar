# Distribucion Polar

API de distribucion construida con ASP.NET Core 10 y una arquitectura multicapa desacoplada.

## Requisitos

- .NET SDK 10.0 o superior.
- Git.

Verificar la instalacion:

```powershell
dotnet --version
git --version
```

## Estructura de la solucion

```text
DistribucionPolar.slnx
├── Core.Domain/
│   └── Entidades y reglas del negocio, sin dependencias externas
├── Core.Application/
│   └── Contratos y servicios de casos de uso
├── Infrastructure/
│   └── Implementaciones de persistencia y servicios externos
└── Presentation.API/
    └── API HTTP, configuracion de DI y middleware
```

Dependencias permitidas:

```text
Presentation.API -> Core.Application
Presentation.API -> Infrastructure
Infrastructure -> Core.Application -> Core.Domain
```

`Core.Domain` permanece independiente de las demas capas y de paquetes externos.

## Ejecucion

Desde la raiz del repositorio:

```powershell
dotnet restore .\DistribucionPolar.slnx
dotnet build .\DistribucionPolar.slnx
dotnet run --project .\Presentation.API\Presentation.API.csproj
```

Con el perfil de desarrollo, la API queda disponible en:

```text
http://localhost:5249
```

Tambien puede especificarse otra URL:

```powershell
dotnet run --project .\Presentation.API\Presentation.API.csproj --urls http://localhost:5099
```

Para detener la aplicacion, presionar `Ctrl+C` en la terminal.

## Endpoints

### Estado de la API

```http
GET /
```

Devuelve la pagina inicial de Distribucion Polar.

### Productos

```http
GET /products
```

Devuelve los productos disponibles en el repositorio en memoria.

Ejemplo con PowerShell:

```powershell
Invoke-RestMethod http://localhost:5249/products
```

En desarrollo, el documento OpenAPI se expone en:

```text
http://localhost:5249/openapi/v1.json
```

## Inyeccion de dependencias

Los servicios se registran en `Presentation.API/Program.cs` con estos ciclos de vida:

- `Singleton`: `IProductRepository` e `InMemoryProductRepository`.
- `Scoped`: `IDistributionService` y `DistributionService`.
- `Transient`: `IRequestIdGenerator` y `RequestIdGenerator`.

## Manejo de excepciones

`ExceptionMiddleware` se registra al inicio del pipeline HTTP y devuelve respuestas `application/problem+json`:

| Excepcion | Estado HTTP |
| --- | ---: |
| `KeyNotFoundException` | 404 |
| `InvalidOperationException` | 400 |
| Cualquier otra `Exception` | 500 |

Las respuestas 500 no exponen el stack trace al cliente. El detalle completo queda registrado en los logs del servidor.

## Validacion

Compilar toda la solucion:

```powershell
dotnet build .\DistribucionPolar.slnx
```

No hay un proyecto de pruebas automatizadas configurado actualmente.

## Estructura de commits

El repositorio utiliza commits pequenos y enfocados siguiendo [Conventional Commits](https://www.conventionalcommits.org/):

```text
<tipo>(<alcance>): <descripcion breve>
```

Tipos recomendados:

- `feat`: nueva funcionalidad.
- `fix`: correccion de comportamiento.
- `refactor`: cambio interno sin modificar el comportamiento.
- `docs`: documentacion.
- `test`: pruebas.
- `build`: cambios de compilacion o dependencias.
- `chore`: mantenimiento.

Ejemplo de secuencia para este proyecto:

```text
feat(domain): agregar entidades base y de distribucion
feat(application): definir contratos y servicio de distribucion
feat(infrastructure): agregar repositorio de productos en memoria
feat(api): configurar inyeccion de dependencias y middleware de excepciones
docs(readme): documentar ejecucion y convencion de commits
```

Buenas practicas:

- Un commit debe representar un cambio coherente y revisable.
- Usar el imperativo en la descripcion: `agregar`, `configurar`, `corregir`.
- Mantener la primera linea breve, idealmente menor a 72 caracteres.
- Separar cambios de documentacion, pruebas y codigo cuando sea posible.
- No incluir secretos, archivos generados de `bin/` u `obj/`, ni configuraciones locales.
