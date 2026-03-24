# TaskFlow API Docker Startup Script (PowerShell)
# Script para inicializar la aplicación con Docker Compose en Windows

$ErrorActionPreference = "Stop"

# Colors
function Print-Info {
    param([string]$Message)
    Write-Host "[INFO] $Message" -ForegroundColor Green
}

function Print-Warning {
    param([string]$Message)
    Write-Host "[WARNING] $Message" -ForegroundColor Yellow
}

function Print-Error {
    param([string]$Message)
    Write-Host "[ERROR] $Message" -ForegroundColor Red
}

# Main Script
Print-Info "TaskFlow API - Docker Setup (Windows)"
Write-Host ""

# Check if Docker is installed
try {
    $dockerVersion = docker --version
    Print-Info "Docker is installed: $dockerVersion"
} catch {
    Print-Error "Docker is not installed. Please install Docker Desktop first."
    exit 1
}

# Check if Docker Compose is installed
try {
    $dockerComposeVersion = docker-compose --version
    Print-Info "Docker Compose is installed: $dockerComposeVersion"
} catch {
    Print-Error "Docker Compose is not installed. Please install Docker Desktop with Compose support."
    exit 1
}

Write-Host ""

# Remove old containers and volumes
Print-Warning "Stopping and removing old containers..."
docker-compose down --volumes | Out-Null
Write-Host ""

# Build and start services
Print-Info "Building and starting services..."
docker-compose up --build -d
Write-Host ""

# Wait for services to be healthy
Print-Info "Waiting for services to be healthy..."
Start-Sleep -Seconds 10

# Check SQL Server health
Print-Info "Checking SQL Server health..."
$sqlserverStatus = docker-compose ps sqlserver | Select-String "healthy"
if ($sqlserverStatus) {
    Print-Info "SQL Server is running and healthy"
} else {
    Print-Warning "SQL Server is starting, this may take a moment..."
    Start-Sleep -Seconds 20
}

Write-Host ""
Print-Info "Setup completed successfully!"
Write-Host ""
Write-Host "Services are running:"
Write-Host "  - SQL Server:   localhost:1433"
Write-Host "  - TaskFlow API: http://localhost:8080"
Write-Host ""
Write-Host "Useful commands:"
Write-Host "  - View logs:           docker-compose logs -f"
Write-Host "  - Stop services:       docker-compose down"
Write-Host "  - View containers:     docker-compose ps"
Write-Host "  - Access API Swagger:  http://localhost:8080/swagger"
Write-Host "  - API Health Check:    http://localhost:8080/api/health"
Write-Host ""
