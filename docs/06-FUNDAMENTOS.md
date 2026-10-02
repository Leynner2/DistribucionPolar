# Fundamentos teóricos aplicados

Relación entre los conceptos del material de las Fases 1, 2 y 3 y su aplicación concreta en el proyecto.
Cuando un concepto todavía no aplica, se explica por qué.

## Fase 1: Fundamentos arquitectónicos, .NET 10 y resiliencia REST

### Evolución de .NET y lenguaje

| Concepto | Aplicación en el proyecto |
| --- | --- |
| **.NET Framework → .NET Core → .NET 5+ → .NET 10**: de un runtime ligado a Windows a una plataforma multiplataforma, de código abierto y orientada a contenedores | La API corre en Linux dentro de Docker sobre `mcr.microsoft.com/dotnet/aspnet:10.0`, con imagen multietapa (`Dockerfile`) |
| **Tipado estático y nullable reference types** | `<Nullable>enable</Nullable>` en todos los proyectos; la solución compila con 0 advertencias |
| **Características modernas de C#** | *Pattern matching* con patrones relacionales y de tupla (`ProductHealthEvaluator`, `BoxQuantity.ToString`, `BusinessMappings.MoneyStatus`) · rangos `[..3]` (`CorporateSkuGenerator`) · `record` para DTOs y value objects · constructores primarios en servicios y repositorios · expresiones de colección `[]` · *raw string literals* (SQL de `DocumentNumberGenerator`) · `[GeneratedRegex]` (regex generada en compilación) · `System.Threading.Lock` (`ExchangeRateCache`) |
| **Ecosistema unificado**: logging, configuración, contenedor IoC y seguridad incluidos | Se usan los componentes nativos: `ILogger`, `IConfiguration`/`IOptions`, DI, autenticación JWT, rate limiting, CORS, health checks y OpenAPI |

### Arquitectura

