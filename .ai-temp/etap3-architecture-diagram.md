# Диаграмма архитектуры Этапа 3

## Текущая архитектура (ПРОБЛЕМА)

```
┌─────────────────────────────────────────────────────────────┐
│                    Web Client                              │
├─────────────────────────────────────────────────────────────┤
│  CustomAuthenticationStateProvider                         │
│  ├─ GetAuthenticationStateAsync()                          │
│  ├─ MarkUserAsAuthenticatedAsync()                         │
│  └─ MarkUserAsLoggedOutAsync()                             │
├─────────────────────────────────────────────────────────────┤
│  WebBaseApiService                                         │
│  ├─ ExecuteHttpRequestAsync()                              │
│  ├─ GetAsync() / PostAsync() / PutAsync() / DeleteAsync()  │
│  └─ HandleStandardResponseAsync()                          │
├─────────────────────────────────────────────────────────────┤
│  ApiErrorHandler                                           │
│  ├─ HandleResponseAsync()                                  │
│  └─ HandleErrorResponseAsync()                             │
├─────────────────────────────────────────────────────────────┤
│  HttpClient                                                │
│  └─ DefaultRequestHeaders.Authorization                    │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                    API Server                              │
├─────────────────────────────────────────────────────────────┤
│  AuthenticationMiddleware                                  │
│  ├─ ValidateTokenAsync()                                   │
│  └─ HandleUnauthorized()                                   │
├─────────────────────────────────────────────────────────────┤
│  AuthController                                            │
│  ├─ /api/auth/login                                        │
│  ├─ /api/auth/refresh                                      │
│  └─ /api/auth/logout                                       │
└─────────────────────────────────────────────────────────────┘

❌ ПРОБЛЕМЫ:
- Нет проверки времени истечения токенов
- Нет автоматического обновления при 401
- Нет обработки refresh токенов
```

## Новая архитектура (РЕШЕНИЕ)

```
┌─────────────────────────────────────────────────────────────┐
│                    Web Client                              │
├─────────────────────────────────────────────────────────────┤
│  CustomAuthenticationStateProvider                         │
│  ├─ GetAuthenticationStateAsync() ← проверка времени       │
│  ├─ MarkUserAsAuthenticatedAsync()                         │
│  └─ MarkUserAsLoggedOutAsync()                             │
├─────────────────────────────────────────────────────────────┤
│  TokenManagementService                                    │
│  ├─ IsTokenExpiringSoonAsync()                             │
│  ├─ TryRefreshTokenAsync()                                 │
│  ├─ ClearTokensAsync()                                     │
│  └─ GetStoredTokenAsync()                                  │
├─────────────────────────────────────────────────────────────┤
│  JwtHttpInterceptor                                        │
│  ├─ InterceptAsync() ← перехват всех запросов              │
│  ├─ CheckAndRefreshTokenAsync()                            │
│  └─ Handle401ResponseAsync()                               │
├─────────────────────────────────────────────────────────────┤
│  WebBaseApiService                                         │
│  ├─ ExecuteHttpRequestAsync() ← через interceptor          │
│  ├─ GetAsync() / PostAsync() / PutAsync() / DeleteAsync()  │
│  └─ HandleStandardResponseAsync()                          │
├─────────────────────────────────────────────────────────────┤
│  ApiErrorHandler (улучшенный)                              │
│  ├─ HandleResponseAsync()                                  │
│  ├─ HandleErrorResponseAsync() ← обработка 401             │
│  └─ TryRefreshAndRetryAsync()                              │
├─────────────────────────────────────────────────────────────┤
│  HttpClient                                                │
│  └─ DefaultRequestHeaders.Authorization                    │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                    API Server                              │
├─────────────────────────────────────────────────────────────┤
│  AuthenticationMiddleware                                  │
│  ├─ ValidateTokenAsync()                                   │
│  └─ HandleUnauthorized()                                   │
├─────────────────────────────────────────────────────────────┤
│  AuthController                                            │
│  ├─ /api/auth/login                                        │
│  ├─ /api/auth/refresh ← используется для обновления       │
│  └─ /api/auth/logout                                       │
└─────────────────────────────────────────────────────────────┘

✅ РЕШЕНИЯ:
- Автоматическая проверка времени истечения
- Автоматическое обновление при 401
- Централизованное управление токенами
- Защита от дублирующих запросов
```

## Поток обработки запросов (НОВЫЙ)

```
1. HTTP Request
   ├─ JwtHttpInterceptor.InterceptAsync()
   ├─ TokenManagementService.IsTokenExpiringSoonAsync()
   │   ├─ Если токен скоро истечет → TryRefreshTokenAsync()
   │   └─ Если токен валиден → продолжить
   └─ Выполнить HTTP запрос

2. HTTP Response
   ├─ Если 200-299 → вернуть результат
   ├─ Если 401 → ApiErrorHandler.HandleErrorResponseAsync()
   │   ├─ TryRefreshTokenAsync()
   │   ├─ Если успешно → повторить запрос
   │   └─ Если неудачно → редирект на логин
   └─ Если другие ошибки → стандартная обработка

3. Token Refresh Flow
   ├─ Получить refresh токен из localStorage
   ├─ Отправить POST /api/auth/refresh
   ├─ Если успешно:
   │   ├─ Сохранить новый access токен
   │   ├─ Сохранить новый refresh токен
   │   └─ Обновить Authorization header
   └─ Если неудачно:
       ├─ Очистить все токены
       └─ Редирект на страницу входа
```

## Ключевые компоненты

### 1. TokenManagementService
- **Ответственность**: Управление жизненным циклом токенов
- **Методы**: Проверка времени, обновление, очистка
- **Безопасность**: Защита от дублирующих запросов

### 2. JwtHttpInterceptor
- **Ответственность**: Перехват всех HTTP запросов
- **Логика**: Проверка и обновление токенов перед запросами
- **Интеграция**: Работает с TokenManagementService

### 3. Улучшенный ApiErrorHandler
- **Ответственность**: Обработка 401 ошибок
- **Логика**: Попытка обновления токена при 401
- **Fallback**: Редирект на логин при неудаче

### 4. Обновленный CustomAuthenticationStateProvider
- **Ответственность**: Управление состоянием аутентификации
- **Интеграция**: Использует TokenManagementService
- **UX**: Прозрачное обновление токенов

## Преимущества новой архитектуры

1. **Автоматизация**: Пользователь не замечает обновления токенов
2. **Надежность**: Обработка всех сценариев истечения токенов
3. **Безопасность**: Ротация refresh токенов, защита от атак
4. **Производительность**: Минимальные накладные расходы
5. **Масштабируемость**: Легко добавлять новые функции
6. **Тестируемость**: Каждый компонент можно тестировать отдельно
