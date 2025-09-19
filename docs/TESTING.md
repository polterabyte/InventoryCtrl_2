# Testing Guide

Комплексное руководство по тестированию системы управления инвентарем.

## 🎯 Стратегия тестирования

### Философия тестирования
- **Real PostgreSQL** — используется реальная СУБД вместо InMemory для Integration Tests
- **Unique Test Databases** — каждый тест получает уникальную базу данных
- **Automatic Cleanup** — тестовые БД автоматически удаляются после тестов
- **Complete Isolation** — полная изоляция тестовых данных
- **Real-time Testing** — тестирование SignalR и Notification System
- **Security Testing** — тестирование Rate Limiting, JWT, Audit System
- **Performance Testing** — мониторинг производительности и времени выполнения

### Результаты тестирования
- ✅ **120+ тестов** — все проходят успешно
- ✅ **100% успешность** — 0 ошибок
- ✅ **Автоматическая очистка** — тестовые БД удаляются автоматически
- ✅ **Изоляция тестов** — каждый тест использует свою БД
- ✅ **Real-time Features** — тестирование SignalR и Notification System
- ✅ **Security Features** — тестирование Rate Limiting и Audit System

## 🏗 Типы тестов

### Unit Tests (79+ тестов)
**Назначение**: Тестирование бизнес-логики, моделей и сервисов

**Технологии**:
- xUnit — фреймворк тестирования
- Moq — мокирование зависимостей
- FluentAssertions — читаемые утверждения
- Microsoft.AspNetCore.Http — тестирование HTTP контекста

**Структура**:
```
test/Inventory.UnitTests/
├── Services/           # Тесты сервисов (AuditService, AuthService, NotificationService)
├── Models/             # Тесты моделей (Category, Product, Warehouse)
├── Controllers/        # Тесты контроллеров (Category, Product, Dashboard)
└── Validators/         # Тесты валидаторов (FluentValidation)
```

**Новые возможности**:
- **AuditService Tests** — тестирование системы аудита
- **NotificationService Tests** — тестирование системы уведомлений
- **AuthService Tests** — тестирование JWT и refresh токенов
- **PortConfigurationService Tests** — тестирование конфигурации портов

### Integration Tests (29+ тестов)
**Назначение**: Тестирование API endpoints с реальной PostgreSQL БД

**Технологии**:
- xUnit — фреймворк тестирования
- Microsoft.AspNetCore.Mvc.Testing — тестирование API
- PostgreSQL — реальная СУБД
- Entity Framework Core — ORM
- FluentAssertions — читаемые утверждения
- Docker — контейнеризация PostgreSQL
- Microsoft.AspNetCore.SignalR.Testing — тестирование SignalR

**Особенности**:
- Уникальная БД для каждого теста
- Автоматическое создание и удаление БД
- Полная изоляция тестовых данных
- Тестирование реальных SQL запросов
- Тестирование SignalR Hub
- Тестирование Rate Limiting

**Структура**:
```
test/Inventory.IntegrationTests/
├── Controllers/        # Integration тесты API (Auth, Category, Dashboard, Audit)
├── Middleware/         # Тесты middleware (Rate Limiting, Audit)
├── Database/           # Тесты БД
└── SignalR/            # Тесты SignalR Hub
```

**Новые возможности**:
- **AuditController Tests** — тестирование endpoints аудита
- **SignalR Tests** — тестирование real-time коммуникации
- **Rate Limiting Tests** — тестирование ограничений по ролям
- **Security Tests** — тестирование JWT и refresh токенов

### Component Tests (12+ тестов)
**Назначение**: Тестирование Blazor компонентов и UI

**Технологии**:
- xUnit — фреймворк тестирования
- bUnit — тестирование Blazor компонентов
- FluentAssertions — читаемые утверждения
- Moq — мокирование сервисов
- Microsoft.AspNetCore.SignalR.Client — тестирование SignalR клиента

**Структура**:
```
test/Inventory.ComponentTests/
├── Components/         # Тесты Blazor компонентов
│   ├── Admin/          # Тесты админских компонентов
│   ├── Dashboard/      # Тесты компонентов дашборда
│   └── Notifications/  # Тесты уведомлений
├── Pages/              # Тесты страниц
└── Layout/             # Тесты макетов
```

**Новые возможности**:
- **ToastNotification Tests** — тестирование toast уведомлений
- **NotificationCenter Tests** — тестирование центра уведомлений
- **SignalR Component Tests** — тестирование real-time компонентов
- **Admin Component Tests** — тестирование админских виджетов
- **Dashboard Tests** — тестирование виджетов дашборда

