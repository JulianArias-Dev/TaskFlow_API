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
| `AppRoles` | 1=Admin, **2=CommonUser** *(rol por defecto en registro)*. La app además siembra al arrancar el usuario `superadmin@taskflow.com` / `Admin123*` con AppRoleId=1. |
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
docker compose up -d --build api
docker compose up -d --build web

# Frontend en modo dev (sin Docker)
cd TaskFlow_front
npm install
npm run dev      # vite en :3000 con proxy /api → :8080
npm run build    # tsc --noEmit + vite build
```

El frontend en dev hace proxy de `/api` a `VITE_BACKEND_PROXY_URL` (default `http://localhost:8080`). En prod, **nginx dentro del contenedor del frontend** proxea `/api/` a `http://api:8080/api/` (resolución DNS por la red de compose).

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

### RF-01.3 + RF-09.1 — Roles y administración de usuarios

- **SuperAdmin seed (vía migración)**: declarado con `HasData` en [TaskFlowDbContext.ConfigureCatalogs](Data/TaskFlowDbContext.cs). Email `superadmin@taskflow.com` / password `Admin123*`, AppRoleId=1, Id fijo `00000000-…-001`. El hash BCrypt está pre-computado en el DbContext (no en runtime). Migración asociada: `SeedSuperAdmin`. Si cambias la contraseña, regenera el hash con `BCrypt.Net.BCrypt.HashPassword("…")` y crea una nueva migración.
- **Auto-promote**: si la tabla `Users` está vacía cuando alguien se registra, se le promueve a Admin automáticamente. Sirve como red de seguridad si el seed no se ejecuta.
- **Cambio de rol**: `PUT /api/Users/{id}/role` (Body: `{ appRoleId, isActive? }`) — sólo accesible para usuarios con claim `Role=Admin`. Valida que el AppRoleId existe en el catálogo antes de hacer UPDATE.
- **UI**: pestaña *Administración* visible cuando `profile.role?.toUpperCase() === 'ADMIN'` (case-insensitive, antes fallaba porque el backend envía "Admin" y la comparación esperaba "ADMIN"). Dropdown de rol se carga vía `useCatalog('app-roles')`.

### RF-05.3 — Preferencias de notificación

Implementado. El usuario configura por evento (`ASSIGNED`, `DUE_OVERDUE`, `COMMENT`, `STATUS_CHANGE`) qué canales reciben (`inApp`, `email`):
- **BD**: columna `Users.NotificationPreferences` (nvarchar(max), JSON) añadida vía migración `20260511202302_AddNotificationPreferences`.
- **API**: `GET/PUT /api/Users/me/notification-preferences` con DTO `NotificationPreferencesDto` (mapa código→canales).
- **Aplicación**: `NotificationService.NotifyAsync(userId, eventCode, …)` consulta las preferencias antes de iterar los Adapters; si el canal está silenciado se omite. Los callers (TaskService, ProjectService) pasan `"ASSIGNED"`, `"STATUS_CHANGE"`, etc.
- **UI**: pestaña "Notificaciones" del perfil ([ProfileDashboard.tsx](TaskFlow_front/src/features/profile/ProfileDashboard.tsx)). Los toggles se persisten **al instante** vía `persistNotificationPrefs` (no requieren entrar en modo edición).

## Gotchas / tribal knowledge

- **Encoding de archivos `.cs`**: en algún momento se guardaron strings con `�` literales (replacement char) en lugar de tildes/`ñ`. Los DTOs ya están limpios; si encuentras `�` en otro archivo (`Services/`, `Middleware/`, `Controllers/`), arréglalo con búsqueda/reemplazo en lote como hicimos en [DTOs/UserDto.cs](DTOs/UserDto.cs).
- **FKs obligatorios en entidades nuevas**: los modelos `Project`, `Task`, etc. tienen FKs `int` no nullable (`StatusId`, `PriorityId`, `TypeId`, `AppRoleId`, …). El default `0` **no existe en los catálogos** y revienta `SaveChangesAsync` con `FK_…_constraint`. Si creas una entidad con un FK que el cliente no envía, asígnale un default razonable en el service/builder. Casos hechos: `ProjectBuilder.StatusId = 1`, `TaskService.CreateTaskAsync.StatusId = 1`. Replica este patrón al añadir nuevas entidades.
- **Navegaciones declaradas `null!` (Status, Owner, Project, Type, Priority, …)**: son `null` cuando la entidad se acaba de crear o cuando la query no las incluye. Cualquier `MapToDto` debe usar `?.Name ?? ""` y nunca `.ToString()` sobre la navegación (devolvía el nombre del tipo C#, no el valor del catálogo). Tras `SaveChangesAsync` en un *create*, recarga con `GetXxxWithDetailsAsync` antes de mapear, o tolera nulls en el mapper. Casos de referencia: [ProjectService.CreateProjectAsync](Services/ProjectService.cs), [TaskService.MapToDto](Services/TaskService.cs), [MappingProfile](Mappings/MappingProfile.cs).
- **Factory Method del PDF (RF/2.3) está en `POST /api/Tasks/by-type`**. La interfaz `ITaskFactory.CreateTask(title, description, columnId)` exige `columnId` porque es FK obligatoria. Cada factory concreta (`BugTaskFactory`, `FeatureTaskFactory`, etc.) setea sus propios defaults (TypeId/PriorityId). El frontend usa este endpoint en `dbService.createTask` y luego hace un PUT con los campos extra (fecha, asignados) — así demuestra el patrón sin perder la flexibilidad del formulario.
- **Frontend usa los catálogos del backend**: hook [useCatalog](TaskFlow_front/src/hooks/useCatalog.ts) carga desde `/api/BaseCatalog/*` y se usa en TaskModal (types/priorities) y ProjectSettingsModal (project-status/roles). Los `<option>` ya no están hardcoded — se sincronizan automáticamente con los seeds. Los nombres que vienen del backend ("LOW", "Active", ...) se muestran tal cual; los comparadores en UI son case-insensitive y aceptan tanto los nombres del backend como los nombres legacy en español.
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
