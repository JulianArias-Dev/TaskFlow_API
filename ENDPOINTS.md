# TaskFlow API — Documentación de Endpoints

API REST para gestión de tareas Kanban construida con **.NET 9**, Entity Framework Core y SQL Server. Implementa autenticación JWT (con **logout real** vía blacklist persistida) y aplica los siguientes patrones de diseño:

- **Creacionales (1ª entrega):** Singleton, Factory Method, Abstract Factory, Builder, Prototype.
- **Estructurales (2ª entrega):** Adapter, Bridge, Composite, Decorator, Facade, Proxy, Flyweight.

---

## Información general

| Item | Valor |
|------|-------|
| Servicio | TaskFlow API |
| Versión | 1.1.0 |
| Framework | .NET 9.0 |
| Autenticación | JWT Bearer + JWT Blacklist (logout real) |
| Formato | `application/json` |
| Base de datos | SQL Server |
| Documentación interactiva | `/swagger` (sólo en Development) |

### Convenciones

- **Base URL:** `https://{host}/api`
- **Autenticación:** `Authorization: Bearer {token}` (excepto endpoints marcados como `Público`).
- **Roles soportados:** `Admin`, `CommonUser`, `Manager`, `Developer`.
- **Políticas de autorización:** `AdminOnly`, `ManagerOrAdmin`, `DeveloperOrHigher`.
- **Token expirado:** la respuesta incluye la cabecera `X-Token-Expired: true`.
- **Token revocado (logout):** devuelve `401 Unauthorized` con mensaje `"Token revoked. Please login again."` — el middleware `JwtBlacklistMiddleware` lo intercepta antes de llegar al controller.
- **JTI claim:** cada JWT lleva un `jti` único para soportar revocación granular.

### Estructura de respuesta estándar

`ResponseDto<T>`:
```json
{
  "success": true,
  "message": "Operación exitosa",
  "data": { },
  "errors": [],
  "statusCode": 200
}
```

`PagedResponseDto<T>`:
```json
{
  "success": true,
  "message": "Datos recuperados exitosamente",
  "items": [],
  "pageNumber": 1,
  "pageSize": 10,
  "totalCount": 0,
  "totalPages": 0,
  "hasPreviousPage": false,
  "hasNextPage": false
}
```

---

## Tabla de contenidos

