# Docker Deployment Guide

Этот документ описывает процесс развертывания системы управления складом (Inventory Control System) с использованием Docker и nginx.

## Архитектура

Система состоит из следующих компонентов:

- **PostgreSQL** - база данных
- **Inventory API** - .NET 9.0 Web API
- **Inventory Web Client** - Blazor WebAssembly приложение
- **Nginx** - веб-сервер и reverse proxy (для production)

## Быстрый старт

### 1. Клонирование и подготовка

```bash
git clone <repository-url>
cd InventoryCtrl_2
```

### 2. Настройка переменных окружения

Скопируйте файл `.env.example` в `.env` и настройте переменные:

```bash
cp .env.example .env
```

Отредактируйте `.env` файл с вашими настройками.

### 3. Запуск в режиме разработки

```powershell
# Сборка и запуск всех сервисов
.\docker-run.ps1 -Environment development -Build

# Или только запуск (если образы уже собраны)
.\docker-run.ps1 -Environment development
```

### 4. Запуск в production режиме

```powershell
# Сборка и запуск в production режиме
.\docker-run.ps1 -Environment production -Build

# Или только запуск
.\docker-run.ps1 -Environment production
```

## Доступ к приложению

После успешного запуска приложение будет доступно по следующим адресам:

- **Web Application**: http://localhost
- **API**: http://localhost:5000
- **API Swagger**: http://localhost:5000/swagger
- **Database**: localhost:5432

Для production режима с SSL:
- **HTTPS**: https://localhost

## Управление контейнерами

### Просмотр логов

```bash
# Все сервисы
docker-compose logs -f

# Конкретный сервис
docker-compose logs -f inventory-api
docker-compose logs -f inventory-web
docker-compose logs -f postgres
```

### Остановка сервисов

```bash
# Остановка без удаления данных
docker-compose down

# Остановка с удалением данных
docker-compose down -v
```

### Перезапуск сервиса

```bash
# Перезапуск конкретного сервиса
docker-compose restart inventory-api
```

## Настройка SSL для Production

1. Поместите ваши SSL сертификаты в папку `nginx/ssl/`:
   - `cert.pem` - сертификат
   - `key.pem` - приватный ключ

2. Запустите в production режиме:
   ```bash
   .\docker-run.ps1 -Environment production
   ```

## Мониторинг и отладка

### Health Checks

Все сервисы имеют встроенные health checks:

```bash
# Проверка состояния контейнеров
docker ps

# Детальная информация о health checks
docker inspect inventory-api | findstr Health -A 10
```

### Подключение к базе данных

```bash
# Подключение к PostgreSQL
docker exec -it inventory-postgres psql -U postgres -d inventorydb
```

### Просмотр логов приложения

```bash
# Логи API
docker logs inventory-api

# Логи Web Client
docker logs inventory-web
```

## Масштабирование

### Горизонтальное масштабирование API

```bash
# Запуск нескольких экземпляров API
docker-compose up --scale inventory-api=3
```

### Настройка ресурсов

Отредактируйте `docker-compose.prod.yml` для настройки лимитов ресурсов:

```yaml
deploy:
  resources:
    limits:
      memory: 1G
      cpus: '0.5'
    reservations:
      memory: 512M
      cpus: '0.25'
```

## Безопасность

### Рекомендации для Production

1. **Измените пароли по умолчанию** в `.env` файле
2. **Используйте сильные JWT ключи** (минимум 32 символа)
3. **Настройте SSL сертификаты** для HTTPS
4. **Ограничьте доступ к базе данных** (не открывайте порт 5432 наружу)
5. **Регулярно обновляйте** Docker образы

### Firewall настройки

Для production рекомендуется открыть только необходимые порты:

- **80** - HTTP (для редиректа на HTTPS)
- **443** - HTTPS
- **22** - SSH (для управления)

## Устранение неполадок

### Проблемы с подключением к базе данных

```bash
# Проверка состояния PostgreSQL
docker-compose logs postgres

# Проверка подключения
docker exec -it inventory-postgres pg_isready -U postgres
```

### Проблемы с API

```bash
# Проверка логов API
docker-compose logs inventory-api

# Проверка health check
curl http://localhost:5000/health
```

### Проблемы с Web Client

```bash
# Проверка логов Web Client
docker-compose logs inventory-web

# Проверка nginx конфигурации
docker exec -it inventory-web nginx -t
```

### Очистка системы

```bash
# Удаление всех контейнеров и образов
docker-compose down -v --rmi all

# Очистка неиспользуемых ресурсов
docker system prune -a
```

## Дополнительные команды

### Сборка образов

```powershell
# Сборка всех образов
.\docker-build.ps1

# Сборка без кэша
.\docker-build.ps1 -NoCache

# Сборка для production
.\docker-build.ps1 -Environment production
```

### Backup базы данных

```bash
# Создание backup
docker exec inventory-postgres pg_dump -U postgres inventorydb > backup.sql

# Восстановление из backup
docker exec -i inventory-postgres psql -U postgres inventorydb < backup.sql
```

## Поддержка

При возникновении проблем:

1. Проверьте логи сервисов
2. Убедитесь, что все порты свободны
3. Проверьте настройки в `.env` файле
4. Обратитесь к документации проекта
