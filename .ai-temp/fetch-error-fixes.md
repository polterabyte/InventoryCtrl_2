# Исправления ошибки "Failed to fetch"

## Проблема
В консоли браузера возникала ошибка:
```
Inventory.Web.Client.Services.ApiErrorHandler[0]
Exception in GET /dashboard/low-stock-products
System.Net.Http.HttpRequestException: TypeError: Failed to fetch
```

## Причины
1. **CORS настройки** не включали staging.warehouse.cuby
2. **URL построение** использовало HTTP вместо HTTPS для staging
3. **Fallback URL** в UrlBuilderService был неправильным
4. **Недостаточное логирование** для диагностики проблем

## Исправления

### 1. CORS настройки (ServiceCollectionExtensions.cs)
```csharp
// Добавлены staging и production origins
"https://staging.warehouse.cuby",
"https://warehouse.cuby",
```

### 2. URL построение (UrlBuilderService.cs)
```csharp
// Исправлен fallback URL на HTTPS
var fallbackUrl = $"https://staging.warehouse.cuby{url}";
```

### 3. API URL Service (ApiUrlService.cs)
```csharp
// Улучшена обработка staging окружения
if (_environment.Environment == "Staging")
{
    return $"https://staging.warehouse.cuby{url}";
}
```

### 4. Staging настройки (appsettings.Staging.json)
```json
"Staging": {
    "ApiUrl": "https://staging.warehouse.cuby/api",
    "SignalRUrl": "https://staging.warehouse.cuby/notificationHub",
    "HealthCheckUrl": "https://staging.warehouse.cuby/health"
}
```

### 5. Улучшенное логирование (WebBaseApiService.cs)
- Добавлено детальное логирование HTTP запросов
- Улучшена обработка ошибок
- Добавлена обработка timeout'ов

## Результат
- ✅ CORS правильно настроен для всех окружений
- ✅ URL построение использует HTTPS для staging/production
- ✅ Улучшена диагностика проблем
- ✅ Исправлены все предупреждения линтера

## Тестирование
После деплоя проверьте:
1. Отсутствие ошибок CORS в консоли браузера
2. Успешные запросы к API endpoints
3. Правильное построение URL для staging окружения