## 🗄️ Database Testing Strategy

### Почему PostgreSQL вместо InMemory?

| Аспект | InMemory | PostgreSQL |
|--------|----------|------------|
| **Реализм** | ❌ Упрощенная модель | ✅ Реальная СУБД |
| **SQL Features** | ❌ Ограниченная поддержка | ✅ Полная поддержка PostgreSQL |
| **Migrations** | ❌ Не тестируются | ✅ Полное тестирование миграций |
| **Performance** | ❌ Нереалистичная | ✅ Реальная производительность |
| **Constraints** | ❌ Базовые | ✅ Foreign Keys, Check Constraints |
| **Transactions** | ❌ Упрощенные | ✅ ACID свойства |

### Стратегия изоляции БД

#### 1. Уникальные тестовые базы данных
```csharp
// Каждый тест получает уникальную базу данных
TestDatabaseName = $"inventory_test_{Guid.NewGuid():N}_{DateTime.UtcNow:yyyyMMddHHmmss}";

// Примеры:
// inventory_test_a1b2c3d4e5f6_20250914120000
// inventory_test_f6e5d4c3b2a1_20250914120001
```

#### 2. Автоматическая очистка
```csharp
// После каждого теста база данных автоматически удаляется
public void Dispose()
{
    Context.Database.EnsureDeleted();
    CleanupTestDatabase(); // Удаление из PostgreSQL
}
```

#### 3. Изоляция тестовых данных
```csharp
// Очистка данных между тестами
protected async Task CleanupDatabaseAsync()
{
    // Удаление всех данных в правильном порядке
    Context.InventoryTransactions.RemoveRange(Context.InventoryTransactions);
    Context.Products.RemoveRange(Context.Products);
    // ... остальные таблицы
    
    // Очистка Identity таблиц
    await Context.Database.ExecuteSqlRawAsync("DELETE FROM \"AspNetUserRoles\"");
    await Context.Database.ExecuteSqlRawAsync("DELETE FROM \"AspNetRoles\"");
    await Context.Database.ExecuteSqlRawAsync("DELETE FROM \"AspNetUsers\"");
}
```

## 🔔 Testing SignalR & Notifications

### SignalR Testing Strategy

#### Unit Tests для SignalR
```csharp
[Fact]
public async Task NotificationHub_OnConnected_ShouldAddUserToGroup()
{
    // Arrange
    var hub = new NotificationHub();
    var context = CreateMockHubCallerContext();
    var groups = new Mock<IGroupManager>();
    
    // Act
    await hub.OnConnectedAsync();
    
    // Assert
    groups.Verify(g => g.AddToGroupAsync(
        context.ConnectionId, 
        $"user_{context.User.Identity.Name}", 
        CancellationToken.None), Times.Once);
}

[Fact]
public async Task NotificationHub_SendNotification_ShouldInvokeClient()
{
    // Arrange
    var hub = new NotificationHub();
    var clients = new Mock<IHubCallerClients>();
    var clientProxy = new Mock<IClientProxy>();
    
    clients.Setup(c => c.User("test-user")).Returns(clientProxy.Object);
    
    // Act
    await hub.SendNotificationToUser("test-user", "Test notification");
    
    // Assert
    clientProxy.Verify(c => c.SendCoreAsync(
        "ReceiveNotification", 
        It.IsAny<object[]>(), 
        CancellationToken.None), Times.Once);
}
```

#### Integration Tests для SignalR
```csharp
[Fact]
public async Task SignalR_Connection_ShouldEstablishSuccessfully()
{
    // Arrange
    var connection = new HubConnectionBuilder()
        .WithUrl("http://localhost/notificationHub", options =>
        {
            options.AccessTokenProvider = () => Task.FromResult("test-token");
        })
        .Build();
    
    var notificationReceived = false;
    connection.On<string, string>("ReceiveNotification", (title, message) =>
    {
        notificationReceived = true;
    });
    
    // Act
    await connection.StartAsync();
    
    // Simulate sending notification
    await connection.InvokeAsync("SendNotification", "Test", "Test message");
    
    // Wait for notification
    await Task.Delay(100);
    
    // Assert
    Assert.True(notificationReceived);
    await connection.DisposeAsync();
}
```

### Notification System Testing