| Concepto | Aplicación |
| --- | --- |
| **Problema del monolito acoplado**: la lógica mezclada con la base de datos y la interfaz dificulta mantener y probar | La lógica está aislada en `Core.Domain`. Las 75 pruebas de dominio corren sin base de datos ni servidor |
| **Origen de Onion**: DDD (Evans, 2003) → Arquitectura Hexagonal / *Ports and Adapters* (Cockburn, 2005) → Onion (Palermo, 2008) → Clean Architecture (Martin) | Puertos = interfaces en `Core.Application/Abstractions`; adaptadores = implementaciones en `Infrastructure` (EF Core, HttpClient, QuestPDF) y en `Presentation.API` (HTTP) |
| **Regla de dependencia**: las capas externas dependen de las internas, nunca al revés | La impone el compilador: `Core.Domain` no tiene referencias ([Arquitectura §1](02-ARQUITECTURA.md#1-visión-general)) |
| **DDD**: entidades con identidad y comportamiento, value objects, agregados, invariantes | Entidades ricas (`Product`, `Client`, `Invoice`), value objects (`BoxQuantity`, `ProductLine`) y agregados que controlan sus colecciones (`Invoice` → líneas y vacíos; `Truck` → inventario; `ConsignmentEvent` → productos) |

### Inyección de dependencias e IoC

| Concepto | Aplicación |
| --- | --- |
| **Inversión de control**: el contenedor crea los objetos; el código no usa `new` sobre dependencias | Todos los servicios, repositorios y adaptadores se reciben por constructor y se registran en `DependencyInjection.cs` |
| **Transient / Scoped / Singleton** | Tabla y justificación en [Arquitectura §11](02-ARQUITECTURA.md#11-inyección-de-dependencias) |
| **Dependencia cautiva**: un Singleton que retiene un Scoped (por ejemplo, un `DbContext`) y lo reutiliza entre peticiones | No existe ninguna. La auditoría, que debía aislarse, usa una **fábrica** de `DbContext` en lugar de retener uno |

### Manejo de errores y resiliencia

| Concepto | Aplicación |
| --- | --- |
| **Middleware global**: un componente del pipeline que intercepta todas las peticiones | `ExceptionMiddleware`, primero en el pipeline |
| **RFC 7807 (Problem Details)**: errores con `type`, `title`, `status`, `detail` e `instance` | Todas las respuestas de error (400, 401, 403, 404, 409, 429, 500, 503) usan `application/problem+json` y nunca exponen el stack trace |
| **Reintentos (Retry)**: repetir una operación que falló por una causa transitoria | Npgsql `EnableRetryOnFailure` (base de datos) y Polly (proveedor de tasas), con espera exponencial |
| **Circuit Breaker**: tras muchas fallas, deja de llamar al servicio caído durante un tiempo y falla de inmediato (estados cerrado → abierto → semiabierto) | `AddStandardResilienceHandler` en el `HttpClient` de tasas de cambio ([ADR 35](04-DECISIONES.md#35-resiliencia-con-polly-y-circuit-breaker-en-el-servicio-externo)). Se aplica a la única dependencia HTTP saliente; la base de datos se protege con reintentos y transacciones |
| **Timeout y fallback** | Timeout de 3 s por intento; si el proveedor falla, se usa el último valor conocido |

### Glosario del material

| Término | En este proyecto |
| --- | --- |
| **API REST** | Recursos con verbos HTTP: `GET` consulta, `POST` crea o ejecuta un proceso (`/load`, `/cancel`), `PUT` edita, `DELETE` elimina (lógicamente). JSON y sin estado de sesión |
| **ORM** | EF Core 10 traduce LINQ a SQL de PostgreSQL |
| **Middleware** | `ExceptionMiddleware`, CORS, autenticación, rate limiting, autorización |
| **Inyección de dependencias** | Ver arriba |
| **JWT** | Token firmado con los claims del usuario ([Fase 3](#fase-3-seguridad-stateless-jwt-rbac-y-validación)) |
| **CORS** | Política `frontend`: solo los orígenes configurados por entorno pueden llamar a la API desde un navegador |
| **API Gateway** | No aplica: hay una sola API. Sería útil si el sistema se dividiera en microservicios (enrutamiento, autenticación centralizada) |
| **React SPA / State Management** | Corresponde a la fase del frontend. La API ya está preparada: JSON, CORS para `localhost:5173` (Vite) y contrato OpenAPI para generar el cliente |

## Fase 2: Persistencia con EF Core 10, Fluent API y siembra

| Concepto | Aplicación |
| --- | --- |
| **Code-First vs Database-First**: el esquema nace del código y se versiona con migraciones, o se genera el código a partir de una base existente | **Code-First**. Migraciones `InitialCatalog` y `BusinessOperations` versionadas en Git; script en `docs/fase2/schema.sql` |
| **Fluent API vs Data Annotations**: con atributos, el dominio queda acoplado a EF | Fluent API en `Infrastructure/Persistence/Configurations`, una clase por entidad; el dominio no tiene atributos |
| **`ApplyConfigurationsFromAssembly`** | `ApplicationDbContext.OnModelCreating` |
| **Claves UUID**: no son enumerables y se generan en la aplicación | Todas las entidades de negocio. Excepción justificada: `AuditLogs` usa BIGINT identity porque es de solo inserción y crece rápido |
| **Claves deterministas en `HasData`**: con `Guid.NewGuid()`, cada migración detectaría cambios falsos | UUID fijos en `CatalogSeedData` y `OperationsSeedData` |
| **`HasData` vs `DbInitializer`** | Catálogo y datos de ejemplo con `HasData`; usuarios con `DbInitializer`, porque necesitan un hash con sal aleatoria |
| **Idempotencia de la siembra dinámica** | `DbInitializer` solo crea usuarios si la tabla está vacía; reiniciar el contenedor no duplica nada |
| **Precisión `numeric(18,2)`**: los flotantes binarios redondean mal el dinero | Todos los montos; además, los validadores rechazan más de 2 decimales |
| **Índices únicos (B-Tree)**: unicidad garantizada por el motor y búsquedas puntuales con muy pocas lecturas de página (complejidad logarítmica, en la práctica casi constante) | SKU, número de factura, número de compra, código y RIF del cliente, placa, username y email |
| **Integridad referencial y `DeleteBehavior.Restrict`** | Las referencias entre entidades del negocio (categoría → producto, producto, cliente, empleado o camión referenciados por documentos) usan `Restrict`. Solo las partes internas de un agregado (líneas de documento, inventario del camión, saldos de vacíos por grupo) se borran en cascada con su dueño |
| **`.AsNoTracking()`**: sin snapshots del ChangeTracker en las lecturas | Todas las consultas de lectura de los repositorios |
| **`.AsNoTrackingWithIdentityResolution()`**: sin seguimiento, pero sin duplicar objetos repetidos | Camiones con su inventario (los productos se repiten entre camiones) y facturas con sus líneas |
| **Proyección a DTO con `Select`**: el SQL trae solo las columnas necesarias | Listado paginado de facturas, reportes diarios, inventario por categoría y camión |
| **NoTracking global** (`UseQueryTrackingBehavior`) | No se usa a propósito: cada repositorio declara explícitamente sus lecturas sin seguimiento y sus `GetForUpdateAsync` con seguimiento, para que la intención quede visible en cada consulta |
| **3FN y anomalías** (inserción, actualización, borrado) | Cada dato vive en una sola tabla (el depósito en `Categories`, los saldos en `Clients`). Excepción deliberada: las líneas de documento copian precios porque son históricas ([ADR 28](04-DECISIONES.md#28-snapshot-del-producto-en-las-líneas-de-documento)) |
| **ACID y transacciones explícitas** (`BeginTransactionAsync`, commit y rollback) | `ExecuteInTransactionAsync` en cada proceso de negocio ([Arquitectura §4](02-ARQUITECTURA.md#4-transacciones-concurrencia-y-numeración)) |
| **Aislamiento y concurrencia** | Nivel *Read Committed* de PostgreSQL + concurrencia optimista con `xmin` (409 ante escrituras simultáneas) |
| **Auditoría con JSONB e inet** | Tabla `AuditLogs`: BIGINT identity, `jsonb` para antes, después y metadatos, `inet` para la IP y `NOW()` por defecto |

## Fase 3: Seguridad stateless (JWT), RBAC y validación

| Concepto | Aplicación |
| --- | --- |
| **Autenticación stateless**: el servidor no guarda sesiones; cada petición trae su token | JWT Bearer. Permite escalar horizontalmente sin sesiones compartidas |
| **Estructura del JWT**: encabezado, carga (claims) y firma | Claims `sub`, `unique_name`, `email`, `name`, `role`, `jti`, `iss`, `aud`, `iat`, `nbf`, `exp`; firma HMAC-SHA256 |
| **Validación del token** | Emisor, audiencia, firma, expiración, algoritmo fijo `HS256` (evita la sustitución por `alg: none`) y tolerancia de reloj de 30 s |
| **RBAC** | `[Authorize(Roles = ...)]` en los controladores, con matriz Admin/Employee ([Arquitectura §7](02-ARQUITECTURA.md#7-seguridad)) |
| **401 vs 403**: 401 = no se sabe quién eres (sin token, o token inválido o expirado); 403 = se sabe quién eres, pero tu rol no puede | Ambos en RFC 7807 desde los eventos de JwtBearer |
| **Seguro por defecto** | Política de respaldo: toda ruta exige autenticación salvo las marcadas `[AllowAnonymous]` |
| **Validación defensiva (FluentValidation)** | Validadores en `Core.Application/Validators` para todos los DTOs de creación y edición; 400 con la lista de errores por campo, en español |
| **Hash de claves (SHA-256 o superior)** | PBKDF2 + HMAC-SHA256, 100.000 iteraciones y sal por usuario. Un SHA-256 simple se calcula a miles de millones por segundo; PBKDF2 lo hace costoso a propósito. bcrypt sería equivalente, pero requiere un paquete externo ([ADR 18](04-DECISIONES.md#18-claves-con-pbkdf2-hmac-sha256)) |
| **Protección de secretos** | Clave JWT y claves de base de datos por variable de entorno en Staging y Production; los `.env` no se versionan |
| **Fuerza bruta** | Rate limiting en el login, por IP (429 con `Retry-After`) |
| **Mensajes que no filtran información** | Usuario o clave incorrectos dan el mismo mensaje; el usuario inactivo solo se informa después de verificar la clave |
