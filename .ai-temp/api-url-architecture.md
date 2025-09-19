# Архитектура определения URL сервера в Blazor WebAssembly

## Обзор

Реализована улучшенная система определения URL сервера API, следующая лучшим практикам для Blazor WebAssembly приложений.

## Компоненты системы

### 1. ApiUrlService
**Основной сервис** для определения URL сервера с множественными fallback стратегиями.

**Приоритеты определения URL:**
1. Конфигурация по окружению из `appsettings.{Environment}.json`
2. Динамическое определение для development (через `window.location`)
3. Fallback к относительному пути `/api`

**Кэширование:** URL кэшируются после первого определения для повышения производительности.

### 2. ApiHealthService
**Сервис проверки здоровья** API с поддержкой:
- Проверки доступности API перед выполнением операций
- Health check endpoints (`/health`)
- Таймауты и обработка ошибок
- Подробное логирование

### 3. ResilientApiService
**Сервис устойчивости** с retry механизмом:
- Экспоненциальная задержка с jitter
- Настраиваемое количество попыток
- Health check перед выполнением операций
- Расширения для удобного использования

### 4. Конфигурация

#### appsettings.json (базовая конфигурация)
```json
{
    "ApiSettings": {
        "BaseUrl": "/api",
        "Environments": {
            "Development": {
                "ApiUrl": "https://localhost:7000/api",
                "SignalRUrl": "https://localhost:7000/notificationHub",
                "HealthCheckUrl": "https://localhost:7000/health"
            }
        },
        "Fallback": {
            "UseDynamicDetection": true,
            "DefaultPorts": {
                "Http": 5000,
                "Https": 7000
            },
            "RetrySettings": {
                "MaxRetries": 3,
                "BaseDelayMs": 1000,
                "MaxDelayMs": 10000
            }
        }
    }
}
```

#### appsettings.Production.json
```json
{
    "ApiSettings": {
        "Environments": {
            "Production": {
                "ApiUrl": "https://warehouse.cuby/api",
                "SignalRUrl": "https://warehouse.cuby/notificationHub",
                "HealthCheckUrl": "https://warehouse.cuby/health"
            }
        }
    }
}
```

#### appsettings.Staging.json
```json
{
    "ApiSettings": {
        "Environments": {
            "Staging": {
                "ApiUrl": "https://staging.warehouse.cuby/api",
                "SignalRUrl": "https://staging.warehouse.cuby/notificationHub",
                "HealthCheckUrl": "https://staging.warehouse.cuby/health"
            }
        }
    }
}
```

#### appsettings.Test.json
```json
{
    "ApiSettings": {
        "Environments": {
            "Test": {
                "ApiUrl": "https://test.warehouse.cuby/api",
                "SignalRUrl": "https://test.warehouse.cuby/notificationHub",
                "HealthCheckUrl": "https://test.warehouse.cuby/health"
            }
        }
    }
}
```

## Преимущества новой архитектуры

### 1. **Централизованная конфигурация**
- Все URL настраиваются в конфигурационных файлах
- Поддержка разных окружений (Development, Staging, Production)
- Легкое изменение URL без перекомпиляции

### 2. **Множественные fallback стратегии**
- Если конфигурация не задана → динамическое определение
- Если динамическое определение не работает → относительный путь
- Гарантированная работоспособность в любых условиях

### 3. **Устойчивость к сбоям**
- Retry механизм с экспоненциальной задержкой
- Health checks перед выполнением операций
- Автоматическое восстановление при временных сбоях

### 4. **Производительность**
- Кэширование URL после первого определения
- Минимальное количество обращений к JavaScript
- Оптимизированная логика определения URL

### 5. **Отладка и мониторинг**
- Подробное логирование всех операций
- Информация о используемых URL
- Health check результаты

## Использование

### В API сервисах
```csharp
public class ProductApiService : WebBaseApiService
{
    public ProductApiService(
        HttpClient httpClient, 
        IApiUrlService apiUrlService, 
        IResilientApiService resilientApiService,
        ILogger<ProductApiService> logger) 
        : base(httpClient, apiUrlService, resilientApiService, logger)
    {
    }

    // Все HTTP методы автоматически используют retry механизм
    public async Task<ApiResponse<List<Product>>> GetProductsAsync()
    {
        return await GetAsync<List<Product>>("/products");
    }
}
```

### Проверка здоровья API
```csharp
public class SomeService
{
    private readonly IApiHealthService _healthService;

    public async Task<bool> IsApiReadyAsync()
    {
        return await _healthService.IsApiAvailableAsync();
    }
}
```

### Получение информации об URL
```csharp
public class DebugService
{
    private readonly IApiUrlService _apiUrlService;

    public async Task<ApiUrlInfo> GetUrlInfoAsync()
    {
        return await _apiUrlService.GetApiUrlInfoAsync();
    }
}
```

## Миграция с предыдущей версии

1. **Удалены устаревшие конфигурации:**
   - `ExternalAccessConfiguration`
   - `ExternalApiUrl` из `ApiConfiguration`

2. **Упрощен JavaScript:**
   - Убрана дублирующая логика определения URL
   - Оставлен только fallback для критических случаев

3. **Обновлены сервисы:**
   - Все API сервисы теперь используют `ResilientApiService`
   - Автоматический retry для всех HTTP операций
   - Health checks перед выполнением операций

## Рекомендации по настройке

### Development
- Используйте динамическое определение URL
- Настройте правильные порты в `DefaultPorts`
- Включите подробное логирование

### Production (warehouse.cuby)
- Основной домен: `https://warehouse.cuby`
- API endpoint: `https://warehouse.cuby/api`
- SignalR hub: `https://warehouse.cuby/notificationHub`
- Health check: `https://warehouse.cuby/health`
- Настройте правильные CORS политики для домена
- Мониторьте health check endpoints

### Staging (staging.warehouse.cuby)
- Тестовый домен: `https://staging.warehouse.cuby`
- Используйте отдельные URL для тестирования
- Настройте retry параметры для тестирования устойчивости
- Включите детальное логирование для отладки

### Test (test.warehouse.cuby)
- Тестовый домен: `https://test.warehouse.cuby`
- Для автоматизированного тестирования
- Изолированная среда для QA