#### Toast Notification Component Tests
```csharp
[Fact]
public void ToastNotification_WithSuccessType_ShouldRenderSuccessStyles()
{
    // Arrange
    var notification = new Notification
    {
        Id = Guid.NewGuid().ToString(),
        Title = "Success",
        Message = "Operation completed",
        Type = NotificationType.Success,
        Duration = 5000
    };
    
    // Act
    var cut = RenderComponent<ToastNotification>(parameters => 
        parameters.Add(p => p.Notification, notification));
    
    // Assert
    cut.Find(".toast-notification").ClassList.Should().Contain("toast-success");
    cut.Find(".toast-title").TextContent.Should().Be("Success");
    cut.Find(".toast-message").TextContent.Should().Be("Operation completed");
}

[Fact]
public async Task ToastNotification_WithRetryAction_ShouldExecuteRetry()
{
    // Arrange
    var retryExecuted = false;
    var notification = new Notification
    {
        Id = Guid.NewGuid().ToString(),
        Title = "Error",
        Message = "Network error",
        Type = NotificationType.Error,
        OnRetry = () => retryExecuted = true
    };
    
    // Act
    var cut = RenderComponent<ToastNotification>(parameters => 
        parameters.Add(p => p.Notification, notification));
    
    cut.Find(".btn-retry").Click();
    await Task.Delay(100);
    
    // Assert
    retryExecuted.Should().BeTrue();
}
```

#### Notification Service Tests
```csharp
[Fact]
public async Task NotificationService_ShowSuccess_ShouldCreateSuccessNotification()
{
    // Arrange
    var service = new NotificationService();
    var notificationCreated = false;
    
    service.OnNotificationCreated += (notification) =>
    {
        notificationCreated = true;
        notification.Type.Should().Be(NotificationType.Success);
        notification.Title.Should().Be("Success");
        notification.Message.Should().Be("Test message");
    };
    
    // Act
    service.ShowSuccess("Success", "Test message");
    
    // Assert
    notificationCreated.Should().BeTrue();
}
```

## 🔒 Testing Security & Rate Limiting

### Rate Limiting Tests

#### Unit Tests для Rate Limiting
```csharp
[Fact]
public async Task RateLimiter_AdminUser_ShouldAllow1000Requests()
{
    // Arrange
    var limiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
    {
        TokenLimit = 1000,
        ReplenishmentPeriod = TimeSpan.FromMinutes(1),
        TokensPerPeriod = 1000,
        AutoReplenishment = true
    });
    
    // Act & Assert
    for (int i = 0; i < 1000; i++)
    {
        var lease = await limiter.AcquireAsync(1, CancellationToken.None);
        lease.IsAcquired.Should().BeTrue();
    }
    
    // 1001st request should be rate limited
    var finalLease = await limiter.AcquireAsync(1, CancellationToken.None);
    finalLease.IsAcquired.Should().BeFalse();
}

[Fact]
public async Task RateLimiter_RegularUser_ShouldAllow100Requests()
{
    // Arrange
    var limiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
    {
        TokenLimit = 100,
        ReplenishmentPeriod = TimeSpan.FromMinutes(1),
        TokensPerPeriod = 100,
        AutoReplenishment = true
    });
    
    // Act & Assert
    for (int i = 0; i < 100; i++)
    {
        var lease = await limiter.AcquireAsync(1, CancellationToken.None);
        lease.IsAcquired.Should().BeTrue();
    }
    
    // 101st request should be rate limited
    var finalLease = await limiter.AcquireAsync(1, CancellationToken.None);
    finalLease.IsAcquired.Should().BeFalse();
}
```

#### Integration Tests для Rate Limiting
```csharp
[Fact]
public async Task ApiEndpoint_WithRateLimit_ShouldReturn429AfterLimit()
{
    // Arrange
    var client = _factory.CreateClient();
    var token = await GetUserToken();
    client.DefaultRequestHeaders.Authorization = 
        new AuthenticationHeaderValue("Bearer", token);
    
    // Act - Make requests up to the limit
    var requests = new List<HttpResponseMessage>();
    for (int i = 0; i < 100; i++)
    {
        var response = await client.GetAsync("/api/products");
        requests.Add(response);
    }
    
    // 101st request should be rate limited
    var rateLimitedResponse = await client.GetAsync("/api/products");
    
    // Assert
    requests.All(r => r.StatusCode == HttpStatusCode.OK).Should().BeTrue();
    rateLimitedResponse.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
}
```

### JWT & Authentication Tests

