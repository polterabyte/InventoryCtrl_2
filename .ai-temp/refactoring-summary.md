# Рефакторинг: Удаление функции window.getApiBaseUrl

## Выполненные изменения

### 1. Созданы новые классы конфигурации
- `src/Inventory.Web.Client/Configuration/ApiConfiguration.cs` - конфигурация API настроек
- `src/Inventory.Web.Client/Configuration/ExternalAccessConfiguration.cs` - конфигурация внешнего доступа

### 2. Создан сервис для работы с API URL
- `src/Inventory.Web.Client/Services/ApiUrlService.cs` - сервис для получения API URL
- `src/Inventory.Web.Client/Services/IApiUrlService.cs` - интерфейс сервиса

### 3. Создан сервис для SignalR
- `src/Inventory.Web.Client/Services/SignalRService.cs` - сервис для работы с SignalR

### 4. Обновлены существующие сервисы
- `src/Inventory.Web.Client/Services/WebBaseApiService.cs` - теперь использует IApiUrlService вместо JS
- `src/Inventory.Web.Client/Services/WebAuthApiService.cs` - обновлен конструктор

### 5. Обновлена конфигурация
- `src/Inventory.Web.Client/Program.cs` - добавлена регистрация новых сервисов
- `src/Inventory.Web.Client/appsettings.json` - добавлены новые настройки

### 6. Обновлены компоненты
- `src/Inventory.UI/Components/RealTimeNotificationComponent.razor` - теперь использует SignalRService

### 7. Очищен JavaScript код
- `src/Inventory.Web.Client/wwwroot/js/api-config.js` - удалена функция getApiBaseUrl

## Преимущества нового подхода

1. **Типобезопасность** - использование C# вместо JavaScript для конфигурации
2. **Централизованная конфигурация** - все настройки API в одном месте
3. **Dependency Injection** - правильное использование DI контейнера
4. **Тестируемость** - сервисы можно легко мокать для тестов
5. **Конфигурируемость** - настройки можно изменять через appsettings.json
6. **Общепринятые практики** - соответствует стандартам ASP.NET Core

## Конфигурация

Теперь API URL настраивается через appsettings.json:

```json
{
  "ApiSettings": {
    "BaseUrl": "/api",
    "ExternalApiUrl": "",
    "UseExternalApi": false
  },
  "ExternalAccess": {
    "Enabled": true,
    "ApiPort": 5000,
    "ApiHttpsPort": 7000
  }
}
```

В development режиме система автоматически определяет URL на основе текущего хоста и использует HTTPS порт для API.
