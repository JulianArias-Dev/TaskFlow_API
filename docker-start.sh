#!/bin/bash

# TaskFlow API Docker Startup Script
# Script para inicializar la aplicación con Docker Compose

set -e

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Functions
print_info() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Main Script
print_info "TaskFlow API - Docker Setup"
echo ""

# Check if Docker is installed
if ! command -v docker &> /dev/null; then
    print_error "Docker is not installed. Please install Docker first."
    exit 1
fi

print_info "Docker is installed"

# Check if Docker Compose is installed
if ! command -v docker-compose &> /dev/null; then
    print_error "Docker Compose is not installed. Please install Docker Compose first."
    exit 1
fi

print_info "Docker Compose is installed"
echo ""

# Remove old containers and volumes
print_warning "Stopping and removing old containers..."
docker-compose down --volumes || true
echo ""

# Build and start services
print_info "Building and starting services..."
docker-compose up --build -d
echo ""

# Wait for services to be healthy
print_info "Waiting for services to be healthy..."
sleep 10

# Check SQL Server health
print_info "Checking SQL Server health..."
if docker-compose ps sqlserver | grep -q "healthy"; then
    print_info "SQL Server is running and healthy"
else
    print_warning "SQL Server is starting, this may take a moment..."
    sleep 20
fi

echo ""
print_info "Setup completed successfully!"
echo ""
echo "Services are running:"
echo "  - SQL Server:   localhost:1433"
echo "  - TaskFlow API: http://localhost:8080"
echo ""
echo "Useful commands:"
echo "  - View logs:           docker-compose logs -f"
echo "  - Stop services:       docker-compose down"
echo "  - View containers:     docker-compose ps"
echo "  - Access API Swagger:  http://localhost:8080/swagger"
echo "  - API Health Check:    http://localhost:8080/api/health"
echo ""