#### JWT Token Tests
```csharp
[Fact]
public async Task AuthService_GenerateJwtToken_ShouldCreateValidToken()
{
    // Arrange
    var authService = new AuthService();
    var user = new User { Id = "test-user", Role = "Admin" };
    
    // Act
    var token = await authService.GenerateJwtTokenAsync(user);
    
    // Assert
    token.Should().NotBeNullOrEmpty();
    
    var handler = new JwtSecurityTokenHandler();
    var jwtToken = handler.ReadJwtToken(token);
    
    jwtToken.Claims.Should().Contain(c => c.Type == "sub" && c.Value == "test-user");
    jwtToken.Claims.Should().Contain(c => c.Type == "role" && c.Value == "Admin");
}

[Fact]
public async Task AuthService_RefreshToken_ShouldGenerateNewTokens()
{
    // Arrange
    var authService = new AuthService();
    var user = new User { Id = "test-user", Role = "User" };
    var refreshToken = "valid-refresh-token";
    
    // Act
    var result = await authService.RefreshTokenAsync(user, refreshToken);
    
    // Assert
    result.Should().NotBeNull();
    result.AccessToken.Should().NotBeNullOrEmpty();
    result.RefreshToken.Should().NotBeNullOrEmpty();
    result.AccessToken.Should().NotBe(refreshToken); // Should be different
}
```

## 📊 Testing Audit System

### Audit Service Testing

#### Unit Tests для AuditService
```csharp
[Fact]
public async Task LogEntityChangeAsync_ShouldCreateAuditLog()
{
    // Arrange
    var entityName = "Product";
    var entityId = "123";
    var action = "CREATE";
    var oldValues = new { Name = "Old Name" };
    var newValues = new { Name = "New Name" };
    var description = "Product created";

    // Act
    await _auditService.LogEntityChangeAsync(entityName, entityId, action, oldValues, newValues, description);

    // Assert
    var auditLog = await Context.AuditLogs.FirstOrDefaultAsync();
    auditLog.Should().NotBeNull();
    auditLog.EntityName.Should().Be(entityName);
    auditLog.EntityId.Should().Be(entityId);
    auditLog.Action.Should().Be(action);
    auditLog.UserId.Should().Be("test-user-id");
    auditLog.Username.Should().Be("testuser");
    auditLog.OldValues.Should().NotBeNull();
    auditLog.NewValues.Should().NotBeNull();
    auditLog.Description.Should().Be(description);
    auditLog.IpAddress.Should().Be("127.0.0.1");
    auditLog.UserAgent.Should().Be("TestAgent/1.0");
}

[Fact]
public async Task LogHttpRequestAsync_ShouldCreateHttpAuditLog()
{
    // Arrange
    var httpMethod = "GET";
    var url = "https://localhost:5001/api/products";
    var statusCode = 200;
    var duration = 150L;

    // Act
    await _auditService.LogHttpRequestAsync(httpMethod, url, statusCode, duration);

    // Assert
    var auditLog = await Context.AuditLogs.FirstOrDefaultAsync();
    auditLog.Should().NotBeNull();
    auditLog.EntityName.Should().Be("HTTP");
    auditLog.HttpMethod.Should().Be(httpMethod);
    auditLog.Url.Should().Be(url);
    auditLog.StatusCode.Should().Be(statusCode);
    auditLog.Duration.Should().Be(duration);
}

[Fact]
public async Task GetAuditLogsAsync_WithFilters_ShouldReturnFilteredLogs()
{
    // Arrange
    await SeedAuditLogs();

    // Act
    var result = await _auditService.GetAuditLogsAsync(
        entityName: "Product",
        action: "CREATE",
        userId: null,
        startDate: null,
        endDate: null,
        severity: null,
        page: 1,
        pageSize: 10);

    // Assert
    result.TotalCount.Should().Be(2);
    result.Logs.Should().HaveCount(2);
    result.Logs.Should().OnlyContain(log => log.EntityName == "Product");
    result.Logs.Should().OnlyContain(log => log.Action == "CREATE");
}
```

