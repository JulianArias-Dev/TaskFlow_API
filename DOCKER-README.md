# TaskFlow API - Docker Deployment Guide

## Overview
TaskFlow API es una API REST para gestión de tareas Kanban construida con ASP.NET Core 9.0 y Entity Framework Core, con SQL Server como base de datos.

## Requisitos Previos

### Local (Sin Docker)
- .NET 9.0 SDK
- SQL Server 2019 o superior
- Visual Studio 2022 o Visual Studio Code

### Docker
- Docker Engine 20.10+
- Docker Compose 1.29+

## Instalación Rápida con Docker

### Linux/macOS
```bash
chmod +x docker-start.sh
./docker-start.sh
```

### Windows (PowerShell)
```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
.\docker-start.ps1
```

### Manual
```bash
docker-compose up --build -d
```

## Estructura de Servicios

### SQL Server
- **Container:** taskflow-sqlserver
- **Puerto:** 1433
- **Usuario:** sa
- **Contraseña:** Unicesar+2026
- **Base de datos:** TaskFlowDB
- **Volumen:** sqlserver_data (persistencia)

### TaskFlow API
- **Container:** taskflow-api
- **Imagen:** julianariasdev/taskflow-api:latest
- **Puerto:** 8080
- **Entorno:** Development/Production
- **Health Check:** http://localhost:8080/api/health

## Endpoints Disponibles

### Health & Information
```
GET  /api/health              - Estado de la API
GET  /api/health/info         - Información de la API
GET  /api/health/database     - Estado de la BD
```

### Tasks
```
GET    /api/tasks                          - Obtener todas las tareas
GET    /api/tasks/{id}                     - Obtener tarea por ID
GET    /api/tasks/project/{projectId}      - Tareas de un proyecto
GET    /api/tasks/status/{status}          - Tareas por estado
GET    /api/tasks/assignee/{userId}        - Tareas asignadas
GET    /api/tasks/overdue                  - Tareas vencidas
POST   /api/tasks                          - Crear tarea
POST   /api/tasks/by-type                  - Crear tarea por tipo
PUT    /api/tasks/{id}                     - Actualizar tarea
DELETE /api/tasks/{id}                     - Eliminar tarea
POST   /api/tasks/{taskId}/clone           - Clonar tarea
GET    /api/tasks/count/project/{id}       - Contar tareas por proyecto
GET    /api/tasks/count/status/{status}    - Contar tareas por estado
```

### Projects
```
GET    /api/projects                                - Obtener todos los proyectos
GET    /api/projects/{id}                          - Obtener proyecto por ID
GET    /api/projects/owner/{ownerId}               - Proyectos por propietario
GET    /api/projects/member/{userId}               - Proyectos por miembro
POST   /api/projects                               - Crear proyecto
PUT    /api/projects/{id}                          - Actualizar proyecto
DELETE /api/projects/{id}                          - Eliminar proyecto
POST   /api/projects/{projectId}/clone             - Clonar proyecto
POST   /api/projects/{projectId}/members/{userId}  - Agregar miembro
DELETE /api/projects/{projectId}/members/{userId}  - Eliminar miembro
GET    /api/projects/{projectId}/members/{userId}  - Verificar membresía
GET    /api/projects/count/owner/{ownerId}        - Contar proyectos
```

### Users
```
GET    /api/users                    - Obtener todos los usuarios
GET    /api/users/{id}               - Obtener usuario por ID
GET    /api/users/email/{email}      - Obtener usuario por email
GET    /api/users/project/{projectId} - Usuarios de un proyecto
POST   /api/users                    - Crear usuario
PUT    /api/users/{id}               - Actualizar usuario
DELETE /api/users/{id}               - Eliminar usuario
GET    /api/users/exists/{email}     - Verificar si email existe
GET    /api/users/{userId}/project-count - Contar proyectos de usuario
```

### Themes
```
GET /api/themes              - Obtener todos los temas
GET /api/themes/{themeName}  - Obtener tema específico
GET /api/themes/info/available - Información de temas disponibles
```