1. [Endpoints raíz / Health](#1-endpoints-raíz--health)
2. [Auth — `/api/auth`](#2-auth--apiauth)
3. [Users — `/api/users`](#3-users--apiusers)
4. [Projects — `/api/projects`](#4-projects--apiprojects)
5. [Boards — `/api/boards`](#5-boards--apiboards)
6. [Columns — `/api/columns`](#6-columns--apicolumns)
7. [Tasks — `/api/tasks`](#7-tasks--apitasks)
8. [Comments — `/api/comments`](#8-comments--apicomments)
9. [Tags — `/api/tags`](#9-tags--apitags)
10. [Attachments — `/api/attachments`](#10-attachments--apiattachments) **(NUEVO)**
11. [SavedFilters — `/api/savedfilters`](#11-savedfilters--apisavedfilters) **(NUEVO)**
12. [Dashboard — `/api/dashboard`](#12-dashboard--apidashboard) **(NUEVO — Facade)**
13. [Reports — `/api/reports`](#13-reports--apireports) **(NUEVO — Bridge)**
14. [Themes — `/api/themes`](#14-themes--apithemes)
15. [BaseCatalog — `/api/basecatalog`](#15-basecatalog--apibasecatalog)

---

## Cambios de esta versión (v1.1.0)

| Cambio | Detalle |
|---|---|
| **Logout real** | Nuevo `POST /api/auth/logout` revoca el JWT (blacklist persistida + caché en memoria). |
| **Dashboard centralizado** | Nuevo `GET /api/dashboard/project/{id}` — orquestado por `ProjectFacade`. |
| **Reportes multi-formato** | Nuevo `GET /api/reports/project/{id}?format=pdf\|csv` — patrón Bridge. |
| **Adjuntos** | Nuevo `AttachmentsController` (upload/download/list). |
| **Filtros guardados** | Nuevo `SavedFiltersController` (RF-07.3). |
| **Auth en Users / Themes / BaseCatalog** | Ahora requieren JWT (antes eran públicos). |
| **`LoginResponseDto`** | Reemplaza `lightTheme: bool` por `themePreference: "Light"\|"Dark"\|"System"` + `notifyByEmail: bool`. |
| **Tasks** | El `TaskService` se sirve a través de `TaskServiceProxy` (validaciones previas: proyecto activo + membresía). El contrato del controller no cambia. |
| **Tablas nuevas** | `RevokedTokens`, `SavedFilters`, `TaskLabels` (reemplaza `TaskTags`), `PasswordPolicies`, `AttachmentPolicies`. |

---

## 1. Endpoints raíz / Health

### `GET /`
- **Descripción:** Endpoint informativo del servicio.
- **Auth:** Público
- **Respuesta 200:**
```json
{
  "service": "TaskFlow API",
  "version": "1.1.0",
  "status": "running",
  "timestamp": "2026-05-08T00:00:00Z"
}
```

### `GET /health`
- **Descripción:** Health check oficial (incluye `DbContextCheck`).
- **Auth:** Público
- **Respuesta:** `200` (Healthy) / `503` (Unhealthy)

### `GET /api/health`
- **Descripción:** Estado general de la API.
- **Auth:** Público

### `GET /api/health/info`
- **Descripción:** Información descriptiva de la API.
- **Auth:** Público

### `GET /api/health/database`
- **Descripción:** Verifica conectividad con la base de datos.
- **Auth:** Público
- **Respuestas:** `200` conectada · `503` no disponible

---

## 2. Auth — `/api/auth`

Autenticación con JWT, registro de usuarios y **logout real con blacklist** de tokens.

### `POST /api/auth/login`
- **Descripción:** Inicia sesión y devuelve un JWT con `jti` único para soportar revocación.
- **Auth:** Público
- **Body (`LoginDto`):**
```json
{
  "email": "user@example.com",
  "password": "Password123!"
}
```
- **Respuesta 200 (`ResponseDto<LoginResponseDto>`):**
```json
{
  "success": true,
  "message": "Login successful",
  "data": {
    "userId": "guid",
    "email": "user@example.com",
    "name": "Nombre",
    "role": "Developer",
    "token": "eyJ...",
    "themePreference": "Light",
    "notifyByEmail": true,
    "expiresAt": "2026-05-08T01:00:00Z",
    "lastConnection": "2026-05-08T00:00:00Z"
  }
}
```
- **Errores:** `400` Validación · `401` Credenciales inválidas

### `POST /api/auth/register`
- **Descripción:** Registra un nuevo usuario y devuelve un JWT.
- **Auth:** Público
- **Body (`CreateUserDto`):**
```json
{
  "email": "user@example.com",
  "name": "Nombre Apellido",
  "password": "Password123!",
  "avatarUrl": "https://...",
  "roleId": 2
}
```
- **Validación de password:** 8+ caracteres, mayúscula, minúscula, dígito y carácter especial.
- **Respuestas:** `201` Created · `400` Validación · `409` Email ya existe

### `GET /api/auth/validate`
- **Descripción:** Valida el token JWT actual y devuelve los claims.
- **Auth:** **Requerido** (Bearer JWT)
- **Respuestas:** `200` Token válido · `401` Token inválido / expirado / revocado

### `POST /api/auth/logout` *(NUEVO)*
- **Descripción:** Cierra sesión revocando el token JWT actual. El `jti` se añade a:
  1. Caché en memoria (`ConcurrentDictionary`, lookup O(1) por request).
  2. Tabla `RevokedTokens` (sobrevive reinicios; se carga al arranque).
- **Auth:** **Requerido**
- **Body:** Ninguno (el token se lee del header `Authorization`).
- **Respuesta 200:**
```json
{
  "success": true,
  "message": "Logout successful — token revoked",
  "statusCode": 200
}
```
- **Errores:** `401` Sin bearer token o token inválido
- **Comportamiento posterior:** cualquier request futuro con ese token devuelve `401` con mensaje `"Token revoked. Please login again."`.

---

## 3. Users — `/api/users`

Gestión CRUD de usuarios y notificaciones del usuario autenticado.

> **⚠️ Cambio en v1.1.0:** `UsersController` ahora aplica `[Authorize]` a nivel de controlador. Todos los endpoints requieren JWT (antes eran públicos).

### `GET /api/users`
- **Descripción:** Lista todos los usuarios.
- **Auth:** **Requerido**

### `GET /api/users/{id}`
- **Descripción:** Obtiene un usuario por su `Guid`.
- **Auth:** **Requerido**
- **Respuestas:** `200` · `404`

### `GET /api/users/email/{email}`
- **Descripción:** Obtiene un usuario por email.
- **Auth:** **Requerido**

### `GET /api/users/project/{projectId}`
- **Descripción:** Lista los usuarios miembros de un proyecto.
- **Auth:** **Requerido**

### `POST /api/users`
- **Descripción:** Crea un nuevo usuario (parámetros vía query string).
- **Auth:** **Requerido**
- **Query params:** `email` (req), `name` (req), `passwordHash` (req), `avatar` (opcional)
- **Respuestas:** `201` · `400` · `409` Email duplicado

### `PUT /api/users/{id}`
- **Descripción:** Actualiza un usuario existente.
- **Auth:** **Requerido**
- **Query params:** `name` (req), `avatar` (opcional)
- **Respuestas:** `200` · `400` · `404`

### `DELETE /api/users/{id}`
- **Descripción:** Elimina un usuario.
- **Auth:** **Requerido**
- **Respuestas:** `204` · `404`

### `GET /api/users/exists/{email}`
- **Descripción:** Verifica si un email está registrado.
- **Auth:** **Requerido**
- **Respuesta 200:** `{ "exists": true }`

### `GET /api/users/{userId}/project-count`
- **Descripción:** Cantidad de proyectos asociados al usuario.
- **Auth:** **Requerido**

### `GET /api/users/my-notifications`
- **Descripción:** Notificaciones del usuario autenticado (extrae el `userId` del claim).
- **Auth:** **Requerido**
- **Respuesta 200:** `IEnumerable<NotificationDto>`

### `POST /api/users/notifications/{id}/read`
- **Descripción:** Marca la notificación como leída.
- **Auth:** **Requerido**
- **Respuesta:** `204`

---

## 4. Projects — `/api/projects`

Gestión de proyectos. Implementa **Builder** en la creación y **Prototype** en el clonado. Todo el controlador requiere JWT.

### `GET /api/projects`
- **Descripción:** Lista todos los proyectos.

### `GET /api/projects/{id}`
- **Respuestas:** `200` · `404`

### `GET /api/projects/owner/{ownerId}`
- **Descripción:** Proyectos cuyo propietario es `ownerId`.

### `GET /api/projects/member/{userId}`
- **Descripción:** Proyectos donde el usuario es miembro.

### `POST /api/projects`
- **Descripción:** Crea un proyecto usando el patrón Builder.
- **Query params:** `ownerId` (Guid, requerido)
- **Body (`CreateProjectDto`):**
```json
{
  "name": "Mi Proyecto",
  "description": "Descripción opcional",
  "color": "#3498db",
  "startDate": "2026-05-08T00:00:00Z",
  "endDate": "2026-12-31T00:00:00Z",
  "statusId": 1
}
```
- **Respuestas:** `201` · `400`

### `PUT /api/projects/{id}` · `DELETE /api/projects/{id}`
- **Respuestas:** `200` · `404`

### `POST /api/projects/{projectId}/clone`
- **Descripción:** Clona un proyecto completo (Prototype).
- **Query params:** `newOwnerId` (Guid, requerido)
- **Respuestas:** `201` · `400` · `404`

### `POST /api/projects/{projectId}/members/{userId}` · `DELETE /api/projects/{projectId}/members/{userId}`
- **Descripción:** Agrega/elimina miembros (rol por defecto `3` = Developer).

### `GET /api/projects/{projectId}/members/{userId}`
- **Descripción:** Verifica si el usuario es miembro. Devuelve `ResponseDto<bool>`.

### `GET /api/projects/count/owner/{ownerId}`
- **Descripción:** Cantidad de proyectos por propietario.

---

## 5. Boards — `/api/boards`

Tableros Kanban. Todo el controlador requiere JWT.

### `GET /api/boards/{id}` · `GET /api/boards/project/{projectId}`

### `GET /api/boards/paged`
- **Query params:** `projectId` (req), `pageNumber` (def 1), `pageSize` (def 10)

### `POST /api/boards`
- **Body (`CreateBoardDto`):**
```json
{
  "name": "Sprint 1",
  "description": "Tablero del sprint",
  "projectId": "guid",
  "displayOrder": 0
}
```

### `PUT /api/boards/{id}` · `DELETE /api/boards/{id}`

---

## 6. Columns — `/api/columns`

Columnas Kanban con soporte de **WIP Limits**. Todo el controlador requiere JWT.

### `GET /api/columns/{id}` · `GET /api/columns/board/{boardId}` · `GET /api/columns/paged`

### `POST /api/columns`
- **Body (`CreateColumnDto`):**
```json
{
  "name": "To Do",
  "boardId": "guid",
  "wipLimit": 5,
  "color": "#ecf0f1"
}
```

### `PUT /api/columns/{id}` · `DELETE /api/columns/{id}`

### `GET /api/columns/{columnId}/wip-check`
- **Respuesta 200:** `ResponseDto<bool>` (`true` = se pueden añadir tareas)

### `GET /api/columns/{columnId}/task-count`
- **Respuesta 200:** `ResponseDto<int>`

---

## 7. Tasks — `/api/tasks`

CRUD y operaciones especializadas sobre tareas. Aplica **Factory Method**, **Prototype** (clonado) y **Proxy** (validaciones previas).

> **🔒 Proxy aplicado (transparente al cliente):** todas las operaciones de creación/actualización/eliminación pasan por `TaskServiceProxy`, que valida:
> - El proyecto debe estar activo (no completado/cancelado).
> - El usuario autenticado debe ser miembro o el dueño del proyecto.
>
> Si la validación falla devuelve `403` o `400` antes de llegar al servicio real.

Todo el controlador requiere JWT.

### `GET /api/tasks` · `GET /api/tasks/{id}` · `GET /api/tasks/project/{projectId}`

### `GET /api/tasks/status/{status}` — `200` · `400` (status inválido)

### `GET /api/tasks/assignee/{userId}` · `GET /api/tasks/overdue`

### `POST /api/tasks`
- **Body (`CreateTaskDto`):**
```json
{
  "title": "Implementar login",
  "description": "Descripción opcional",
  "typeId": 1,
  "columnId": "guid",
  "priorityId": 2,
  "assignedToUserId": "guid",
  "dueDate": "2026-05-15T00:00:00Z",
  "estimatedHours": 8,
  "tags": ["frontend"],
  "parentTaskId": null
}
```
- **Respuestas:** `201` · `400` · `403` (Proxy: no eres miembro) · `400` (Proxy: proyecto cerrado)

### `POST /api/tasks/by-type` *(Factory Method)*
- **Query params:** `taskType` (int, req), `title` (req), `description`, `projectId` (Guid)

### `PUT /api/tasks/{id}` · `DELETE /api/tasks/{id}`
- `DELETE` falla con `400` si la tarea tiene subtareas asociadas.

### `POST /api/tasks/{taskId}/clone` *(Prototype)*

### `GET /api/tasks/count/project/{projectId}` · `GET /api/tasks/count/status/{status}`

---

## 8. Comments — `/api/comments`

Comentarios sobre tareas. Todo el controlador requiere JWT.

### `GET /api/comments/{id}` · `GET /api/comments/task/{taskId}` · `GET /api/comments/task/{taskId}/paged`

### `GET /api/comments/user/{userId}`

### `POST /api/comments`
- **Body (`CreateCommentDto`):**
```json
{
  "content": "Texto del comentario",
  "taskId": "guid",
  "userId": "guid"
}
```

### `PUT /api/comments/{id}` · `DELETE /api/comments/{id}`

### `GET /api/comments/task/{taskId}/count`

---

## 9. Tags — `/api/tags`

Etiquetas asociadas a un proyecto. La relación N:M Task↔Tag se persiste vía la tabla intermedia **`TaskLabels`** (reemplaza la antigua `TaskTags`).

Todo el controlador requiere JWT.

### `GET /api/tags/{id}` · `GET /api/tags/project/{projectId}` · `GET /api/tags/paged`

### `GET /api/tags/search`
- **Query params:** `searchTerm` (req), `projectId` (req)

### `POST /api/tags`
- **Body (`CreateTagDto`):**
```json
{
  "name": "frontend",
  "description": "UI / Front-end",
  "color": "#3498db",
  "projectId": "guid"
}
```

### `PUT /api/tags/{id}` · `DELETE /api/tags/{id}`

### `GET /api/tags/{tagId}/task-count`

---

## 10. Attachments — `/api/attachments` *(NUEVO)*

Gestión de archivos adjuntos a tareas (RF-04.7). Almacenamiento local en `uploads/{taskId}/`. Todo el controlador requiere JWT.

**Límite:** 10 MB por archivo (configurable en `AttachmentPolicy`).

### `POST /api/attachments/upload`
- **Descripción:** Sube un archivo y lo asocia a una tarea.
- **Content-Type:** `multipart/form-data`
- **Form fields:**
  - `File` (IFormFile, req)
  - `TaskId` (Guid, req)
- **Respuestas:** `201` · `400` (sin archivo / excede límite) · `404` (task/usuario no existe)

### `GET /api/attachments/task/{taskId}`
- **Descripción:** Lista los archivos adjuntos de una tarea.
- **Respuesta 200:** `ResponseDto<List<FileDto>>`

### `GET /api/attachments/{id}`
- **Descripción:** Metadatos de un archivo.
- **Respuesta 200:**
```json
{
  "success": true,
  "data": {
    "id": "guid",
    "fileName": "spec.pdf",
    "fileUrl": "/uploads/{taskId}/{guid}_spec.pdf",
    "mimeType": "application/pdf",
    "fileSize": 234567,
    "taskId": "guid",
    "createdAt": "2026-05-08T...",
    "uploadedByUserId": "guid",
    "uploadedByUserName": "Juan"
  }
}
```

### `GET /api/attachments/{id}/download`
- **Descripción:** Descarga el contenido binario.
- **Respuesta:** `200` con `Content-Type` original del archivo · `404` si falta en disco.

### `DELETE /api/attachments/{id}`
- **Respuestas:** `200` · `404`

---

## 11. SavedFilters — `/api/savedfilters` *(NUEVO)*

Filtros guardados por usuario para listados de tareas (RF-07.3). Cada usuario sólo accede a sus propios filtros (lectura/escritura aisladas por `userId` del claim). Todo el controlador requiere JWT.

`FilterCriteria` se persiste como JSON serializado libre — la app cliente decide su forma.

### `GET /api/savedfilters`
- **Descripción:** Lista los filtros del usuario autenticado.
- **Respuesta 200:** `ResponseDto<List<SavedFilterDto>>`

### `GET /api/savedfilters/{id}`
- **Respuestas:** `200` · `404` (si no existe o pertenece a otro usuario)

### `POST /api/savedfilters`
- **Body (`CreateSavedFilterDto`):**
```json
{
  "name": "Mis pendientes urgentes",
  "filterCriteria": "{\"statusId\":1,\"priorityId\":[3,4],\"tags\":[\"backend\"]}"
}
```
- **Respuestas:** `201` · `400`

### `PUT /api/savedfilters/{id}`
- **Body (`UpdateSavedFilterDto`):**
```json
{
  "name": "Nombre nuevo (opcional)",
  "filterCriteria": "{\"statusId\":2}"
}
```
- **Respuestas:** `200` · `404`

### `DELETE /api/savedfilters/{id}`
- **Respuestas:** `200` · `404`

**`SavedFilterDto`:**
```json
{
  "id": "guid",
  "userId": "guid",
  "name": "Mis pendientes urgentes",
  "filterCriteria": "{...}",
  "createdAt": "2026-05-08T...",
  "updatedAt": "2026-05-08T..."
}
```

---

## 12. Dashboard — `/api/dashboard` *(NUEVO — patrón Facade)*

Dashboard centralizado de un proyecto (RF-08.1). Una sola llamada que orquesta tareas + miembros + métricas + cálculo de progreso (vía Composite). Todo el controlador requiere JWT.

### `GET /api/dashboard/project/{projectId}`
- **Descripción:** Devuelve el dashboard del proyecto.
- **Respuesta 200 (`ResponseDto<DashboardDto>`):**
```json
{
  "success": true,
  "message": "Dashboard generated",
  "data": {
    "projectId": "guid",
    "projectName": "TaskFlow",
    "projectStatus": "Active",
    "startDate": "2026-01-01T00:00:00Z",
    "endDate": "2026-12-31T00:00:00Z",
    "totalTasks": 84,
    "completedTasks": 32,
    "inProgressTasks": 28,
    "blockedTasks": 4,
    "overdueTasks": 6,
    "completionPercentage": 38.1,
    "memberCount": 5,
    "memberWorkloads": [
      {
        "userId": "guid",
        "userName": "Juan",
        "assignedTasks": 18,
        "completedTasks": 7
      }
    ],
    "totalEstimatedHours": 320,
    "totalActualHours": 142,
    "upcomingDeadlines": [
      {
        "taskId": "guid",
        "title": "Login OAuth",
        "dueDate": "2026-05-12T00:00:00Z",
        "priorityId": 3
      }
    ],
    "generatedAt": "2026-05-08T..."
  }
}
```
- **Errores:** `404` Proyecto no encontrado

> El cálculo de `completionPercentage` usa el patrón **Composite** sobre el árbol de tareas/subtareas (media ponderada recursiva por `Weight`).

---

## 13. Reports — `/api/reports` *(NUEVO — patrón Bridge)*

Generación de reportes en múltiples formatos. La abstracción `Report` (qué datos van) está desacoplada del formato (`IReportFormat` = PDF / CSV / extensible). Todo el controlador requiere JWT.

### `GET /api/reports/project/{projectId}`
- **Descripción:** Genera el reporte del proyecto en el formato solicitado.
- **Query params:**
  - `format` (string, opcional, default `pdf`) — valores: `pdf` | `csv`
- **Headers de respuesta:**
  - `Content-Type: application/pdf` o `text/csv` (UTF-8 con BOM Excel-friendly)
  - `Content-Disposition: attachment; filename=project-{guid}-{yyyyMMddHHmm}.{ext}`
- **Body de respuesta:** binario.
- **Errores:**
  - `400` Formato no soportado
  - `404` Proyecto no encontrado

**Estructura del reporte:**
- Sección 1 — Resumen: total de tareas, completadas, en progreso, bloqueadas, miembros, tableros.
- Sección 2 — Detalle de tareas: título, estado, prioridad, fecha límite, horas estimadas vs reales.

> Para añadir un formato nuevo (Excel, JSON enriquecido) basta con implementar `IReportFormat` — no se toca la jerarquía `Report`.

---

## 14. Themes — `/api/themes`

Configuración de temas visuales (Abstract Factory).

> **⚠️ Cambio en v1.1.0:** ahora aplica `[Authorize]` a nivel de controlador. Todos los endpoints requieren JWT (antes eran públicos).

### `GET /api/themes`
- **Auth:** **Requerido**
- **Respuesta 200:** `List<ThemeConfiguration>`

### `GET /api/themes/{themeName}`
- **Valores válidos:** `dark`, `light`
- **Respuestas:** `200` · `400` (nombre inválido)

### `GET /api/themes/info/available`
- **Respuesta 200:**
```json
{
  "available": ["dark", "light"],
  "description": "Use theme names to fetch specific theme configurations"
}
```

> El backend persiste la preferencia del usuario en `User.ThemePreference` como string (`"Light"` / `"Dark"` / `"System"`), listo para alimentar el Abstract Factory de UI en el cliente Angular/React.

---

## 15. BaseCatalog — `/api/basecatalog`

Catálogos de datos base. Todos los endpoints retornan `IEnumerable<CatalogDto>` con la forma `{ id, name }`.

> **⚠️ Cambio en v1.1.0:** ahora aplica `[Authorize]` a nivel de controlador. Todos los endpoints requieren JWT (antes eran públicos).

### `GET /api/basecatalog/task-types`
- **Auth:** **Requerido**
- **Valores seed:** Feature, Bug, Improvement, Research, Task, SubTask

### `GET /api/basecatalog/task-priorities`
- **Auth:** **Requerido**
- **Valores seed:** LOW, MEDIUM, HIGH, CRITICAL

### `GET /api/basecatalog/task-status`
- **Auth:** **Requerido**
- **Valores seed:** To Do, In Progress, Done, Blocked

### `GET /api/basecatalog/app-roles`
- **Auth:** **Requerido**
- **Valores seed:** Admin, CommonUser

### `GET /api/basecatalog/project-roles`
- **Auth:** **Requerido**
- **Valores seed:** Creator, Project Manager, Developer

### `GET /api/basecatalog/project-status`
- **Auth:** **Requerido**
- **Valores seed:** Active, Completed, On Hold, Cancelled

---

## Códigos de estado comunes

| Código | Significado |
|--------|-------------|
| 200 | OK — Operación exitosa |
| 201 | Created — Recurso creado |
| 204 | No Content — Operación exitosa sin cuerpo |
| 400 | Bad Request — Validación o regla de negocio fallida |
| 401 | Unauthorized — Token ausente, inválido, expirado o **revocado (logout)** |
| 403 | Forbidden — Sin permisos suficientes (rol/política/Proxy de Tasks) |
| 404 | Not Found — Recurso no existe |
| 409 | Conflict — Conflicto (p. ej. email duplicado) |
| 500 | Internal Server Error — Error inesperado |
| 503 | Service Unavailable — Health check de DB falló |

---

## Resumen rápido de endpoints

| # | Método | Ruta | Auth | Patrón |
|---|--------|------|------|--------|
| 1 | GET | `/` | Público | — |
| 2 | GET | `/health` | Público | — |
| 3 | GET | `/api/health` | Público | — |
| 4 | GET | `/api/health/info` | Público | — |
| 5 | GET | `/api/health/database` | Público | — |
| 6 | POST | `/api/auth/login` | Público | — |
| 7 | POST | `/api/auth/register` | Público | — |
| 8 | GET | `/api/auth/validate` | JWT | — |
| 9 | **POST** | **`/api/auth/logout`** | **JWT** | **Blacklist** |
| 10 | GET | `/api/users` | JWT | — |
| 11 | GET | `/api/users/{id}` | JWT | — |
| 12 | GET | `/api/users/email/{email}` | JWT | — |
| 13 | GET | `/api/users/project/{projectId}` | JWT | — |
| 14 | POST | `/api/users` | JWT | — |
| 15 | PUT | `/api/users/{id}` | JWT | — |
| 16 | DELETE | `/api/users/{id}` | JWT | — |
| 17 | GET | `/api/users/exists/{email}` | JWT | — |
| 18 | GET | `/api/users/{userId}/project-count` | JWT | — |
| 19 | GET | `/api/users/my-notifications` | JWT | Adapter |
| 20 | POST | `/api/users/notifications/{id}/read` | JWT | Adapter |
| 21 | GET | `/api/projects` | JWT | — |
| 22 | GET | `/api/projects/{id}` | JWT | — |
| 23 | GET | `/api/projects/owner/{ownerId}` | JWT | — |
| 24 | GET | `/api/projects/member/{userId}` | JWT | — |
| 25 | POST | `/api/projects` | JWT | Builder |
| 26 | PUT | `/api/projects/{id}` | JWT | — |
| 27 | DELETE | `/api/projects/{id}` | JWT | — |
| 28 | POST | `/api/projects/{projectId}/clone` | JWT | Prototype |
| 29 | POST | `/api/projects/{projectId}/members/{userId}` | JWT | — |
| 30 | DELETE | `/api/projects/{projectId}/members/{userId}` | JWT | — |
| 31 | GET | `/api/projects/{projectId}/members/{userId}` | JWT | — |
| 32 | GET | `/api/projects/count/owner/{ownerId}` | JWT | — |
| 33 | GET | `/api/boards/{id}` | JWT | — |
| 34 | GET | `/api/boards/project/{projectId}` | JWT | — |
| 35 | GET | `/api/boards/paged` | JWT | — |
| 36 | POST | `/api/boards` | JWT | — |
| 37 | PUT | `/api/boards/{id}` | JWT | — |
| 38 | DELETE | `/api/boards/{id}` | JWT | — |
| 39 | GET | `/api/columns/{id}` | JWT | — |
| 40 | GET | `/api/columns/board/{boardId}` | JWT | — |
| 41 | GET | `/api/columns/paged` | JWT | — |
| 42 | POST | `/api/columns` | JWT | — |
| 43 | PUT | `/api/columns/{id}` | JWT | — |
| 44 | DELETE | `/api/columns/{id}` | JWT | — |
| 45 | GET | `/api/columns/{columnId}/wip-check` | JWT | — |
| 46 | GET | `/api/columns/{columnId}/task-count` | JWT | — |
| 47 | GET | `/api/tasks` | JWT | — |
| 48 | GET | `/api/tasks/{id}` | JWT | — |
| 49 | GET | `/api/tasks/project/{projectId}` | JWT | — |
| 50 | GET | `/api/tasks/status/{status}` | JWT | — |
| 51 | GET | `/api/tasks/assignee/{userId}` | JWT | — |
| 52 | GET | `/api/tasks/overdue` | JWT | — |
| 53 | POST | `/api/tasks` | JWT | **Proxy** |
| 54 | POST | `/api/tasks/by-type` | JWT | **Proxy** + Factory |
| 55 | PUT | `/api/tasks/{id}` | JWT | **Proxy** |
| 56 | DELETE | `/api/tasks/{id}` | JWT | **Proxy** |
| 57 | POST | `/api/tasks/{taskId}/clone` | JWT | **Proxy** + Prototype |
| 58 | GET | `/api/tasks/count/project/{projectId}` | JWT | — |
| 59 | GET | `/api/tasks/count/status/{status}` | JWT | — |
| 60 | GET | `/api/comments/{id}` | JWT | — |
| 61 | GET | `/api/comments/task/{taskId}` | JWT | — |
| 62 | GET | `/api/comments/task/{taskId}/paged` | JWT | — |
| 63 | GET | `/api/comments/user/{userId}` | JWT | — |
| 64 | POST | `/api/comments` | JWT | — |
| 65 | PUT | `/api/comments/{id}` | JWT | — |
| 66 | DELETE | `/api/comments/{id}` | JWT | — |
| 67 | GET | `/api/comments/task/{taskId}/count` | JWT | — |
| 68 | GET | `/api/tags/{id}` | JWT | — |
| 69 | GET | `/api/tags/project/{projectId}` | JWT | — |
| 70 | GET | `/api/tags/paged` | JWT | — |
| 71 | GET | `/api/tags/search` | JWT | — |
| 72 | POST | `/api/tags` | JWT | — |
| 73 | PUT | `/api/tags/{id}` | JWT | — |
| 74 | DELETE | `/api/tags/{id}` | JWT | — |
| 75 | GET | `/api/tags/{tagId}/task-count` | JWT | — |
| 76 | **POST** | **`/api/attachments/upload`** | **JWT** | — |
| 77 | **GET** | **`/api/attachments/task/{taskId}`** | **JWT** | — |
| 78 | **GET** | **`/api/attachments/{id}`** | **JWT** | — |
| 79 | **GET** | **`/api/attachments/{id}/download`** | **JWT** | — |
| 80 | **DELETE** | **`/api/attachments/{id}`** | **JWT** | — |
| 81 | **GET** | **`/api/savedfilters`** | **JWT** | — |
| 82 | **GET** | **`/api/savedfilters/{id}`** | **JWT** | — |
| 83 | **POST** | **`/api/savedfilters`** | **JWT** | — |
| 84 | **PUT** | **`/api/savedfilters/{id}`** | **JWT** | — |
| 85 | **DELETE** | **`/api/savedfilters/{id}`** | **JWT** | — |
| 86 | **GET** | **`/api/dashboard/project/{projectId}`** | **JWT** | **Facade + Composite** |
| 87 | **GET** | **`/api/reports/project/{projectId}`** | **JWT** | **Bridge** |
| 88 | GET | `/api/themes` | JWT | Abstract Factory |
| 89 | GET | `/api/themes/{themeName}` | JWT | Abstract Factory |
| 90 | GET | `/api/themes/info/available` | JWT | — |
| 91 | GET | `/api/basecatalog/task-types` | JWT | — |
| 92 | GET | `/api/basecatalog/task-priorities` | JWT | — |
| 93 | GET | `/api/basecatalog/task-status` | JWT | — |
| 94 | GET | `/api/basecatalog/app-roles` | JWT | — |
| 95 | GET | `/api/basecatalog/project-roles` | JWT | — |
| 96 | GET | `/api/basecatalog/project-status` | JWT | — |

> **Filas en negrita = nuevas o modificadas en v1.1.0.**

---

## Mapa de patrones de diseño

| Patrón | Tipo | Dónde se usa | Endpoints relacionados |
|---|---|---|---|
| Singleton | Creacional | `TokenBlacklistService` (caché en memoria), `TaskFlyweightFactory` | Transversal |
| Factory Method | Creacional | Creación de tareas por tipo (`TaskFactoryProvider`) | `POST /api/tasks/by-type` |
| Abstract Factory | Creacional | `ThemeFactory` (Light/Dark) | `/api/themes/*` |
| Builder | Creacional | `ProjectBuilder`, `BoardBuilder`, `TaskBuilder` | `POST /api/projects`, `POST /api/boards` |
| Prototype | Creacional | Clonado profundo de proyectos y tareas | `POST /api/projects/{id}/clone`, `POST /api/tasks/{id}/clone` |
| **Adapter** | **Estructural** | `INotificationAdapter` (InApp / SMTP / SendGrid) | Notificaciones internas (`/api/users/my-notifications`) |
| **Bridge** | **Estructural** | `Report` × `IReportFormat` (PDF/CSV) | `GET /api/reports/project/{id}` |
| **Composite** | **Estructural** | Árbol de subtareas con progreso unificado | Cálculo interno usado por `Dashboard` |
| **Decorator** | **Estructural** | `TaskViewDecorator` (Tags / Attachments / Overdue / PriorityColor) | Vista enriquecida de tareas (consumido por el cliente) |
| **Facade** | **Estructural** | `ProjectFacade` orquesta tareas + miembros + métricas | `GET /api/dashboard/project/{id}` |
| **Proxy** | **Estructural** | `TaskServiceProxy` valida proyecto activo + membresía | `POST/PUT/DELETE /api/tasks/*` |
| **Flyweight** | **Estructural** | `TaskFlyweightFactory` cachea metadata compartida | Transversal en payloads grandes |

---

## Notas operativas

1. **Logout real:** después de `POST /api/auth/logout` el token queda invalidado para siempre (hasta su expiración natural). El frontend debe descartar su almacenamiento local del token.
2. **Caché de blacklist:** se carga en memoria al arrancar la app desde la tabla `RevokedTokens` (`TokenBlacklistService.LoadFromDatabaseAsync`). El cache se actualiza inmediatamente al hacer logout.
3. **Limpieza de tokens revocados expirados:** `RevokedTokenRepository.PurgeExpiredAsync()` está disponible para tareas de mantenimiento (cron).
4. **Migración EF Core pendiente:** la nueva versión introduce `RevokedTokens`, `SavedFilters`, `TaskLabels`, `PasswordPolicies`, `AttachmentPolicies` y renombra columnas en `Users`. Aplica con `dotnet ef database update`.
5. **Almacenamiento de adjuntos:** los archivos se guardan en `{ContentRoot}/uploads/{taskId}/`. En despliegues productivos considera mover a Azure Blob / S3 cambiando sólo el `AttachmentsController` (ningún otro componente lo conoce).