#### Integration Tests для AuditController
```csharp
[Fact]
public async Task GetAuditLogs_WithAuthentication_ShouldReturnAuditLogs()
{
    // Arrange
    await SeedTestData();
    var token = await GetAdminToken();
    _client.DefaultRequestHeaders.Authorization = 
        new AuthenticationHeaderValue("Bearer", token);

    // Act
    var response = await _client.GetAsync("/api/audit");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    
    var result = await response.Content.ReadFromJsonAsync<AuditLogResponse>();
    result.Should().NotBeNull();
    result.Logs.Should().HaveCountGreaterThan(0);
}

[Fact]
public async Task GetAuditLogs_WithFilters_ShouldReturnFilteredLogs()
{
    // Arrange
    await SeedTestData();
    var token = await GetAdminToken();
    _client.DefaultRequestHeaders.Authorization = 
        new AuthenticationHeaderValue("Bearer", token);

    // Act
    var response = await _client.GetAsync("/api/audit?entityName=Product&action=CREATE");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    
    var result = await response.Content.ReadFromJsonAsync<AuditLogResponse>();
    result.Should().NotBeNull();
    result.Logs.Should().OnlyContain(log => log.EntityName == "Product");
    result.Logs.Should().OnlyContain(log => log.Action == "CREATE");
}

[Fact]
public async Task GetAuditStatistics_ShouldReturnStatistics()
{
    // Arrange
    await SeedTestData();
    var token = await GetAdminToken();
    _client.DefaultRequestHeaders.Authorization = 
        new AuthenticationHeaderValue("Bearer", token);

    // Act
    var response = await _client.GetAsync("/api/audit/statistics");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    
    var result = await response.Content.ReadFromJsonAsync<AuditStatisticsDto>();
    result.Should().NotBeNull();
    result.TotalLogs.Should().BeGreaterThan(0);
    result.SuccessfulLogs.Should().BeGreaterOrEqualTo(0);
    result.FailedLogs.Should().BeGreaterOrEqualTo(0);
}

[Fact]
public async Task CleanupOldLogs_AsAdmin_ShouldReturnCleanupResult()
{
    // Arrange
    await SeedTestData();
    var token = await GetAdminToken();
    _client.DefaultRequestHeaders.Authorization = 
        new AuthenticationHeaderValue("Bearer", token);

    // Act
    var response = await _client.DeleteAsync("/api/audit/cleanup?daysToKeep=30");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    
    var result = await response.Content.ReadFromJsonAsync<CleanupResultDto>();
    result.Should().NotBeNull();
    result.DeletedCount.Should().BeGreaterOrEqualTo(0);
    result.DaysToKeep.Should().Be(30);
}
```

### Audit Middleware Testing

#### Middleware Unit Tests
```csharp
[Fact]
public async Task AuditMiddleware_ShouldLogHttpRequest()
{
    // Arrange
    var auditService = new Mock<IAuditService>();
    var middleware = new AuditMiddleware(async (context) =>
    {
        context.Response.StatusCode = 200;
        await context.Response.WriteAsync("Test response");
    }, auditService.Object);
    
    var context = new DefaultHttpContext();
    context.Request.Method = "GET";
    context.Request.Path = "/api/products";
    context.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
    {
        new Claim(ClaimTypes.NameIdentifier, "test-user")
    }));
    context.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.1");
    context.Request.Headers["User-Agent"] = "TestAgent/1.0";

    // Act
    await middleware.Invoke(context);

    // Assert
    auditService.Verify(a => a.LogHttpRequestAsync(
        "GET", 
        "/api/products", 
        200, 
        It.IsAny<long>()), Times.Once);
}

[Fact]
public async Task AuditMiddleware_ShouldExtractUserInfo()
{
    // Arrange
    var auditService = new Mock<IAuditService>();
    var middleware = new AuditMiddleware(async (context) =>
    {
        await context.Response.WriteAsync("Test");
    }, auditService.Object);
    
    var context = new DefaultHttpContext();
    context.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
    {
        new Claim(ClaimTypes.NameIdentifier, "user-123"),
        new Claim(ClaimTypes.Name, "testuser"),
        new Claim(ClaimTypes.Role, "Admin")
    }));
    context.Request.Method = "POST";
    context.Request.Path = "/api/products";

    // Act
    await middleware.Invoke(context);

    // Assert
    auditService.Verify(a => a.LogHttpRequestAsync(
        "POST", 
        "/api/products", 
        200, 
        It.IsAny<long>()), Times.Once);
}
```

## 🚀 Запуск тестов

### Все тесты с автоматической очисткой БД
```powershell
# Из корня проекта
dotnet test

# Или через PowerShell скрипт
.\test\run-tests.ps1
```

### Конкретные типы тестов
```powershell
# Unit тесты (InMemory БД)
dotnet test Inventory.UnitTests

# Integration тесты (PostgreSQL БД)
dotnet test Inventory.IntegrationTests

# Component тесты (Mocked сервисы)
dotnet test Inventory.ComponentTests
```

### PowerShell скрипты
```powershell
# Запуск всех тестов с очисткой
.\test\run-tests.ps1

# Запуск конкретных тестов
.\test\run-tests.ps1 -Project unit
.\test\run-tests.ps1 -Project integration
.\test\run-tests.ps1 -Project component

# С подробным выводом
.\test\run-tests.ps1 -Verbose

# С покрытием кода
.\test\run-tests.ps1 -Coverage
```

