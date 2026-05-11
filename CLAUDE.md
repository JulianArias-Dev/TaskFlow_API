# TaskFlow — Contexto del proyecto

Proyecto académico que demuestra **patrones de diseño GoF** en un sistema de gestión de tareas tipo Kanban. Mismo repo aloja el **backend .NET 9** (raíz) y el **frontend React 18 + Vite** ([TaskFlow_front/](TaskFlow_front/)). El frontend original vivía en otro repositorio con Firebase; lo que hay aquí es una **reimplementación contra la API REST propia**.

## Stack

**Backend** ([raíz](.))
- .NET 9, ASP.NET Core Web API, EF Core 9
- SQL Server 2022 (Express)
- JWT auth con blacklist (logout real revoca tokens)
- AutoMapper, BCrypt, MailKit
- Swagger en `/swagger` (sólo en `Development`)

**Frontend** ([TaskFlow_front/](TaskFlow_front/))
- React 18 + TypeScript + Vite 6
- Tailwind CSS 4
- react-router-dom v6, framer-motion, @hello-pangea/dnd, recharts, jspdf
- Servido por **Nginx** en producción ([nginx.conf](TaskFlow_front/nginx.conf) proxea `/api/` al backend)

## Estructura

```
.
├── Controllers/          # ASP.NET Controllers (api/Auth, api/Projects, api/Tasks, ...)
├── DTOs/                 # Request/Response DTOs + ResponseDto<T> wrapper estándar
├── Models/               # Entidades EF Core
├── Data/TaskFlowDbContext.cs  # DbContext + seeds de catálogos
├── Repositories/         # IRepository<T> + UnitOfWork
├── Services/             # Lógica de dominio
├── Mappings/             # Perfiles AutoMapper
├── Middleware/           # GlobalExceptionHandler, JwtBlacklist
├── Validations/          # Atributos custom
├── Patterns/             # Implementaciones de patrones (ver abajo)
├── Migrations/           # EF migrations — se aplican en startup vía MigrateAsync
└── TaskFlow_front/       # Frontend React (Dockerfile multi-stage + nginx)
```

## Patrones implementados (espejados en frontend)

| Patrón | Backend (`Patterns/`) | Frontend (`src/lib/designPatterns/`) |
| --- | --- | --- |
| Singleton | `Singleton/TaskFlowDbContextSingleton` | DbService (singleton via `getInstance`) |
| Factory Method | `Factory/TaskFactory` | `TaskFactory.ts` |
| Abstract Factory | `AbstractFactory/ThemeFactory` | `ThemeFactory.ts` |
| Builder | `Builder/TaskBuilder`, `BoardBuilder`, `ProjectBuilder` (mismo archivo) | `TaskBuilder.ts` |
| Prototype | `Prototype/PrototypeManager` | `Prototype.ts` |
| Adapter | `Adapter/{InApp,Smtp,SendGrid}NotificationAdapter` | `NotificationAdapter.ts` |
| Bridge | `Bridge/Report` + `{Pdf,Csv}ReportFormat` | `ReportBridge.ts` |
| Composite | `Composite/TaskComposite` + `TaskLeaf` | `TaskComposite.ts` |
| Decorator | `Decorator/TaskViewDecorator` | `TaskDecorator.ts` |
| Facade | `Facade/ProjectFacade` | `ProjectFacade.ts` |
| Flyweight | `Flyweight/TaskFlyweightFactory` | `Flyweight.ts` |
| Proxy | `Proxy/TaskServiceProxy` (registrado vía DI como `ITaskService`) | `TaskManagementProxy.ts` |

El **Proxy** en backend ([Program.cs](Program.cs)) es importante: `ITaskService` resuelve al proxy, no a `TaskService` directo. El proxy intercepta para añadir auditoría/permisos.

## Catálogos seedeados (IDs importantes)

Definidos en [Data/TaskFlowDbContext.cs](Data/TaskFlowDbContext.cs) → `ConfigureCatalogs`:

