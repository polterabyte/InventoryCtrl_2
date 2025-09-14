# Component Testing Plan - Поэтапное добавление тестов

## Обзор

Этот план описывает стратегию постепенного добавления Component тестов для Blazor компонентов в соответствии с принципами из `.ai-agent-prompts`.

## Приоритеты тестирования

### Этап 1: Критически важные компоненты (ВЫПОЛНЕНО ✅)
- ✅ `UserGreeting.razor` - аутентификация и навигация
- ✅ `ToastNotification.razor` - система уведомлений
- ✅ `ProductCard.razor` - отображение продуктов

### Этап 2: Формы и взаимодействие (СЛЕДУЮЩИЙ)
- [ ] `LoginForm.razor` - форма входа
- [ ] `RegisterForm.razor` - форма регистрации
- [ ] `ProductForm.razor` - форма редактирования продуктов

### Этап 3: Макеты и навигация
- [ ] `MainLayout.razor` - основной макет
- [ ] `NavigationMenu.razor` - меню навигации
- [ ] `NotificationContainer.razor` - контейнер уведомлений

### Этап 4: Списки и коллекции
- [ ] `ProductList.razor` - список продуктов

## Стратегия тестирования

### Принципы
1. **Тестировать при создании**: Каждый новый компонент должен иметь тесты
2. **Тестировать при изменении**: При изменении существующего компонента добавлять тесты
3. **Приоритет по важности**: Критически важные компоненты тестируются в первую очередь

### Структура тестов
```csharp
public class ComponentNameTests : TestContext
{
    [Fact]
    public void MethodName_Scenario_ExpectedResult()
    {
        // Arrange - подготовка данных
        // Act - выполнение действия  
        // Assert - проверка результата
    }
}
```

### Типы тестов для каждого компонента

#### 1. Рендеринг
- ✅ Отображение с валидными данными
- ✅ Отображение с пустыми/null данными
- ✅ Отображение в разных состояниях (loading, error, success)

#### 2. Взаимодействие
- ✅ Клики по кнопкам
- ✅ Ввод в формы
- ✅ События и callbacks

#### 3. Условное отображение
- ✅ Показ/скрытие элементов по условиям
- ✅ Различные варианты отображения

#### 4. Валидация
- ✅ Ошибки валидации
- ✅ Успешная валидация

## Примеры тестов

### Тест рендеринга
```csharp
[Fact]
public void Render_WithValidData_ShouldDisplayCorrectly()
{
    // Arrange
    var data = new TestData { Name = "Test" };
    
    // Act
    var cut = RenderComponent<MyComponent>(p => p.Add(x => x.Data, data));
    
    // Assert
    cut.Find("h1").TextContent.Should().Be("Test");
}
```

### Тест взаимодействия
```csharp
[Fact]
public void ClickButton_ShouldTriggerCallback()
{
    // Arrange
    var clicked = false;
    var cut = RenderComponent<MyComponent>(p => 
        p.Add(x => x.OnClick, () => clicked = true));
    
    // Act
    cut.Find("button").Click();
    
    // Assert
    clicked.Should().BeTrue();
}
```

### Тест условного отображения
```csharp
[Fact]
public void Render_WithCondition_ShouldShowElement()
{
    // Arrange
    var cut = RenderComponent<MyComponent>(p => 
        p.Add(x => x.ShowElement, true));
    
    // Act & Assert
    cut.Find(".conditional-element").Should().NotBeNull();
}
```

## Инструменты и зависимости

### bUnit
- Основной фреймворк для тестирования Blazor компонентов
- Версия: 1.34.0

### FluentAssertions
- Для читаемых утверждений
- Версия: 7.0.0

### xUnit
- Фреймворк тестирования
- Версия: 2.9.2

## Запуск тестов

```bash
# Все Component тесты
dotnet test test/Inventory.ComponentTests

# Конкретный тест
dotnet test test/Inventory.ComponentTests --filter "ProductCardTests"

# С покрытием
dotnet test test/Inventory.ComponentTests --collect:"XPlat Code Coverage"
```

## Метрики качества

### Покрытие тестами
- **Цель**: 80%+ для критически важных компонентов
- **Текущее**: ~60% (3 из 5 основных компонентов)

### Количество тестов
- **Цель**: Минимум 3-5 тестов на компонент
- **Текущее**: 15 тестов для 3 компонентов

## Следующие шаги

### Немедленные действия
1. ✅ Создать тесты для `ToastNotification` и `ProductCard`
2. [ ] Создать тесты для форм (`LoginForm`, `RegisterForm`, `ProductForm`)
3. [ ] Добавить интеграционные тесты для взаимодействия компонентов

### Долгосрочные цели
1. [ ] Достичь 80% покрытия всех компонентов
2. [ ] Создать автоматические тесты для регрессий
3. [ ] Интегрировать тесты в CI/CD pipeline

## Обновления плана

Этот план обновляется при:
- Добавлении новых компонентов
- Изменении требований к тестированию
- Обнаружении новых паттернов тестирования

**Последнее обновление**: [Текущая дата]
**Версия плана**: 1.0