### Управление тестовыми базами данных
```powershell
# Очистка всех тестовых БД
.\scripts\Cleanup-TestDatabases.ps1

# Запуск тестов без очистки
.\scripts\Run-TestsWithCleanup.ps1 -NoCleanup
```

### С покрытием кода
```bash
dotnet test --collect:"XPlat Code Coverage"

# Генерация HTML отчета
.\scripts\Generate-Coverage-Report.ps1 -OpenReport
```

### С подробным выводом
```bash
dotnet test --logger "console;verbosity=detailed"
```

## 🎯 Принципы тестирования

### Unit Tests
- **Изоляция**: Каждый тест независим
- **Быстрота**: Тесты выполняются быстро
- **Мокирование**: Внешние зависимости мокируются
- **Один сценарий**: Один тест = один сценарий

### Integration Tests
- **Реальная среда**: Используется реальная инфраструктура
- **Полный цикл**: Тестируется весь путь запроса
- **База данных**: Используется реальная PostgreSQL БД для изоляции
- **Конфигурация**: Тестовая конфигурация отдельно
- **Уникальные БД**: Каждый тест получает свою БД
- **Автоматическая очистка**: БД удаляются после тестов

### Component Tests
- **UI тестирование**: Тестируется рендеринг компонентов
- **Взаимодействие**: Тестируются пользовательские действия
- **Состояние**: Тестируется управление состоянием
- **События**: Тестируются события и callbacks

## 🏗️ Database Test Architecture

### TestBase Configuration
```csharp
public class IntegrationTestBase : IDisposable
{
    protected string TestDatabaseName { get; }
    protected WebApplicationFactory<Program> Factory { get; }
    protected HttpClient Client { get; }
    protected AppDbContext Context { get; }

    protected IntegrationTestBase(WebApplicationFactory<Program> factory)
    {
        // 1. Создание уникального имени БД
        TestDatabaseName = $"inventory_test_{Guid.NewGuid():N}_{DateTime.UtcNow:yyyyMMddHHmmss}";
        
        // 2. Настройка тестовой БД
        Factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Замена connection string на тестовую
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null) services.Remove(descriptor);

                services.AddDbContext<AppDbContext>(options =>
                {
                    var connectionString = $"Host=localhost;Port=5432;Database={TestDatabaseName};Username=postgres;Password=postgres;Pooling=false;";
                    options.UseNpgsql(connectionString);
                });
            });
        });
    }
}
```

### Test Data Management
```csharp
// Создание тестовых данных без фиксированных ID
protected async Task SeedTestDataAsync()
{
    // 1. Создание категорий
    var electronicsCategory = new Category
    {
        Name = "Electronics",
        Description = "Electronic devices",
        IsActive = true,
        CreatedAt = DateTime.UtcNow
    };
    
    Context.Categories.Add(electronicsCategory);
    await Context.SaveChangesAsync(); // Получаем реальный ID
    
    // 2. Создание подкатегории с правильной ссылкой
    var smartphonesCategory = new Category
    {
        Name = "Smartphones",
        Description = "Mobile phones",
        IsActive = true,
        ParentCategoryId = electronicsCategory.Id, // Используем реальный ID
        CreatedAt = DateTime.UtcNow
    };
    
    Context.Categories.Add(smartphonesCategory);
    await Context.SaveChangesAsync();
}
```

## 🎯 Best Practices

### 1. Изоляция тестов
```csharp
[Fact]
public async Task GetCategories_WithEmptyDatabase_ShouldReturnEmptyList()
{
    // Arrange - полная очистка БД
    await CleanupDatabaseAsync();
    await InitializeEmptyAsync(); // Только пользователи, без данных
    await SetAuthHeaderAsync();

    // Act
    var response = await Client.GetAsync("/api/category");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<CategoryDto>>>();
    result!.Data.Should().BeEmpty();
}
```

### 2. Управление тестовыми данными
```csharp
// ✅ Правильно - без фиксированных ID
var category = new Category { Name = "Test", IsActive = true };
Context.Categories.Add(category);
await Context.SaveChangesAsync(); // Получаем реальный ID

// ❌ Неправильно - фиксированные ID
var category = new Category { Id = 1, Name = "Test", IsActive = true };
```

