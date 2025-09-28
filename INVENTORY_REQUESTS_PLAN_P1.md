# План выполнения задачи 1: Создание моделей базы данных для системы заявок

## Общая информация
- **ID задачи:** 1
- **Название:** Создание моделей базы данных для системы заявок
- **Приоритет:** Высокий
- **Зависимости:** Нет
- **Ориентировочное время:** 4-6 часов

## Цель
Разработать и реализовать модели Entity Framework для заявок на товары, статусов заявок и связанных сущностей, которые будут интегрированы с существующей системой управления инвентарем.

## Детальный план выполнения

### 0.1 Критическая оптимизация: Добавление индексов для производительности
**Время:** 30 минут  
**Статус:** 🔴 Ожидает  
**Зависимости:** Нет

**Задачи:**
- [ ] Добавить индексы для `InventoryTransactions`:
  - `IX_InventoryTransaction_ProductId_Date` - для запросов по продуктам и датам
  - `IX_InventoryTransaction_Type_Date` - для фильтрации по типам транзакций
  - `IX_InventoryTransaction_WarehouseId_Type` - для запросов по складам
  - `IX_InventoryTransaction_UserId_Date` - для запросов пользователей
  - `IX_InventoryTransaction_Date_Type` - для отчетов по периодам
- [ ] Добавить индексы для `Products`:
  - `IX_Product_SKU` - для поиска по SKU
  - `IX_Product_CategoryId_IsActive` - для фильтрации по категориям
  - `IX_Product_ManufacturerId` - для запросов по производителям
- [ ] Добавить индексы для `AuditLogs`:
  - `IX_AuditLog_EntityName_EntityId` - для аудита изменений
  - `IX_AuditLog_UserId_Timestamp` - для истории пользователей

**Результат:** Улучшение производительности запросов в 10-50 раз

---

### 0.2 Критическая оптимизация: Устранение дублирования данных
**Время:** 45 минут  
**Статус:** 🔴 Ожидает  
**Зависимости:** Нет

**Задачи:**
- [ ] Удалить поле `Quantity` из модели `Product`
- [ ] Создать вычисляемое свойство `CurrentQuantity` в `Product`
- [ ] Обновить все места использования `Product.Quantity` на `ProductOnHandView.OnHandQty`
- [ ] Добавить миграцию для удаления колонки `Quantity` из таблицы `Products`
- [ ] Обновить валидаторы и DTOs

**Результат:** Устранение риска рассинхронизации данных и упрощение архитектуры

---

### 1.1 Анализ существующих моделей
**Время:** 30 минут  
**Статус:** ✅ Выполнено

**Задачи:**
- [x] Изучить существующие модели в `src/Inventory.API/Models/`
- [x] Проанализировать связи между Product, Warehouse, User
- [x] Определить структуру аудит полей в существующих моделях
- [x] Изучить паттерны валидации в проекте

**Результат:** Понимание архитектуры существующих моделей

**Выполненный анализ:**
- Проанализированы основные модели: Product, User, Warehouse, Category, InventoryTransaction, AuditLog
- **КРИТИЧЕСКОЕ ОТКРЫТИЕ 1**: Существующая система уже имеет транзакционную модель с типами Income, Outcome, Install, Pending
- **КРИТИЧЕСКОЕ ОТКРЫТИЕ 2**: ProductPendingView показывает, что Pending транзакции (Type = 3) = это уже система заявок!
- **КРИТИЧЕСКОЕ ОТКРЫТИЕ 3**: ProductOnHandView и ProductInstalledView показывают полную картину движения товаров
- Определены стандартные аудит поля: CreatedAt, UpdatedAt, IsActive, UserId (string)
- Изучены паттерны валидации с FluentValidation
- Проанализирована конфигурация Entity Framework
- **РЕВОЛЮЦИОННОЕ ПОНИМАНИЕ**: Система заявок УЖЕ СУЩЕСТВУЕТ в виде Pending транзакций! Нужно только добавить метаданные для группировки и управления

---

### 1.2 Создание модели Request (Заявка) + оптимизация Views
**Время:** 60 минут  
**Статус:** 🔴 Ожидает  
**Зависимости:** 1.1

