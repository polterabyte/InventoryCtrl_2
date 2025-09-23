# Пошаговый план устранения ошибок JWT токенов

## 🚨 НЕМЕДЛЕННЫЕ ДЕЙСТВИЯ (без изменения кода)

### Шаг 1: Очистить истекшие токены
```bash
# Выполнить в консоли браузера (F12 → Console)
localStorage.removeItem('authToken');
localStorage.removeItem('refreshToken');
location.reload();
```

### Шаг 2: Перезапустить веб-клиент
```bash
# Остановить и запустить контейнер веб-клиента
docker restart inventory-web-staging
```

**Результат**: Ошибки должны исчезнуть на время, пока токены снова не истекут.

---

## 🔧 КРАТКОСРОЧНЫЕ ИСПРАВЛЕНИЯ (требуют изменения кода)

### Шаг 3: Увеличить срок жизни JWT токенов

**Файл**: `src/Inventory.API/appsettings.json`
**Изменение**: 
```json
{
  "Jwt": {
    "ExpireMinutes": 60,  // Было: 15, Стало: 60
    "RefreshTokenExpireDays": 7
  }
}
```

**Результат**: Токены будут действовать 1 час вместо 15 минут.

### Шаг 4: Добавить автоматическое обновление токенов в клиенте

**Файл**: `src/Inventory.Web.Client/Services/WebBaseApiService.cs`
**Добавить метод**:
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

### Шаг 5: Обработать 401 ошибки в HTTP клиенте

**Файл**: `src/Inventory.Web.Client/Services/WebBaseApiService.cs`
**Модифицировать метод** `HandleStandardResponseAsync`:
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

---

## 🏗️ ДОЛГОСРОЧНЫЕ УЛУЧШЕНИЯ

### Шаг 6: Добавить проверку времени истечения токена

**Файл**: `src/Inventory.Web.Client/Services/WebBaseApiService.cs`
**Добавить метод**:
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

### Шаг 7: Интегрировать проверку в каждый запрос

**Модифицировать метод** `ExecuteHttpRequestAsync`:
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

### Шаг 8: Улучшить UX при истечении сессии

**Добавить уведомления пользователю**:
```csharp
private async Task RedirectToLoginAsync()
{
    // Показать уведомление пользователю
    var notificationService = _serviceProvider.GetRequiredService<IUINotificationService>();
    await notificationService.ShowErrorAsync("Сессия истекла. Пожалуйста, войдите снова.");
    
    // Очистить токены
    await _localStorage.RemoveItemAsync("authToken");
    await _localStorage.RemoveItemAsync("refreshToken");
    
    // Редирект на страницу входа
    _navigationManager.NavigateTo("/login");
}
```

---

## 📊 ОЖИДАЕМЫЕ РЕЗУЛЬТАТЫ

### После немедленных действий:
- ✅ Ошибки исчезнут из логов
- ✅ Пользователи смогут работать 1 час без прерываний

### После краткосрочных исправлений:
- ✅ Автоматическое обновление токенов
- ✅ Обработка 401 ошибок
- ✅ Улучшенный UX

### После долгосрочных улучшений:
- ✅ Полностью автоматическая система токенов
- ✅ Нет ошибок в логах
- ✅ Отличный пользовательский опыт
- ✅ Надежная система аутентификации

---

## ⚠️ ВАЖНЫЕ ЗАМЕЧАНИЯ

1. **Тестирование**: После каждого изменения тестировать в staging окружении
2. **Мониторинг**: Следить за логами после изменений
3. **Откат**: Подготовить план отката на случай проблем
4. **Безопасность**: Refresh токены должны быть безопасно храниться
5. **Производительность**: Не делать слишком частые проверки токенов