### 3. Очистка между тестами
```csharp
// Очистка в правильном порядке (с учетом foreign keys)
protected async Task CleanupDatabaseAsync()
{
    // 1. Удаляем зависимые таблицы
    Context.InventoryTransactions.RemoveRange(Context.InventoryTransactions);
    Context.Products.RemoveRange(Context.Products);
    
    // 2. Удаляем основные таблицы
    Context.Categories.RemoveRange(Context.Categories);
    Context.Manufacturers.RemoveRange(Context.Manufacturers);
    
    // 3. Очищаем Identity таблицы
    await Context.Database.ExecuteSqlRawAsync("DELETE FROM \"AspNetUserRoles\"");
    await Context.Database.ExecuteSqlRawAsync("DELETE FROM \"AspNetUsers\"");
}
```

### 4. Именование тестов
```csharp
// ✅ Четкое описание сценария
[Fact]
public async Task GetDashboardStats_WithEmptyDatabase_ShouldReturnZeroStats()

[Fact]
public async Task GetCategories_WithValidData_ShouldReturnCategories()

[Fact]
public async Task CreateCategory_WithInvalidData_ShouldReturnBadRequest()
```

### 5. Асинхронные тесты
```csharp
// ✅ Always use async for database tests
[Fact]
public async Task GetDashboardStats_WithEmptyDatabase_ShouldReturnZeroStats()

// ❌ Don't use sync methods for database operations
[Fact]
public void GetDashboardStats_WithEmptyDatabase_ShouldReturnZeroStats()
```

### 6. Правильные утверждения
```csharp
// ✅ Use FluentAssertions for readable tests
response.StatusCode.Should().Be(HttpStatusCode.OK);
result!.Data.Should().NotBeNull();
result.Data.Should().HaveCount(2);
result.Data.Should().Contain(c => c.Name == "Electronics");

// ❌ Avoid basic assertions
Assert.Equal(HttpStatusCode.OK, response.StatusCode);
Assert.NotNull(result.Data);
Assert.Equal(2, result.Data.Count);
```

## 🚀 Performance & Reliability

### Автоматическая очистка тестовых БД
- **После каждого теста** — база данных автоматически удаляется
- **После всего набора тестов** — все тестовые БД очищаются
- **При ошибках** — база данных все равно удаляется в `Dispose()`

### Мониторинг тестовых БД
```powershell
# Проверка существующих тестовых БД
docker exec inventoryctrl-db-1 psql -U postgres -c "SELECT datname FROM pg_database WHERE datname LIKE 'inventory_test_%';"

# Очистка всех тестовых БД
.\scripts\Cleanup-TestDatabases.ps1
```

## 📊 Test Results

### Текущее состояние
- ✅ **120+ тестов** — все проходят успешно
- ✅ **100% успешность** — 0 ошибок
- ✅ **Автоматическая очистка** — тестовые БД удаляются автоматически
- ✅ **Изоляция тестов** — каждый тест использует свою БД
- ✅ **Реалистичное тестирование** — работа с реальной PostgreSQL
- ✅ **Real-time Features** — тестирование SignalR и Notification System
- ✅ **Security Features** — тестирование Rate Limiting и Audit System

### Категории тестов

| Тип | Количество | Статус | База данных | Новые возможности |
|-----|------------|--------|-------------|-------------------|
| Unit Tests | 79+ | ✅ Passing | InMemory | AuditService, NotificationService, AuthService |
| Integration Tests | 29+ | ✅ Passing | PostgreSQL | AuditController, SignalR, Rate Limiting |
| Component Tests | 12+ | ✅ Passing | Mocked | ToastNotification, NotificationCenter |

### Новые тестовые возможности

#### SignalR & Notifications
- ✅ **NotificationHub Tests** — тестирование real-time коммуникации
- ✅ **ToastNotification Tests** — тестирование UI компонентов уведомлений
- ✅ **NotificationService Tests** — тестирование сервиса уведомлений
- ✅ **SignalR Connection Tests** — тестирование подключений клиентов

#### Security & Rate Limiting
- ✅ **Rate Limiting Tests** — тестирование ограничений по ролям
- ✅ **JWT Token Tests** — тестирование генерации и валидации токенов
- ✅ **Refresh Token Tests** — тестирование обновления токенов
- ✅ **Authentication Tests** — тестирование системы аутентификации

#### Audit System
- ✅ **AuditService Tests** — тестирование логирования действий
- ✅ **AuditController Tests** — тестирование API endpoints аудита
- ✅ **AuditMiddleware Tests** — тестирование middleware для HTTP запросов
- ✅ **Audit Statistics Tests** — тестирование статистики и отчетов

## 🛠️ Troubleshooting

### Общие проблемы

