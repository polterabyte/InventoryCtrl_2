# Testing Guide

Комплексное руководство по тестированию системы управления инвентарем.

## 🎯 Стратегия тестирования

### Философия тестирования
- **Real PostgreSQL** — используется реальная СУБД вместо InMemory для Integration Tests
- **Unique Test Databases** — каждый тест получает уникальную базу данных
- **Automatic Cleanup** — тестовые БД автоматически удаляются после тестов
- **Complete Isolation** — полная изоляция тестовых данных

### Результаты тестирования
- ✅ **120 тестов** — все проходят успешно
- ✅ **100% успешность** — 0 ошибок
- ✅ **Автоматическая очистка** — тестовые БД удаляются автоматически
- ✅ **Изоляция тестов** — каждый тест использует свою БД

## 🏗 Типы тестов

### Unit Tests (79 тестов)
**Назначение**: Тестирование бизнес-логики и моделей

**Технологии**:
- xUnit — фреймворк тестирования
- Moq — мокирование зависимостей
- FluentAssertions — читаемые утверждения

**Структура**:
```
test/Inventory.UnitTests/
├── Services/           # Тесты сервисов
├── Models/             # Тесты моделей
├── Controllers/        # Тесты контроллеров
└── Validators/         # Тесты валидаторов
```

### Integration Tests (29 тестов)
**Назначение**: Тестирование API endpoints с реальной PostgreSQL БД

**Технологии**:
- xUnit — фреймворк тестирования
- Microsoft.AspNetCore.Mvc.Testing — тестирование API
- PostgreSQL — реальная СУБД
- Entity Framework Core — ORM
- FluentAssertions — читаемые утверждения
- Docker — контейнеризация PostgreSQL

**Особенности**:
- Уникальная БД для каждого теста
- Автоматическое создание и удаление БД
- Полная изоляция тестовых данных
- Тестирование реальных SQL запросов

**Структура**:
```
test/Inventory.IntegrationTests/
├── Controllers/        # Integration тесты API
├── Middleware/         # Тесты middleware
└── Database/           # Тесты БД
```

### Component Tests (12 тестов)
**Назначение**: Тестирование Blazor компонентов

**Технологии**:
- xUnit — фреймворк тестирования
- bUnit — тестирование Blazor компонентов
- FluentAssertions — читаемые утверждения
- Moq — мокирование сервисов

**Структура**:
```
test/Inventory.ComponentTests/
├── Components/         # Тесты Blazor компонентов
├── Pages/              # Тесты страниц
└── Layout/             # Тесты макетов
```

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
- ✅ **120 тестов** — все проходят успешно
- ✅ **100% успешность** — 0 ошибок
- ✅ **Автоматическая очистка** — тестовые БД удаляются автоматически
- ✅ **Изоляция тестов** — каждый тест использует свою БД
- ✅ **Реалистичное тестирование** — работа с реальной PostgreSQL

### Категории тестов

| Тип | Количество | Статус | База данных |
|-----|------------|--------|-------------|
| Unit Tests | 79 | ✅ Passing | InMemory |
| Integration Tests | 29 | ✅ Passing | PostgreSQL |
| Component Tests | 12 | ✅ Passing | Mocked |

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

### Команды отладки

```powershell
# Проверить статус контейнера PostgreSQL
docker ps | findstr postgres

# Проверить подключения к тестовым БД
docker exec inventoryctrl-db-1 psql -U postgres -c "SELECT datname, usename, application_name FROM pg_stat_activity WHERE datname LIKE 'inventory_test_%';"

# Мониторить создание тестовых БД
docker logs inventoryctrl-db-1 -f
```

## 📚 Дополнительные ресурсы

### Инструменты
- [xUnit Documentation](https://xunit.net/) — фреймворк тестирования
- [bUnit Documentation](https://bunit.dev/) — тестирование Blazor компонентов
- [FluentAssertions](https://fluentassertions.com/) — читаемые утверждения
- [Moq](https://github.com/moq/moq4) — мокирование зависимостей

### Microsoft Documentation
- [Entity Framework Core Testing](https://docs.microsoft.com/en-us/ef/core/testing/)
- [ASP.NET Core Integration Testing](https://docs.microsoft.com/en-us/aspnet/core/test/integration-tests)

### Полезные ссылки
- [PostgreSQL Documentation](https://www.postgresql.org/docs/)
- [Docker Documentation](https://docs.docker.com/)

---

> 💡 **Совет**: Используйте реальную PostgreSQL для Integration тестов — это обеспечивает более реалистичное тестирование и выявляет проблемы, которые не заметны при использовании InMemory БД.
