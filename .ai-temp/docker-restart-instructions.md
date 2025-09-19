# Инструкции по исправлению проблемы авторизации в Docker

## Проблема
- Клиент пытался подключиться к `https://localhost:7000`, но API работает на `http://localhost:5000`
- CORS не был настроен для Docker окружения
- Неправильная конфигурация URL в клиенте

## Исправления
1. **Обновлен URL API в клиенте**: `src/Inventory.Web.Client/appsettings.json`
   - Изменен с `https://localhost:7000` на `http://localhost:5000`

2. **Обновлена CORS конфигурация**: `src/Inventory.API/Services/PortConfigurationService.cs`
   - Добавлены Docker-специфичные origins
   - Добавлены общие development origins

3. **Обновлена логика определения URL в клиенте**: `src/Inventory.Web.Client/Services/PortConfigurationService.cs`
   - Упрощена логика для Blazor WebAssembly
   - Добавлено логирование для отладки

4. **Обновлен Docker Compose**: `docker-compose.yml`
   - Добавлены переменные окружения для веб-клиента

## Шаги для применения исправлений

### 1. Остановить текущие контейнеры
```powershell
docker-compose down
```

### 2. Пересобрать образы с новыми изменениями
```powershell
docker-compose build --no-cache
```

### 3. Запустить приложение заново
```powershell
docker-compose up -d
```

### 4. Проверить логи
```powershell
# Логи API
docker logs inventory-api

# Логи веб-клиента
docker logs inventory-web
```

### 5. Протестировать подключение
```powershell
# Запустить тест подключения
.\.ai-temp\test-api-connection.ps1
```

## Ожидаемый результат
- API должен быть доступен на `http://localhost:5000`
- Веб-клиент должен быть доступен на `http://localhost:80`
- Авторизация должна работать без ошибок CORS
- В логах API должны появляться запросы на авторизацию

## Отладка
Если проблема остается:
1. Проверить, что порт 5000 не занят другими приложениями
2. Проверить логи контейнеров на наличие ошибок
3. Убедиться, что база данных PostgreSQL запущена и доступна
4. Проверить, что все сервисы находятся в одной Docker сети
