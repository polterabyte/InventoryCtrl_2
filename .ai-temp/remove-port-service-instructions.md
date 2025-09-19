# Удаление PortConfigurationService и ports.json

## Внесенные изменения

### 1. Удаленные файлы
- `src/Inventory.Web.Client/Services/PortConfigurationService.cs`
- `src/Inventory.API/Services/PortConfigurationService.cs`
- `ports.json`

### 2. Созданные файлы
- `src/Inventory.Web.Client/Services/WebBaseApiService.cs` - новый базовый сервис для Web.Client
- `src/Inventory.Web.Client/Services/WebAuthApiService.cs` - Web-версия AuthApiService

### 3. Обновленные файлы
- `src/Inventory.Web.Client/Program.cs` - убрана регистрация PortConfigurationService
- `src/Inventory.API/Program.cs` - убрана логика работы с портами
- `docker-compose.yml` - убрана ссылка на ports.json

## Как это работает теперь

1. **JavaScript определяет API URL**: Функция `getApiBaseUrl()` в `api-config.js` определяет правильный API URL
2. **WebBaseApiService**: Использует JavaScript для получения полного API URL и строит полные URL для запросов
3. **WebAuthApiService**: Наследуется от WebBaseApiService и использует полные URL

## Инструкции по применению

### 1. Перезапустите контейнеры
```powershell
# Остановите все контейнеры
docker-compose down

# Пересоберите и запустите
docker-compose up --build -d
```

### 2. Проверьте статус
```powershell
docker-compose ps
```

### 3. Протестируйте изменения
```powershell
# Запустите тестовый скрипт
.\ai-temp\test-no-port-service.ps1 -ServerIP "YOUR_SERVER_IP"
```

### 4. Тестирование с другого компьютера
1. Откройте браузер на другом компьютере
2. Перейдите по адресу: `http://YOUR_SERVER_IP`
3. Попробуйте войти в систему
4. Проверьте консоль браузера (F12) - должны быть логи от `getApiBaseUrl()`
5. API запросы должны использовать полные URL (например, `http://192.168.1.100/api/auth/login`)

## Ожидаемый результат

- ✅ Нет ошибки `Not allowed to load local resource: file:///api/auth/login`
- ✅ API запросы используют полные URL
- ✅ Внешний доступ работает корректно
- ✅ Упрощенная архитектура без PortConfigurationService

## Отладка

Если проблемы остаются:
1. Откройте консоль браузера (F12)
2. Проверьте логи от `getApiBaseUrl()` - должен показывать правильный URL
3. Проверьте Network tab - API запросы должны идти на полные URL
4. Проверьте логи контейнеров:
   - `docker logs inventory-nginx-proxy`
   - `docker logs inventory-api`
   - `docker logs inventory-web`
