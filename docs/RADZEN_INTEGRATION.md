# Radzen Blazor Integration Guide

## Обзор

Проект успешно интегрирован с Radzen Blazor - библиотекой компонентов для Blazor приложений. Radzen предоставляет более 90 бесплатных компонентов для создания современных веб-приложений.

## Что было настроено

### 1. NuGet пакет
```xml
<PackageReference Include="Radzen.Blazor" />
```

### 2. Imports (_Imports.razor)
```csharp
@using Radzen
@using Radzen.Blazor
```

### 3. Тема (App.razor)
```html
<RadzenTheme Theme="material" @rendermode="InteractiveAuto" />
<script src="_content/Radzen.Blazor/Radzen.Blazor.js?v=@(typeof(Radzen.Colors).Assembly.GetName().Version)"></script>
```

### 4. Компоненты (MainLayout.razor)
```html
<RadzenComponents @rendermode="InteractiveAuto" />
```

### 5. Сервисы (Program.cs)
```csharp
using Radzen;
// ...
builder.Services.AddRadzenComponents();
```

## Доступные темы

- **Material** (текущая) - Material Design
- **Material Dark** - темная версия Material Design
- **Standard** - стандартная тема
- **Standard Dark** - темная стандартная тема
- **Default** - тема по умолчанию
- **Dark** - темная тема
- **Humanistic** - гуманистическая тема
- **Humanistic Dark** - темная гуманистическая тема
- **Software** - программная тема
- **Software Dark** - темная программная тема

## Тестирование

Для проверки работы Radzen компонентов создана тестовая страница:
- URL: `/radzen-test`
- Компонент: `RadzenTestComponent.razor`

Тестовая страница включает:
- RadzenButton
- RadzenTextBox
- RadzenDropDown
- RadzenCheckBox
- RadzenDataGrid
- RadzenNotification (планируется)

## Основные компоненты

### Формы
- `RadzenTextBox` - текстовое поле
- `RadzenPassword` - поле пароля
- `RadzenTextArea` - многострочное текстовое поле
- `RadzenNumeric` - числовое поле
- `RadzenDatePicker` - выбор даты
- `RadzenTimePicker` - выбор времени
- `RadzenDropDown` - выпадающий список
- `RadzenListBox` - список выбора
- `RadzenCheckBox` - чекбокс
- `RadzenRadioButtonList` - радиокнопки
- `RadzenSwitch` - переключатель

### Кнопки и действия
- `RadzenButton` - кнопка
- `RadzenButtonGroup` - группа кнопок
- `RadzenSplitButton` - кнопка с выпадающим меню
- `RadzenMenu` - меню
- `RadzenContextMenu` - контекстное меню

### Данные
- `RadzenDataGrid` - таблица данных
- `RadzenDataList` - список данных
- `RadzenTree` - дерево
- `RadzenTreeView` - представление дерева
- `RadzenPager` - пагинация

### Навигация
- `RadzenTabs` - вкладки
- `RadzenAccordion` - аккордеон
- `RadzenCard` - карточка
- `RadzenPanel` - панель
- `RadzenStack` - стек
- `RadzenSplitter` - разделитель

### Уведомления
- `RadzenNotification` - уведомления
- `RadzenAlert` - предупреждения
- `RadzenBadge` - значок
- `RadzenProgressBar` - прогресс-бар
- `RadzenProgressBarCircular` - круговой прогресс-бар

### Диалоги
- `RadzenDialog` - диалог
- `RadzenDrawer` - выдвижная панель
- `RadzenSidebar` - боковая панель

## Примеры использования

### Простая форма
```html
<RadzenCard>
    <RadzenStack Orientation="Orientation.Vertical" Gap="1rem">
        <RadzenTextBox @bind-Value="@name" Placeholder="Введите имя" />
        <RadzenDropDown @bind-Value="@selectedValue" 
                       Data="@options" 
                       ValueProperty="Value" 
                       TextProperty="Text" />
        <RadzenButton Text="Сохранить" 
                     ButtonStyle="ButtonStyle.Primary" 
                     Click="@OnSave" />
    </RadzenStack>
</RadzenCard>
```

### Таблица данных
```html
<RadzenDataGrid Data="@items" TItem="Item" AllowPaging="true" PageSize="10">
    <Columns>
        <RadzenDataGridColumn TItem="Item" Property="Id" Title="ID" />
        <RadzenDataGridColumn TItem="Item" Property="Name" Title="Название" />
        <RadzenDataGridColumn TItem="Item" Property="Value" Title="Значение" />
    </Columns>
</RadzenDataGrid>
```

### Уведомления
```csharp
@inject NotificationService notificationService

private void ShowSuccess()
{
    notificationService.Notify(new NotificationMessage 
    { 
        Severity = NotificationSeverity.Success, 
        Summary = "Успех", 
        Detail = "Операция выполнена успешно" 
    });
}
```

## Рекомендации

1. **Используйте RadzenComponents** - обязательно добавьте `<RadzenComponents />` в MainLayout для работы интерактивных компонентов
2. **Выберите подходящую тему** - Material тема хорошо подходит для современных приложений
3. **Используйте консистентный стиль** - придерживайтесь выбранной темы во всем приложении
4. **Тестируйте компоненты** - используйте тестовую страницу для проверки новых компонентов
5. **Изучайте документацию** - Radzen имеет отличную документацию с примерами

## Полезные ссылки

- [Официальная документация Radzen](https://blazor.radzen.com/)
- [Примеры компонентов](https://blazor.radzen.com/get-started)
- [API документация](https://blazor.radzen.com/docs/api)
- [Темы](https://blazor.radzen.com/themes)
