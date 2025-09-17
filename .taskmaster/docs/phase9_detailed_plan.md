# Детальный план Фазы 9 - Улучшение безопасности и аутентификации

## 📋 Обзор текущего состояния

### ✅ Уже реализовано:
- JWT аутентификация с токенами
- Базовая модель AuditLog
- Ролевая система (Admin, User, Manager)
- AuthController с login/register/refresh/logout endpoints

### ❌ Требует доработки:
- Refresh Token система (сейчас работает как re-authentication)
- Расширенный аудит для всех операций
- Rate Limiting для API
- Улучшенная валидация данных

## 🎯 Задача 1: JWT Refresh Token система

### 1.1 Обновление модели User
**Файл**: `src/Inventory.API/Models/User.cs`
**Действия**:
- Добавить поле `RefreshToken` (string?)
- Добавить поле `RefreshTokenExpiry` (DateTime?)
- Создать миграцию для новых полей

**Код**:
```csharp
public class User : Microsoft.AspNetCore.Identity.IdentityUser<string>
{
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiry { get; set; }
    public ICollection<InventoryTransaction> Transactions { get; set; } = new List<InventoryTransaction>();
    public ICollection<ProductHistory> ProductHistories { get; set; } = new List<ProductHistory>();
}
```

### 1.2 Создание RefreshTokenService
**Файл**: `src/Inventory.API/Services/RefreshTokenService.cs`
**Функциональность**:
- Генерация refresh токенов
- Валидация refresh токенов
- Отзыв refresh токенов
- Очистка истекших токенов

### 1.3 Обновление AuthController
**Файл**: `src/Inventory.API/Controllers/AuthController.cs`
**Изменения**:
- Обновить метод `Login` для генерации refresh токена
- Реализовать правильную логику в методе `Refresh`
- Обновить метод `Logout` для отзыва refresh токена
- Добавить метод `RevokeToken` для принудительного отзыва

### 1.4 Обновление DTOs
**Файл**: `src/Inventory.Shared/DTOs/`
**Новые DTOs**:
- `RefreshTokenRequest` - для запроса обновления токена
- `RefreshTokenResponse` - для ответа с новым токеном
- Обновить `LoginResult` для включения refresh токена

### 1.5 Обновление конфигурации
**Файл**: `src/Inventory.API/appsettings.json`
**Добавить**:
```json
{
  "Jwt": {
    "Key": "...",
    "Issuer": "...",
    "Audience": "...",
    "ExpireMinutes": 15,
    "RefreshTokenExpireDays": 7
  }
}
```

## 🎯 Задача 2: Расширенный аудит действий

### 2.1 Расширение модели AuditLog
**Файл**: `src/Inventory.API/Models/AuditLog.cs`
**Добавить поля**:
- `ActionType` (enum: Create, Read, Update, Delete)
- `EntityType` (string) - тип сущности
- `Changes` (JSON) - детальные изменения
- `RequestId` (string) - ID запроса для трассировки

### 2.2 Создание AuditService
**Файл**: `src/Inventory.API/Services/AuditService.cs`
**Функциональность**:
- Логирование всех CRUD операций
- Создание детальных записей изменений
- Фильтрация и поиск аудит-логов
- Интеграция с Entity Framework Change Tracking

### 2.3 Создание AuditController
**Файл**: `src/Inventory.API/Controllers/AuditController.cs`
**Endpoints**:
- `GET /api/audit` - получение аудит-логов с фильтрацией
- `GET /api/audit/{entityType}/{entityId}` - аудит конкретной сущности
- `GET /api/audit/user/{userId}` - аудит действий пользователя

### 2.4 Создание AuditMiddleware
**Файл**: `src/Inventory.API/Middleware/AuditMiddleware.cs`
**Функциональность**:
- Автоматическое логирование HTTP запросов
- Извлечение информации о пользователе
- Связывание запросов с аудит-записями

### 2.5 Обновление существующих контроллеров
**Файлы**: Все контроллеры в `src/Inventory.API/Controllers/`
**Добавить**:
- Интеграцию с AuditService
- Логирование всех операций
- Детальное отслеживание изменений

## 🎯 Задача 3: Rate Limiting и валидация

### 3.1 Настройка Rate Limiting
**Файл**: `src/Inventory.API/Program.cs`
**Добавить**:
- ASP.NET Core Rate Limiting middleware
- Разные лимиты для разных ролей
- Конфигурация в appsettings.json

### 3.2 Улучшение валидации
**Файл**: `src/Inventory.Shared/Validators/`
**Создать**:
- FluentValidation валидаторы для всех DTOs
- Кастомные валидационные атрибуты
- Централизованная обработка ошибок валидации

### 3.3 CORS политики по ролям
**Файл**: `src/Inventory.API/Program.cs`
**Настроить**:
- Разные CORS политики для разных ролей
- Безопасные настройки для production

### 3.4 CSRF защита
**Файл**: `src/Inventory.API/Program.cs`
**Добавить**:
- Anti-forgery токены
- CSRF middleware

## 🎯 Задача 4: Frontend обновления

### 4.1 Обновление AuthenticationService
**Файл**: `src/Inventory.Web.Client/Services/AuthenticationService.cs`
**Функциональность**:
- Автоматическое обновление токенов
- Обработка истечения refresh токенов
- Retry логика для failed запросов

### 4.2 Создание AuditLogs страницы
**Файл**: `src/Inventory.UI/Pages/AuditLogs.razor`
**Функциональность**:
- Просмотр аудит-логов
- Фильтрация по пользователю, дате, действию
- Экспорт в CSV/Excel

