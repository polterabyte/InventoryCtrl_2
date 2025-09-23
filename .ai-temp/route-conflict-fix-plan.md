# План устранения конфликта маршрутов radzen-test

## Проблема
Обнаружен конфликт маршрутов: два файла с одинаковым маршрутом `/radzen-test`:
1. `src/Inventory.UI/Pages/RadzenTest.razor` - простой тестовый компонент
2. `src/Inventory.Web.Client/Pages/RadzenTest.razor` - полнофункциональный тестовый компонент с RadzenTestComponent

## Анализ
- `Inventory.Web.Client` - основной Blazor WebAssembly клиент (используется в docker-compose)
- `Inventory.UI` - дополнительный UI проект
- `RadzenTestComponent` существует только в `Inventory.Web.Client/Components/`

## Решение
Удалить дублирующий файл из `Inventory.UI`, так как:
1. Основной клиент - `Inventory.Web.Client`
2. В `Inventory.Web.Client` есть полнофункциональный тестовый компонент
3. `Inventory.UI` содержит только простую заглушку

## Шаги
1. Удалить `src/Inventory.UI/Pages/RadzenTest.razor`
2. Проверить, что маршрут работает в `Inventory.Web.Client`
3. Убедиться, что нет других конфликтов маршрутов