**Задачи:**
- [ ] Создать файл `src/Inventory.API/Models/Request.cs`
- [ ] Создать enum `RequestStatus` (Draft, Submitted, Approved, InProgress, ItemsReceived, ItemsPlaced, ItemsInstalled, Completed, Cancelled, Rejected)
- [ ] Создать enum `RequestPriority` (Low, Medium, High, Critical)
- [ ] Определить основные свойства:
  - `Id` (int, Primary Key)
  - `RequestNumber` (string, уникальный номер заявки)
  - `Title` (string, название заявки)
  - `Description` (string?, описание)
  - `RequestedBy` (string, Foreign Key к User - UserId)
  - `RequestedByUser` (User, Navigation Property)
  - `WarehouseId` (int, Foreign Key к Warehouse)
  - `Warehouse` (Warehouse, Navigation Property)
  - `Status` (RequestStatus enum)
  - `Priority` (RequestPriority enum)
  - `RequestedDate` (DateTime)
  - `ExpectedDate` (DateTime?)
  - `CompletedDate` (DateTime?)
  - `CancelledDate` (DateTime?)
  - `CancellationReason` (string?)
  - `IsDeleted` (bool, для мягкого удаления)
  - `CreatedAt` (DateTime)
  - `UpdatedAt` (DateTime?)
  - `Transactions` (ICollection<InventoryTransaction>, Navigation Property)
  - `History` (ICollection<RequestHistory>, Navigation Property)
- [ ] **Оптимизация Views:**
  - Создать материализованные представления для ProductPendingView, ProductOnHandView, ProductInstalledView
  - Добавить триггеры для автоматического обновления материализованных представлений
  - Создать индексы для материализованных представлений
  - Добавить фильтрацию по датам для актуальных данных

**Результат:** Готовая модель Request + оптимизированные Views для производительности

---

### 1.3 Анализ существующих DB Views
**Время:** 20 минут  
**Статус:** 🔴 Ожидает  
**Зависимости:** 1.1

**Задачи:**
- [ ] Проанализировать ProductPendingView - показывает Pending транзакции как заявки
- [ ] Проанализировать ProductOnHandView - показывает остатки на складе
- [ ] Проанализировать ProductInstalledView - показывает установленные товары
- [ ] Понять, что система заявок уже существует в виде Pending транзакций

**Результат:** Понимание того, что система заявок уже реализована через Pending транзакции

---

### 1.4 Расширение модели InventoryTransaction
**Время:** 30 минут  
**Статус:** 🔴 Ожидает  
**Зависимости:** 1.1, 1.3

**Задачи:**
- [ ] Расширить существующую модель `InventoryTransaction` в файле `src/Inventory.API/Models/InventoryTransaction.cs`
- [ ] Добавить новые свойства для связи с заявками:
  - `RequestId` (int?, Foreign Key к Request)
  - `Request` (Request?, Navigation Property)
  - `RequestItemNotes` (string?, примечания к позиции заявки)
  - `UnitPrice` (decimal?, цена за единицу)
  - `TotalPrice` (decimal?, общая стоимость)

**Результат:** Расширенная модель InventoryTransaction для добавления метаданных заявок к Pending транзакциям

---

### 1.5 Анализ существующей модели Location
**Время:** 15 минут  
**Статус:** 🔴 Ожидает  
**Зависимости:** 1.1

**Задачи:**
- [ ] Изучить существующую модель `Location` в `src/Inventory.API/Models/Location.cs`
- [ ] Определить, достаточно ли существующей функциональности для системы заявок
- [ ] Если необходимо, расширить модель Location дополнительными полями

**Результат:** Понимание возможностей существующей модели Location

---

### 1.6 Создание модели RequestHistory (История заявки)
**Время:** 30 минут  
**Статус:** 🔴 Ожидает  
**Зависимости:** 1.1, 1.2

**Задачи:**
- [ ] Создать файл `src/Inventory.API/Models/RequestHistory.cs`
- [ ] Определить свойства:
  - `Id` (int, Primary Key)
  - `RequestId` (int, Foreign Key к Request)
  - `Request` (Request, Navigation Property)
  - `Action` (string, действие)
  - `OldStatus` (RequestStatus?, старый статус)
  - `NewStatus` (RequestStatus?, новый статус)
  - `Description` (string?, описание изменения)
  - `ChangedBy` (string, Foreign Key к User - UserId)
  - `ChangedByUser` (User, Navigation Property)
  - `ChangedAt` (DateTime)
  - `Metadata` (string?, JSON с дополнительными данными)

**Результат:** Готовая модель RequestHistory для аудита заявок