### 4.3 Обновление UI компонентов
**Файлы**: Компоненты в `src/Inventory.UI/Components/`
**Добавить**:
- Индикаторы загрузки для аудит-операций
- Уведомления об ошибках валидации
- Улучшенная обработка ошибок

## 🎯 Задача 5: Тестирование

### 5.1 Unit тесты
**Файлы**: `test/Inventory.UnitTests/`
**Создать тесты для**:
- RefreshTokenService
- AuditService
- Rate Limiting middleware
- Валидаторы

### 5.2 Integration тесты
**Файлы**: `test/Inventory.IntegrationTests/`
**Создать тесты для**:
- JWT refresh token flow
- Audit logging endpoints
- Rate limiting behavior
- CORS policies

### 5.3 Component тесты
**Файлы**: `test/Inventory.ComponentTests/`
**Создать тесты для**:
- AuditLogs страница
- Обновленные UI компоненты
- Authentication flow

## 📅 Временной план

### Неделя 1: JWT Refresh Token система
- **День 1-2**: Обновление модели User и создание миграции
- **День 3-4**: Создание RefreshTokenService
- **День 5**: Обновление AuthController и DTOs
- **День 6-7**: Тестирование и отладка

### Неделя 2: Расширенный аудит
- **День 1-2**: Расширение AuditLog модели и создание AuditService
- **День 3-4**: Создание AuditController и AuditMiddleware
- **День 5**: Обновление существующих контроллеров
- **День 6-7**: Frontend для просмотра аудит-логов

### Неделя 3: Rate Limiting и валидация
- **День 1-2**: Настройка Rate Limiting
- **День 3-4**: Улучшение валидации с FluentValidation
- **День 5**: CORS политики и CSRF защита
- **День 6-7**: Тестирование и документация

## 🔧 Технические детали

### Зависимости для добавления
```xml
<PackageReference Include="Microsoft.AspNetCore.RateLimiting" Version="9.0.0" />
<PackageReference Include="FluentValidation.AspNetCore" Version="11.3.0" />
<PackageReference Include="FluentValidation.DependencyInjectionExtensions" Version="11.3.0" />
```

### Конфигурация appsettings.json
```json
{
  "RateLimiting": {
    "DefaultPolicy": {
      "PermitLimit": 100,
      "Window": "00:01:00"
    },
    "AdminPolicy": {
      "PermitLimit": 1000,
      "Window": "00:01:00"
    }
  },
  "Jwt": {
    "RefreshTokenExpireDays": 7
  }
}
```

## ✅ Критерии готовности

### JWT Refresh Token система
- [x] Refresh токены генерируются при логине ✅
- [x] Refresh endpoint работает корректно ✅
- [x] Токены можно отозвать при logout ✅
- [ ] Автоматическое обновление на клиенте (требует frontend обновления)

### Расширенный аудит
- [ ] Все CRUD операции логируются
- [ ] Аудит-логи доступны через API
- [ ] Frontend страница для просмотра логов
- [ ] Детальная информация об изменениях

### Rate Limiting и валидация
- [ ] API защищен от злоупотреблений
- [ ] Валидация работает с понятными ошибками
- [ ] CORS настроен безопасно
- [ ] CSRF защита активна

### Тестирование
- [ ] Все новые функции покрыты тестами
- [ ] Integration тесты проходят
- [ ] Component тесты работают
- [ ] Покрытие тестами > 80%

## 🧪 Результаты тестирования JWT Refresh Token системы

### ✅ Успешно протестировано:

1. **Регистрация пользователя**:
   ```
   POST /api/auth/register
   Status: 201 Created
   Response: {"success":true,"data":{"message":"User created successfully"}}
   ```

2. **Логин с получением токенов**:
   ```
   POST /api/auth/login
   Status: 200 OK
   Response: {
     "success": true,
     "data": {
       "token": "JWT access token (15 минут)",
       "refreshToken": "Base64 refresh token (7 дней)",
       "expiresAt": "2025-09-17T09:26:57.6576037Z",
       "username": "testuser",
       "email": "test@example.com",
       "role": "User"
     }
   }
   ```

3. **Refresh токенов**:
   ```
   POST /api/auth/refresh
   Status: 200 OK
   Response: Новые access и refresh токены
   Логи: "Token refreshed successfully for user testuser"
   ```

4. **Logout с отзывом токена**:
   ```
   POST /api/auth/logout
   Status: 200 OK
   Response: {"success":true,"data":{"message":"Logged out successfully"}}
   ```

### 📊 Анализ логов:

Из логов видно корректную работу системы:
- Refresh токены устанавливаются с правильным сроком действия (7 дней)
- Валидация refresh токенов работает корректно
- Новые токены генерируются при каждом refresh
- Система логирует все операции для отладки

### 🔍 Обнаруженные особенности:

1. **JWT Token Validation**: В логах видно "JWT Token validated for user: null" - это нормально для logout операции
2. **Refresh Token Rotation**: Система корректно генерирует новые refresh токены при каждом обновлении
3. **Security**: Refresh токены используют криптографически стойкую генерацию (RandomNumberGenerator)

## 🚀 Следующие шаги

### ✅ Завершено:
1. **Обновление модели User** ✅
2. **Создание миграции** ✅
3. **Реализация RefreshTokenService** ✅
4. **Обновление AuthController** ✅
5. **Тестирование JWT refresh flow** ✅

### 🔄 В процессе:
6. **Обновление AuthenticationService на клиенте** (frontend)

### 📋 Следующие задачи:
7. **Расширение системы аудита** - приоритет 1
8. **Rate Limiting и валидация** - приоритет 2
9. **Тестирование безопасности** - приоритет 3
10. **Обновление документации** - приоритет 4

**Готов продолжить с системой аудита?**