#### 1. Database Already Exists
```
Error: database "inventory_test_abc123" already exists
```
**Решение**: Убедитесь в правильной очистке в методе `Dispose()`

#### 2. Foreign Key Constraint Violations
```
Error: insert or update on table "Products" violates foreign key constraint
```
**Решение**: Создавайте ссылочные сущности сначала, используйте динамические ID

#### 3. Connection String Issues
```
Error: could not connect to server
```
**Решение**: Убедитесь, что контейнер PostgreSQL запущен, проверьте строку подключения

#### 4. Test Data Conflicts
```
Error: The instance of entity type 'Category' cannot be tracked
```
**Решение**: Используйте `AsNoTracking()` или создавайте новые экземпляры контекста

### Новые проблемы v2

#### 5. SignalR Connection Issues
```
Error: Failed to start SignalR connection
```
**Решение**: 
- Проверьте правильность URL для SignalR Hub
- Убедитесь в корректности JWT токена для аутентификации
- Проверьте настройки CORS для SignalR

#### 6. Rate Limiting Test Failures
```
Error: Rate limit exceeded
```
**Решение**:
- Убедитесь в правильной настройке лимитов по ролям
- Проверьте, что тесты не превышают установленные лимиты
- Используйте отдельные тестовые токены для разных ролей

#### 7. Audit Log Issues
```
Error: Audit log creation failed
```
**Решение**:
- Проверьте правильность настройки HttpContext в тестах
- Убедитесь в корректности Claims для пользователя
- Проверьте подключение к тестовой БД для AuditService

#### 8. Notification Service Issues
```
Error: Notification service not registered
```
**Решение**:
- Убедитесь в правильной регистрации NotificationService в DI контейнере
- Проверьте моки для зависимостей NotificationService
- Убедитесь в корректности тестовых данных для уведомлений

### Команды отладки

```powershell
# Проверить статус контейнера PostgreSQL
docker ps | findstr postgres

# Проверить подключения к тестовым БД
docker exec inventoryctrl-db-1 psql -U postgres -c "SELECT datname, usename, application_name FROM pg_stat_activity WHERE datname LIKE 'inventory_test_%';"

# Мониторить создание тестовых БД
docker logs inventoryctrl-db-1 -f

# Проверить SignalR подключения
docker logs inventoryctrl-api-1 -f | findstr "SignalR"

# Проверить Rate Limiting логи
docker logs inventoryctrl-api-1 -f | findstr "Rate limit"

# Проверить Audit логи
docker logs inventoryctrl-api-1 -f | findstr "Audit"

# Проверить Notification логи
docker logs inventoryctrl-api-1 -f | findstr "Notification"
```

## 📚 Дополнительные ресурсы

### Инструменты
- [xUnit Documentation](https://xunit.net/) — фреймворк тестирования
- [bUnit Documentation](https://bunit.dev/) — тестирование Blazor компонентов
- [FluentAssertions](https://fluentassertions.com/) — читаемые утверждения
- [Moq](https://github.com/moq/moq4) — мокирование зависимостей
- [Microsoft.AspNetCore.SignalR.Testing](https://docs.microsoft.com/en-us/aspnet/core/signalr/testing) — тестирование SignalR

### Microsoft Documentation
- [Entity Framework Core Testing](https://docs.microsoft.com/en-us/ef/core/testing/)
- [ASP.NET Core Integration Testing](https://docs.microsoft.com/en-us/aspnet/core/test/integration-tests)
- [SignalR Testing](https://docs.microsoft.com/en-us/aspnet/core/signalr/testing)
- [Rate Limiting Testing](https://docs.microsoft.com/en-us/aspnet/core/performance/rate-limit)

### Новые возможности v2
- **SignalR Testing** — тестирование real-time коммуникации
- **Rate Limiting Testing** — тестирование ограничений по ролям
- **Audit System Testing** — тестирование системы аудита
- **Notification System Testing** — тестирование уведомлений

### Полезные ссылки
- [PostgreSQL Documentation](https://www.postgresql.org/docs/)
- [Docker Documentation](https://docs.docker.com/)
- [JWT Testing](https://jwt.io/) — тестирование JWT токенов
- [SignalR Client Testing](https://docs.microsoft.com/en-us/aspnet/core/signalr/javascript-client)

---

> 💡 **Совет**: Используйте реальную PostgreSQL для Integration тестов — это обеспечивает более реалистичное тестирование и выявляет проблемы, которые не заметны при использовании InMemory БД. Новые возможности v2 включают комплексное тестирование SignalR, Rate Limiting, Audit System и Notification System для обеспечения enterprise-уровня качества.