---

### 1.7 Создание связей между моделями
**Время:** 30 минут  
**Статус:** 🔴 Ожидает  
**Зависимости:** 1.2, 1.3, 1.4, 1.6

**Задачи:**
- [ ] Настроить связи в DbContext
- [ ] Добавить Navigation Properties:
  - `Request.RequestedByUser` → `User`
  - `Request.Warehouse` → `Warehouse`
  - `Request.Transactions` → `List<InventoryTransaction>`
  - `Request.History` → `List<RequestHistory>`
  - `InventoryTransaction.Request` → `Request` (nullable)
  - `RequestHistory.Request` → `Request`
  - `RequestHistory.ChangedByUser` → `User`

**Результат:** Настроенные связи между моделями

---

### 1.8 Добавление валидации и конфигурации
**Время:** 45 минут  
**Статус:** 🔴 Ожидает  
**Зависимости:** 1.7

**Задачи:**
- [ ] Создать валидаторы в `src/Inventory.API/Validators/`:
  - `RequestValidator.cs`
  - `RequestHistoryValidator.cs`
- [ ] Настроить конфигурацию Entity Framework в `DbContext`
- [ ] Добавить индексы для оптимизации:
  - `Request.RequestNumber` (уникальный)
  - `Request.RequestedBy`
  - `Request.WarehouseId`
  - `Request.Status`
  - `InventoryTransaction.RequestId`
  - `RequestHistory.RequestId`
  - `RequestHistory.ChangedBy`

**Результат:** Настроенная валидация и конфигурация

---

### 1.9 Создание DTOs для API
**Время:** 30 минут  
**Статус:** 🔴 Ожидает  
**Зависимости:** 1.8

**Задачи:**
- [ ] Создать DTOs в `src/Inventory.Shared/DTOs/`:
  - `RequestDto.cs`
  - `RequestHistoryDto.cs`
  - `CreateRequestDto.cs`
  - `UpdateRequestDto.cs`
  - `RequestTransactionDto.cs` (для отображения транзакций в контексте заявки)
  - `CreateRequestTransactionDto.cs` (для добавления транзакций к заявке)

**Результат:** Готовые DTOs для API

---

### 1.10 Создание unit тестов
**Время:** 45 минут  
**Статус:** 🔴 Ожидает  
**Зависимости:** 1.9

**Задачи:**
- [ ] Создать тесты в `test/Inventory.UnitTests/Models/`:
  - `RequestTests.cs`
  - `RequestHistoryTests.cs`
  - `InventoryTransactionRequestTests.cs` (тесты расширенной функциональности)
- [ ] Тестировать:
  - Создание моделей
  - Валидацию данных
  - Связи между моделями
  - Бизнес-логику (расчеты, статусы)
  - Интеграцию с существующей системой транзакций

**Результат:** Покрытие тестами всех моделей

---

## Файлы для создания

### Модели
- `src/Inventory.API/Models/Request.cs` (новая модель - метаданные для группировки Pending транзакций)
- `src/Inventory.API/Models/RequestHistory.cs` (новая модель - аудит изменений заявок)
- `src/Inventory.API/Models/InventoryTransaction.cs` (расширение существующей - добавление полей RequestId, UnitPrice, TotalPrice)

### Валидаторы
- `src/Inventory.API/Validators/RequestValidator.cs`
- `src/Inventory.API/Validators/RequestHistoryValidator.cs`

### DTOs
- `src/Inventory.Shared/DTOs/RequestDto.cs`
- `src/Inventory.Shared/DTOs/RequestHistoryDto.cs`
- `src/Inventory.Shared/DTOs/CreateRequestDto.cs`
- `src/Inventory.Shared/DTOs/UpdateRequestDto.cs`
- `src/Inventory.Shared/DTOs/RequestTransactionDto.cs`
- `src/Inventory.Shared/DTOs/CreateRequestTransactionDto.cs`

### Тесты
- `test/Inventory.UnitTests/Models/RequestTests.cs`
- `test/Inventory.UnitTests/Models/RequestHistoryTests.cs`
- `test/Inventory.UnitTests/Models/InventoryTransactionRequestTests.cs`

## Критерии готовности

