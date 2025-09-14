# Inventory Control Tests

Этот каталог содержит тестовые проекты для системы управления инвентарем.

## Структура тестов

### Inventory.UnitTests
**Назначение**: Unit-тесты для бизнес-логики и моделей
- Тестирование контроллеров API
- Тестирование моделей данных
- Тестирование сервисов
- Тестирование валидации

**Технологии**:
- xUnit
- Moq для мокирования
- FluentAssertions для читаемых утверждений

### Inventory.IntegrationTests
**Назначение**: Integration-тесты для API endpoints
- Тестирование полного цикла API запросов
- Тестирование с реальной базой данных (InMemory)
- Тестирование аутентификации и авторизации
- Тестирование CORS и middleware

**Технологии**:
- xUnit
- Microsoft.AspNetCore.Mvc.Testing
- Entity Framework InMemory
- FluentAssertions

### Inventory.ComponentTests
**Назначение**: Тесты для Blazor компонентов
- Тестирование UI компонентов
- Тестирование пользовательских взаимодействий
- Тестирование рендеринга
- Тестирование событий

**Технологии**:
- xUnit
- bUnit для тестирования Blazor компонентов
- FluentAssertions

## Запуск тестов

### Все тесты
```bash
dotnet test
```

### Конкретный проект
```bash
# Unit тесты
dotnet test Inventory.UnitTests

# Integration тесты
dotnet test Inventory.IntegrationTests

# Component тесты
dotnet test Inventory.ComponentTests
```

### С покрытием кода
```bash
dotnet test --collect:"XPlat Code Coverage"
```

### С подробным выводом
```bash
dotnet test --logger "console;verbosity=detailed"
```

## Принципы тестирования

### Unit Tests
- **Изоляция**: Каждый тест независим
- **Быстрота**: Тесты выполняются быстро
- **Мокирование**: Внешние зависимости мокируются
- **Один сценарий**: Один тест = один сценарий

### Integration Tests
- **Реальная среда**: Используется реальная инфраструктура
- **Полный цикл**: Тестируется весь путь запроса
- **База данных**: Используется InMemory база для изоляции
- **Конфигурация**: Тестовая конфигурация отдельно

### Component Tests
- **UI тестирование**: Тестируется рендеринг компонентов
- **Взаимодействие**: Тестируются пользовательские действия
- **Состояние**: Тестируется управление состоянием
- **События**: Тестируются события и callbacks

## Структура тестов

```
test/
├── Inventory.UnitTests/
│   ├── Services/           # Тесты сервисов
│   ├── Models/             # Тесты моделей
│   ├── Controllers/        # Тесты контроллеров
│   └── Validators/         # Тесты валидаторов
├── Inventory.IntegrationTests/
│   ├── Controllers/        # Integration тесты API
│   ├── Middleware/         # Тесты middleware
│   └── Database/           # Тесты БД
├── Inventory.ComponentTests/
│   ├── Components/         # Тесты Blazor компонентов
│   ├── Pages/              # Тесты страниц
│   └── Layout/             # Тесты макетов
└── Inventory.Tests.sln     # Solution файл
```

## Конфигурация

### appsettings.Test.json
Тестовая конфигурация для integration тестов:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "InMemory"
  },
  "Jwt": {
    "Key": "TestKeyThatIsAtLeast32CharactersLong",
    "Issuer": "TestIssuer",
    "Audience": "TestAudience"
  }
}
```

### Тестовые данные
- Тестовые пользователи создаются в `DbInitializer`
- Тестовые данные загружаются из `TestData` классов
- Моки настраиваются в `TestFixtures`

## Лучшие практики

### Именование тестов
```csharp
[Fact]
public void MethodName_Scenario_ExpectedResult()
{
    // Arrange
    // Act  
    // Assert
}
```

### Структура теста
```csharp
[Fact]
public void Login_WithValidCredentials_ShouldReturnToken()
{
    // Arrange - подготовка данных
    var request = new LoginRequest { ... };
    
    // Act - выполнение действия
    var result = await controller.Login(request);
    
    // Assert - проверка результата
    result.Should().NotBeNull();
}
```

### Мокирование
```csharp
var mockService = new Mock<IService>();
mockService.Setup(x => x.Method(It.IsAny<string>()))
    .ReturnsAsync(expectedResult);
```

### Асинхронные тесты
```csharp
[Fact]
public async Task AsyncMethod_ShouldWork()
{
    // Arrange
    // Act
    var result = await service.AsyncMethod();
    
    // Assert
    result.Should().NotBeNull();
}
```

## Отчеты

### Покрытие кода
Тесты генерируют отчеты о покрытии кода в формате Cobertura.

### Результаты тестов
Результаты сохраняются в `TestResults/` каталоге.

### CI/CD
Тесты интегрированы в процесс CI/CD и выполняются автоматически при каждом коммите.

## Требования

- .NET 9.0 SDK
- Visual Studio 2022 или VS Code
- xUnit Test Explorer (для VS Code)

## Полезные ссылки

- [xUnit Documentation](https://xunit.net/)
- [bUnit Documentation](https://bunit.dev/)
- [FluentAssertions](https://fluentassertions.com/)
- [Moq](https://github.com/moq/moq4)
