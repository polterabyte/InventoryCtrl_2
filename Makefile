# Inventory Control System - Docker Management

.PHONY: help build run stop clean logs dev prod ssl

# Default target
help:
	@echo "Inventory Control System - Docker Commands"
	@echo ""
	@echo "Development:"
	@echo "  make dev          - Start in development mode"
	@echo "  make dev-build    - Build and start in development mode"
	@echo ""
	@echo "Production:"
	@echo "  make prod         - Start in production mode"
	@echo "  make prod-build   - Build and start in production mode"
	@echo "  make ssl          - Generate SSL certificates for development"
	@echo ""
	@echo "Management:"
	@echo "  make build        - Build all Docker images"
	@echo "  make run          - Start all services"
	@echo "  make stop         - Stop all services"
	@echo "  make clean        - Clean up containers and volumes"
	@echo "  make logs         - Show logs for all services"
	@echo "  make logs-api     - Show API logs"
	@echo "  make logs-web     - Show Web Client logs"
	@echo "  make logs-db      - Show Database logs"

# Development commands
dev:
	@echo "🚀 Starting in development mode..."
	powershell -ExecutionPolicy Bypass -File docker-run.ps1 -Environment development

dev-build:
	@echo "🔨 Building and starting in development mode..."
	powershell -ExecutionPolicy Bypass -File docker-run.ps1 -Environment development -Build

# Production commands
prod:
	@echo "🚀 Starting in production mode..."
	powershell -ExecutionPolicy Bypass -File docker-run.ps1 -Environment production

prod-build:
	@echo "🔨 Building and starting in production mode..."
	powershell -ExecutionPolicy Bypass -File docker-run.ps1 -Environment production -Build

# SSL certificate generation
ssl:
	@echo "🔐 Generating SSL certificates..."
	powershell -ExecutionPolicy Bypass -File scripts/generate-ssl.ps1

# Build commands
build:
	@echo "📦 Building all Docker images..."
	powershell -ExecutionPolicy Bypass -File docker-build.ps1

build-no-cache:
	@echo "📦 Building all Docker images (no cache)..."
	powershell -ExecutionPolicy Bypass -File docker-build.ps1 -NoCache

# Run commands
run:
	@echo "🚀 Starting all services..."
	docker-compose up -d

# Stop commands
stop:
	@echo "🛑 Stopping all services..."
	docker-compose down

stop-prod:
	@echo "🛑 Stopping production services..."
	docker-compose -f docker-compose.prod.yml down

# Clean commands
clean:
	@echo "🧹 Cleaning up containers and volumes..."
	docker-compose down -v --remove-orphans
	docker system prune -f

clean-prod:
	@echo "🧹 Cleaning up production containers and volumes..."
	docker-compose -f docker-compose.prod.yml down -v --remove-orphans
	docker system prune -f

# Log commands
logs:
	@echo "📋 Showing logs for all services..."
	docker-compose logs -f

logs-api:
	@echo "📋 Showing API logs..."
	docker-compose logs -f inventory-api

logs-web:
	@echo "📋 Showing Web Client logs..."
	docker-compose logs -f inventory-web

logs-db:
	@echo "📋 Showing Database logs..."
	docker-compose logs -f postgres

# Status commands
status:
	@echo "📊 Container status:"
	docker ps --filter "name=inventory-"

# Health check
health:
	@echo "🏥 Health check:"
	@echo "API Health:"
	@curl -s http://localhost:5000/health || echo "API not responding"
	@echo ""
	@echo "Web Health:"
	@curl -s http://localhost/health || echo "Web not responding"