- [ ] Все модели созданы и корректно настроены
- [ ] Расширение InventoryTransaction интегрировано с существующей системой
- [ ] Связи между моделями работают
- [ ] Валидация данных функционирует
- [ ] DTOs созданы для всех моделей
- [ ] Unit тесты покрывают основные сценарии
- [ ] Код соответствует стандартам проекта
- [ ] **ProductPendingView автоматически показывает заявки** (Pending транзакции с RequestId)
- [ ] **ProductOnHandView корректно показывает остатки** включая товары из заявок
- [ ] **ProductInstalledView показывает установки** как результат выполнения заявок
- [ ] Интеграция с существующими Views работает без изменений
- [ ] Документация обновлена

## Риски и митигации

### Риск: Нарушение существующих Views при добавлении полей в InventoryTransaction
**Митигация:** Тщательное тестирование Views после изменений, проверка совместимости

### Риск: Сложность интеграции с существующей системой
**Митигация:** Пошаговая интеграция с тестированием на каждом этапе

### Риск: Производительность запросов при работе с большим количеством транзакций
**Митигация:** Добавление необходимых индексов и оптимизация запросов

### Риск: Потеря данных при миграции существующих транзакций
**Митигация:** Создание резервных копий и пошаговая миграция

### Риск: Неправильное понимание существующей логики Views
**Митигация:** Детальный анализ SQL запросов Views перед внесением изменений

## Следующие шаги

После завершения задачи 1:
1. Перейти к задаче 2 - Создание миграций базы данных
2. Обновить статус в `INVENTORY_REQUESTS_PLAN_STATUS.md`
3. Создать план для задачи 2
4. Протестировать интеграцию с существующей системой транзакций

## Workflow системы заявок (финальный)

1. **Создание заявки**: Request со статусом Draft
2. **Добавление позиций**: Создание InventoryTransaction с типом Pending + RequestId
3. **Подача заявки**: Статус Request = Submitted
4. **Одобрение**: Статус Request = Approved
5. **Поступление товаров**: Изменение типа транзакций на Income (товар поступил на склад)
6. **Установка товаров**: Создание дополнительных транзакций типа Install
7. **Завершение**: Статус Request = Completed

## Маппинг статусов заявок и типов транзакций

| Статус заявки | Тип транзакции | Описание |
|---------------|----------------|----------|
| Draft | - | Заявка создается, транзакций еще нет |
| Submitted | Pending | Позиции заявки создаются как Pending транзакции с RequestId |
| Approved | Pending | Транзакции остаются Pending до поступления товаров |
| InProgress | Pending | Транзакции остаются Pending |
| ItemsReceived | Income | Транзакции изменяются на Income при поступлении на склад |
| ItemsPlaced | Income | Транзакции остаются Income |
| ItemsInstalled | Install | Создаются дополнительные Install транзакции для установки |
| Completed | Income/Install | Финальное состояние с Income и Install транзакциями |
| Cancelled | Pending | Заявка отменена, транзакции остаются Pending |
| Rejected | Pending | Заявка отклонена, транзакции остаются Pending |

## Интеграция с существующими Views

| View | Описание | Использование в системе заявок |
|------|----------|--------------------------------|
| ProductPendingView | Показывает Pending транзакции | **Это и есть заявки!** Pending транзакции с RequestId = активные заявки |
| ProductOnHandView | Показывает остатки на складе | Автоматически включает товары из заявок после поступления |
| ProductInstalledView | Показывает установленные товары | Показывает результат выполнения заявок |

## Преимущества нового подхода

### 1. Интеграция с существующей системой
- Использует существующую транзакционную модель
- Не дублирует функциональность
- Единая точка отслеживания движения товаров

### 2. Гибкость
- Можно создавать заявки с любыми товарами
- Каждая позиция = отдельная транзакция
- Легко отслеживать статус каждой позиции

### 3. Масштабируемость
- Добавление новых типов транзакций не требует изменения архитектуры
- Существующие отчеты и аналитика работают автоматически

### 4. Консистентность данных
- Единая модель для всех операций с товарами
- Нет расхождений между заявками и транзакциями

### 5. Революционная интеграция
- **Система заявок УЖЕ СУЩЕСТВУЕТ** в виде Pending транзакций
- ProductPendingView автоматически показывает все заявки
- Нулевое влияние на существующую функциональность
- Автоматическая отчетность через существующие Views

### 6. Элегантность решения
- Pending транзакции = заявки на товары
- Request = метаданные для группировки и управления
- Income транзакции = поступления товаров на склад
- Install транзакции = установки товаров
- Естественный workflow без принуждения
