# Отчет об исправлении ошибок сборки

## Проблема
При сборке проекта возникала ошибка:
```
CSC : error RZ9999: The child content element 'ChildContent' of component 'RadzenTemplateForm' uses the same parameter name ('context') as enclosing child content element 'Authorized' of component 'AuthorizeView'. Specify the parameter name like: '<ChildContent Context="another_name"> to resolve the ambiguity
```

## Причина
Конфликт имен параметров `context` между компонентами:
- `AuthorizeView` использует параметр `context` по умолчанию в своем `Authorized` блоке
- `RadzenTemplateForm` также использует параметр `context` по умолчанию в своем `ChildContent`

## Решение
Добавили явное указание имени параметра `Context="formContext"` для всех `ChildContent` блоков в `RadzenTemplateForm`, которые находятся внутри `AuthorizeView`.

## Исправленные файлы
1. `src/Inventory.UI/Components/ReferenceDataWidget.razor`
2. `src/Inventory.UI/Components/Admin/CategoryManagementWidget.razor`
3. `src/Inventory.UI/Components/Admin/LocationManagementWidget.razor`
4. `src/Inventory.UI/Components/Admin/ProductModelManagementWidget.razor`
5. `src/Inventory.UI/Components/Admin/UnitOfMeasureManagementWidget.razor`
6. `src/Inventory.UI/Components/Admin/WarehouseManagementWidget.razor`
7. `src/Inventory.UI/Components/Admin/ProductGroupManagementWidget.razor`
8. `src/Inventory.UI/Components/Admin/ManufacturerManagementWidget.razor`

## Дополнительные исправления
1. Удалены дублирующиеся `using` директивы в `AdminGuard.razor`
2. Заменены несуществующие Radzen компоненты (`RadzenPageLayout`, `RadzenCardHeader`, `RadzenCardContent`) на стандартные HTML элементы в `DebugLogs.razor`
3. Исправлен `RadzenTest.razor` - удален несуществующий компонент `RadzenTestComponent`

## Результат
Сборка проекта прошла успешно без ошибок и предупреждений.

## Документация
Решение основано на официальной документации Radzen Blazor Components и Microsoft Blazor по работе с параметрами контекста в компонентах.