| Tabla | IDs |
| --- | --- |
| `AppRoles` | 1=Admin, **2=CommonUser** *(rol por defecto en registro)* |
| `ProjectRoles` | 1=Creator, 2=Project Manager, 3=Developer |
| `ProjectStatuses` | 1=Active, 2=Completed, 3=On Hold, 4=Cancelled |
| `TaskPriorities` | 1=LOW, 2=MEDIUM, 3=HIGH, 4=CRITICAL |
| `TaskStatuses` | 1=To Do, 2=In Progress, 3=Done, 4=Blocked |
| `TaskTypes` | 1=Feature, 2=Bug, 3=Improvement, 4=Research, 5=Task, 6=SubTask |

⚠️ **`AppRoles` sólo tiene 2 filas — no 3.** El error clásico: enviar `roleId: 3` en registro creyendo que es "Developer". Developer pertenece a `ProjectRoles`, no a `AppRoles`. El frontend usa `roleId: 2` y el default de `CreateUserDto.RoleId` también es `2`.

## Convenciones de la API

- **Wrapper estándar**: todos los endpoints devuelven `ResponseDto<T>` con `{ success, message, data, errors[], statusCode }`. Excepción: `UsersController` devuelve `User` plano (sin envolver) en varios métodos — el cliente HTTP del frontend ([lib/api.ts](TaskFlow_front/src/lib/api.ts)) detecta ambos formatos y desempaqueta.
- **Validación**: dos formatos de error coexisten porque `[ApiController]` intercepta el ModelState inválido antes de que llegue al controller:
  - `ResponseDto` con `errors: string[]`
  - `ProblemDetails` (auto de ASP.NET) con `errors: { Field: string[] }`
  
  El cliente HTTP los normaliza a una sola estructura.
- **Auth**: header `Authorization: Bearer <jwt>`. El frontend persiste el token en `localStorage` (`taskflow.jwt`) y escucha el evento custom `taskflow:auth-expired` cuando recibe 401.
- **CORS**: `AllowAll` (cualquier origen) — apropiado para demo; endurece en prod.

## Frontend ↔ Backend — convenciones clave

- **`Task.status` (frontend) = `Task.ColumnId` (backend GUID).** No es el `StatusId` del catálogo. Los Kanban moves cambian la columna.
- **Catálogos lazy-loaded**: [databaseService.ts](TaskFlow_front/src/services/databaseService.ts) tiene una clase `CatalogCache` que cachea los catálogos (`/api/BaseCatalog/*`) y resuelve `priorityId`, `typeId`, etc. desde nombres/códigos legibles.
- **Compat shim de Firebase**: [src/lib/firebase.ts](TaskFlow_front/src/lib/firebase.ts) ya **no** importa `firebase` — expone un objeto `auth` con la misma forma que `firebase/auth.User` (currentUser, subscribe, getIdToken) para no reescribir cada vista. Los stubs `db` y `storage` lanzan errores explícitos si alguien intenta usar Firestore.
- **Avatar uploads**: el backend acepta `PUT /api/Users/{id}/profile` con body JSON (`UpdateUserDto` con `AvatarUrl`). Sirve para data URLs largas. El `PUT /api/Users/{id}` clásico vía query params todavía existe pero no soporta strings grandes.

## Ejecución

```powershell
# Levantar todo
docker compose up -d --build

# Servicios y puertos (host)
# - Frontend  → http://localhost:8081    (host:8081 → contenedor:80)
# - API       → http://localhost:8080    (Swagger en /swagger)
# - SQL       → localhost:1433           (sa / Unicesar+2026)

# Personalizar puerto del frontend si 8081 está ocupado:
$env:FRONTEND_PORT=5173; docker compose up -d

# Reconstruir un servicio puntual
docker compose up -d --build taskflow-api
docker compose up -d --build taskflow-frontend

# Frontend en modo dev (sin Docker)
cd TaskFlow_front
npm install
npm run dev      # vite en :3000 con proxy /api → :8080
npm run build    # tsc --noEmit + vite build
```

El frontend en dev hace proxy de `/api` a `VITE_BACKEND_PROXY_URL` (default `http://localhost:8080`). En prod, **nginx dentro del contenedor del frontend** proxea `/api/` a `http://taskflow-api:8080/api/` (resolución DNS por la red de compose).

## Manejo de excepciones del backend

[GlobalExceptionHandlerMiddleware](Middleware/GlobalExceptionHandlerMiddleware.cs) hace dos cosas críticas:

1. Devuelve `ResponseDto` estándar para `KeyNotFoundException`, `InvalidOperationException`, `ArgumentException`, `UnauthorizedAccessException`, `DbUpdateException`.
2. **Recorre la cadena de `InnerException`** y devuelve el mensaje más profundo. Esto es lo que hace que los errores de EF Core (FKs, NOT NULL) revelen la causa real en lugar del genérico "An error occurred while saving the entity changes".

   En `Production` se oculta el detalle (mensaje genérico al cliente, log al servidor).

## Pendientes conocidos

- **Recuperación de contraseña** — no hay endpoint. La página [ForgotPasswordPage.tsx](TaskFlow_front/src/features/auth/pages/ForgotPasswordPage.tsx) es informativa.
- **Subida real de avatares/adjuntos** — el frontend genera `data:` URLs y las persiste como `AvatarUrl`. El endpoint `POST /api/Attachments` está esbozado pero no se ha conectado.
- **Configuración global** — la pantalla admin sólo gestiona usuarios. No hay endpoint `Settings`.
- **Notificaciones en tiempo real** — actualmente polling cada 20 s sobre `/api/Users/my-notifications` (antes era `onSnapshot` de Firestore).

## Gotchas / tribal knowledge

- **Encoding de archivos `.cs`**: en algún momento se guardaron strings con `�` literales (replacement char) en lugar de tildes/`ñ`. Los DTOs ya están limpios; si encuentras `�` en otro archivo (`Services/`, `Middleware/`, `Controllers/`), arréglalo con búsqueda/reemplazo en lote como hicimos en [DTOs/UserDto.cs](DTOs/UserDto.cs).
- **`ProjectBuilder` y `StatusId`**: el builder ahora inicializa `StatusId = 1` por defecto y expone `WithStatusId(int)`. Antes lo dejaba en 0 y reventaba FK en `SaveChangesAsync`. Si añades nuevos builders, replica el patrón para todos los FKs obligatorios.
- **Migrations en startup**: [Program.cs](Program.cs) llama `await db.Database.MigrateAsync()` antes de aceptar tráfico. Los seeds van en `HasData()` y se aplican vía la migración inicial `20260511150045_Initial-Catalog`.
- **JWT blacklist**: `ITokenBlacklistService` es **Singleton** (cache en memoria compartido globalmente). Al arrancar, hidrata el cache desde la tabla `RevokedTokens` (ver final de [Program.cs](Program.cs)).
- **Frontend no tiene tests**. Para verificar cambios: `npx tsc --noEmit` y luego `npm run build`. El backend tampoco tiene proyecto de tests.
- **`package-lock.json` del frontend** se regeneró al migrar de Firebase → REST. Si añades dependencias, ejecuta `npm install` para refrescarlo antes de commitear.

## Archivos clave

| Archivo | Por qué importa |
| --- | --- |
| [Program.cs](Program.cs) | Composition root — DI, JWT config, CORS, migrations, pipeline. |
| [Data/TaskFlowDbContext.cs](Data/TaskFlowDbContext.cs) | Esquema EF + seeds de catálogos. Cualquier cambio de modelo empieza aquí. |
| [Patterns/Builder/TaskBuilder.cs](Patterns/Builder/TaskBuilder.cs) | Contiene tanto `TaskBuilder` como `ProjectBuilder`. |
| [TaskFlow_front/src/lib/api.ts](TaskFlow_front/src/lib/api.ts) | Cliente HTTP — único lugar donde se manipula el JWT. |
| [TaskFlow_front/src/services/databaseService.ts](TaskFlow_front/src/services/databaseService.ts) | Mapper backend↔frontend. Si cambia un DTO o el modelo, este es el ajuste. |
| [TaskFlow_front/src/lib/firebase.ts](TaskFlow_front/src/lib/firebase.ts) | Shim — NO usa Firebase. Mantiene la API que las vistas legacy esperaban. |
| [docker-compose.yml](docker-compose.yml) | Orquesta SQL + API + Frontend en la red `taskflow-network`. |
| [TaskFlow_front/nginx.conf](TaskFlow_front/nginx.conf) | SPA fallback + proxy `/api/` → `taskflow-api:8080`. |