## Documentación de API

Accede a la documentación interactiva de Swagger:
```
http://localhost:8080/swagger
```

## Comandos Útiles

### Gestión de Contenedores
```bash
# Ver estado de los contenedores
docker-compose ps

# Ver logs
docker-compose logs -f

# Ver logs de un servicio específico
docker-compose logs -f taskflow-api
docker-compose logs -f sqlserver

# Detener servicios
docker-compose down

# Detener y eliminar volúmenes
docker-compose down -v

# Reiniciar servicios
docker-compose restart

# Reconstruir imagen
docker-compose up --build -d
```

### Acceso a Base de Datos
```bash
# Acceder a SQL Server directamente
docker exec -it taskflow-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P Unicesar+2026

# Ejecutar script SQL
docker exec -it taskflow-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P Unicesar+2026 -i /path/to/script.sql
```

### Debugging
```bash
# Ver variables de entorno del contenedor
docker-compose config

# Inspeccionar contenedor
docker inspect taskflow-api

# Ejecutar comando en contenedor
docker exec -it taskflow-api bash
```

## Configuración de Entorno

### Variables de Entorno (docker-compose.yml)
```yaml
ASPNETCORE_ENVIRONMENT: Development    # Development/Production
ASPNETCORE_URLS: http://+:8080         # URL de escucha
ConnectionStrings__DefaultConnection: "..."  # String de conexión
```

### Archivo .env (Opcional)
Copia `.env.example` a `.env` y modifica los valores según necesites.

## Arquitectura

### Patrones de Diseño Implementados
- **Singleton:** DbContext (garantiza única instancia)
- **Factory Method:** Creación de tareas por tipo
- **Abstract Factory:** Configuración de temas visuales
- **Prototype:** Clonado profundo de tareas y proyectos
- **Builder:** Construcción paso a paso de tareas y proyectos
- **Repository:** Acceso a datos abstraído
- **Unit of Work:** Coordinación de repositorios
- **Dependency Injection:** Inyección de dependencias

### Capas Arquitectónicas
```
Controllers (HTTP Endpoints)
    ?
Services (Lógica de Negocio)
    ?
Repositories (Acceso a Datos)
    ?
Models + DbContext (Entidades)
    ?
SQL Server (Base de Datos)
```

## Seguridad

?? **Nota Importante para Producción:**
- Cambiar credenciales de SQL Server
- Usar variables de entorno para datos sensibles
- Implementar autenticación (JWT, OAuth2, etc.)
- Usar HTTPS en lugar de HTTP
- Configurar CORS apropiadamente
- Implementar rate limiting y validación

## Troubleshooting

### SQL Server no inicia
```bash
# Verificar logs
docker-compose logs sqlserver

# Aumentar espera de inicio
# Modificar health check en docker-compose.yml
```

### API no conecta a BD
- Verificar que SQL Server esté healthy: `docker-compose ps`
- Revisar string de conexión en appsettings.json
- Verificar network: `docker network ls`

### Puerto ya en uso
```bash
# Cambiar puertos en docker-compose.yml
ports:
  - "3306:1433"    # SQL Server en puerto 3306
  - "8081:8080"    # API en puerto 8081
```

## Build de Imagen para Registro (Docker Hub)

```bash
# Build
docker build -t julianariasdev/taskflow-api:latest .

# Push (requiere autenticación)
docker login
docker push julianariasdev/taskflow-api:latest
```

## Próximos Pasos

- Implementar autenticación y autorización
- Agregar validación de datos más robusta
- Configurar CI/CD (GitHub Actions, Azure DevOps)
- Implementar logging centralizado (Serilog, ELK)
- Agregar caching (Redis)
- Implementar message queues (RabbitMQ, Service Bus)
- Configurar monitoring (Prometheus, Grafana)

---

**Versión:** 1.0.0  
**Framework:** .NET 9.0  
**Base de Datos:** SQL Server 2022  
**Última actualización:** 2024
