# План исправления ошибок Radzen Blazor

## Анализ проблем

### 1. КРИТИЧЕСКАЯ ОШИБКА: Конфликт параметра 'context'

**Проблема:** В файле `src/Inventory.UI/Pages/Login.razor`:
- Строки 11, 19: `AuthorizeView` использует `Context="authContext"` для `Authorized`/`NotAuthorized`
- Строка 35: `RadzenTemplateForm` внутри `NotAuthorized` использует параметр `context` по умолчанию
- Это создает конфликт имен параметров в Blazor

**Решение:** Добавить явное имя параметра для ChildContent в RadzenTemplateForm:
```razor
<RadzenTemplateForm Data="@loginModel" Submit="@(async (LoginRequest model) => await HandleLogin())">
    <ChildContent Context="formContext">
        <!-- содержимое формы -->
    </ChildContent>
</RadzenTemplateForm>
```

### 2. ОШИБКИ: Неправильные преобразования типов int в Guid

**Проблема:** В трех файлах происходит попытка присвоить `int` значение полю типа `Guid`:

#### ProductModelManagementWidget.razor:192
```csharp
manufacturerOptions.Add(new ManufacturerOption
{
    Value = manufacturer.Id,  // manufacturer.Id имеет тип Guid, но присваивается int
    Text = manufacturer.Name
});
```

#### LocationManagementWidget.razor:330
```csharp
parentLocationOptions.Add(new ParentLocationOption
{
    Value = location.Id,  // location.Id имеет тип Guid, но присваивается int
    Text = location.Name
});
```

#### CategoryManagementWidget.razor:277
```csharp
parentCategoryOptions.Add(new ParentCategoryOption
{
    Value = category.Id,  // category.Id имеет тип Guid, но присваивается int
    Text = category.Name
});
```

**Решение:** Проверить типы данных в моделях и DTO. Возможно:
1. Поля `Id` в моделях должны быть `int`, а не `Guid`
2. Или поля `Value` в Option классах должны быть `Guid`, а не `int`

### 3. ОШИБКА: Неправильное сравнение LogLevel со строкой

**Проблема:** В файле `DebugLogs.razor:266`:
```csharp
(string.IsNullOrEmpty(selectedLevel) || log.Level == selectedLevel)
```

**Решение:** `log.Level` имеет тип `LogLevel` (enum), а `selectedLevel` - строка. Нужно:
```csharp
(string.IsNullOrEmpty(selectedLevel) || log.Level.ToString() == selectedLevel)
```

### 4. ПРЕДУПРЕЖДЕНИЯ: Недостающие @using директивы

**Проблема:** Несколько Radzen компонентов не распознаются из-за отсутствия директив.

**Решение:** Добавить в `_Imports.razor`:
```razor
@using Radzen.Blazor
```

### 5. ПРЕДУПРЕЖДЕНИЕ: Дублирование using директив

**Проблема:** В `AdminGuard.razor` дублируются директивы `using Radzen` и `using Radzen.Blazor`.

**Решение:** Удалить дублирующиеся директивы.

## План действий

### Этап 1: Исправление критической ошибки
1. Исправить конфликт параметра context в `Login.razor`
2. Проверить другие файлы на аналогичные конфликты

### Этап 2: Исправление ошибок типов
1. Проанализировать модели данных для понимания правильных типов
2. Исправить преобразования int в Guid
3. Исправить сравнение LogLevel со строкой

### Этап 3: Исправление предупреждений
1. Добавить недостающие @using директивы
2. Удалить дублирующиеся директивы
3. Удалить неиспользуемые поля

### Этап 4: Тестирование
1. Выполнить сборку проекта
2. Проверить, что все ошибки исправлены
3. Убедиться, что приложение запускается корректно

## Файлы для изменения

1. `src/Inventory.UI/Pages/Login.razor` - исправление конфликта context
2. `src/Inventory.UI/Components/Admin/ProductModelManagementWidget.razor` - исправление типа Guid
3. `src/Inventory.UI/Components/Admin/LocationManagementWidget.razor` - исправление типа Guid
4. `src/Inventory.UI/Components/Admin/CategoryManagementWidget.razor` - исправление типа Guid
5. `src/Inventory.UI/Pages/Debug/DebugLogs.razor` - исправление сравнения LogLevel
6. `src/Inventory.UI/_Imports.razor` - добавление using директив
7. `src/Inventory.UI/Components/AdminGuard.razor` - удаление дублирующихся using
8. `src/Inventory.UI/Pages/Admin/UserManagement.razor` - удаление неиспользуемого поля