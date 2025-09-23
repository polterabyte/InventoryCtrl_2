# Финальный анализ ошибок JWT токенов в Docker API

## 📋 РЕЗЮМЕ ПРОБЛЕМЫ

**Основная проблема**: Клиентское приложение использует истекшие JWT токены, что приводит к постоянным ошибкам 401 в логах API.

**Корень проблемы**: 
- JWT токены истекают через 15 минут
- Клиент не обновляет токены автоматически
- Отсутствует обработка 401 ошибок для refresh токенов

## 🔍 ДЕТАЛЬНЫЙ АНАЛИЗ

### 1. Текущая архитектура (РАБОТАЕТ НЕПРАВИЛЬНО)
```
Web Client → API Server
    │           │
    │           ├─ JWT Token (15 min) ❌ Слишком короткий
    │           ├─ Refresh Token (7 days) ✅ Хорошо
    │           └─ 401 при истечении ✅ Правильно
    │
    ├─ Сохраняет токены ✅
    ├─ НЕ проверяет время истечения ❌
    ├─ НЕ обновляет токены ❌
    └─ НЕ обрабатывает 401 ❌
```

### 2. Обнаруженные ошибки в логах
- **Тип**: `SecurityTokenExpiredException`
- **Код**: `IDX10223: Lifetime validation failed`
- **Частота**: Каждые несколько секунд
- **Затронутые эндпоинты**: `/api/dashboard/*`

### 3. Существующая инфраструктура
✅ **API сторона готова**:
- `RefreshTokenService.cs` - полная реализация
- `AuthController.cs` - endpoint `/api/auth/refresh`
- Валидация токенов работает правильно

❌ **Клиентская сторона не готова**:
- Нет автоматического обновления токенов
- Нет обработки 401 ошибок
- Нет проверки времени истечения

## 🚀 ПЛАН УСТРАНЕНИЯ

### ЭТАП 1: Немедленные действия (0 минут)
```bash
# 1. Очистить токены в браузере
localStorage.removeItem('authToken');
localStorage.removeItem('refreshToken');
location.reload();

# 2. Перезапустить веб-клиент
docker restart inventory-web-staging
```

### ЭТАП 2: Быстрое исправление (15 минут)
**Изменить конфигурацию JWT**:
```json
// src/Inventory.API/appsettings.json
{
  "Jwt": {
    "ExpireMinutes": 60,  // Было: 15, Стало: 60
    "RefreshTokenExpireDays": 7
  }
}
```

**Результат**: Токены будут действовать 1 час вместо 15 минут.

### ЭТАП 3: Полное решение (2-3 часа)

#### 3.1. Добавить проверку времени истечения токена
**Файл**: `src/Inventory.Web.Client/Services/WebBaseApiService.cs`

```csharp
private async Task<bool> IsTokenExpiringSoonAsync()
{
    var token = await _localStorage.GetItemAsStringAsync("authToken");
    if (string.IsNullOrEmpty(token)) return true;
    
    try
    {
        var payload = token.Split('.')[1];
        var jsonBytes = Convert.FromBase64String(payload);
        var claims = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);
        
        if (claims?.ContainsKey("exp") == true)
        {
            var exp = long.Parse(claims["exp"].ToString()!);
            var expTime = DateTimeOffset.FromUnixTimeSeconds(exp);
            var timeUntilExpiry = expTime - DateTimeOffset.UtcNow;
            
            // Если токен истечет в течение 5 минут
            return timeUntilExpiry.TotalMinutes < 5;
        }
    }
    catch
    {
        return true; // Если не можем распарсить, считаем истекшим
    }
    
    return true;
}
```

#### 3.2. Добавить автоматическое обновление токенов
```csharp
private async Task<bool> TryRefreshTokenAsync()
{
    try
    {
        var refreshToken = await _localStorage.GetItemAsStringAsync("refreshToken");
        if (string.IsNullOrEmpty(refreshToken)) return false;
        
        var authService = _serviceProvider.GetRequiredService<IAuthService>();
        var result = await authService.RefreshTokenAsync(refreshToken);
        
        if (result.Success)
        {
            await _localStorage.SetItemAsStringAsync("authToken", result.Token);
            await _localStorage.SetItemAsStringAsync("refreshToken", result.RefreshToken);
            _httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", result.Token);
            return true;
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to refresh token");
    }
    return false;
}
```

#### 3.3. Обработать 401 ошибки
```csharp
private async Task<T> HandleStandardResponseAsync<T>(HttpResponseMessage response)
{
    // Если получили 401, попробуем обновить токен
    if (response.StatusCode == HttpStatusCode.Unauthorized)
    {
        if (await TryRefreshTokenAsync())
        {
            // Повторяем запрос с новым токеном
            return await ExecuteHttpRequestAsync<T>(/* оригинальные параметры */);
        }
        else
        {
            // Если не удалось обновить, редиректим на логин
            await RedirectToLoginAsync();
        }
    }
    
    // Остальная логика обработки ответа...
}
```

#### 3.4. Интегрировать в каждый запрос
```csharp
protected async Task<T> ExecuteHttpRequestAsync<T>(/* параметры */)
{
    // Проверяем, не истекает ли токен
    if (await IsTokenExpiringSoonAsync())
    {
        await TryRefreshTokenAsync();
    }
    
    // Выполняем оригинальный запрос...
}
```

## 📊 ОЖИДАЕМЫЕ РЕЗУЛЬТАТЫ

### После ЭТАПА 1 (немедленно):
- ✅ Ошибки исчезнут из логов
- ✅ Пользователи смогут работать

### После ЭТАПА 2 (15 минут):
- ✅ Токены действуют 1 час
- ✅ Меньше прерываний сессии

### После ЭТАПА 3 (2-3 часа):
- ✅ Полностью автоматическая система токенов
- ✅ Нет ошибок в логах
- ✅ Отличный пользовательский опыт
- ✅ Надежная система аутентификации

## ⚠️ ВАЖНЫЕ ЗАМЕЧАНИЯ

1. **Тестирование**: Тестировать каждый этап в staging окружении
2. **Мониторинг**: Следить за логами после каждого изменения
3. **Безопасность**: Refresh токены уже безопасно хранятся в базе данных
4. **Производительность**: Проверка токенов будет происходить только при необходимости
5. **Откат**: Если что-то пойдет не так, можно откатить изменения конфигурации

## 🎯 ПРИОРИТЕТЫ

1. **ВЫСОКИЙ**: ЭТАП 1 - немедленно очистить токены
2. **ВЫСОКИЙ**: ЭТАП 2 - увеличить время жизни токенов
3. **СРЕДНИЙ**: ЭТАП 3 - полная автоматизация (можно отложить)

## 📝 ЗАКЛЮЧЕНИЕ

Проблема решаема и не требует кардинальных изменений архитектуры. API сторона уже готова, нужно только доработать клиентскую часть. После реализации всех этапов система будет работать стабильно без ошибок в логах.
