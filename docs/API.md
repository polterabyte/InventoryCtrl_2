# API Documentation

Документация RESTful API для системы управления инвентарем.

## 🔗 Base URL

- **Development**: https://localhost:7000/api
- **Swagger UI**: https://localhost:7000/swagger

## 🔐 Аутентификация

Все API endpoints требуют JWT аутентификации (кроме `/auth/login` и `/auth/register`).

### Заголовок авторизации
```http
Authorization: Bearer <jwt-token>
```

### Роли пользователей
- **Admin** — полный доступ ко всем операциям
- **User** — базовые операции с товарами
- **Manager** — управление товарами и складами

## 📋 API Endpoints

### Authentication

#### POST /api/auth/login
Вход в систему.

**Request:**
```json
{
  "username": "admin",
  "password": "Admin123!"
}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "refresh_token_here",
    "expiresAt": "2024-01-15T12:00:00Z",
    "user": {
      "id": "user_id",
      "username": "admin",
      "email": "admin@localhost",
      "role": "Admin"
    }
  }
}
```

#### POST /api/auth/register
Регистрация нового пользователя.

**Request:**
```json
{
  "username": "newuser",
  "email": "user@example.com",
  "password": "Password123!",
  "confirmPassword": "Password123!"
}
```

#### POST /api/auth/refresh
Обновление токена доступа.

**Request:**
```json
{
  "refreshToken": "refresh_token_here"
}
```

#### POST /api/auth/logout
Выход из системы.

### Dashboard

#### GET /api/dashboard/stats
Получение статистики дашборда.

**Response:**
```json
{
  "success": true,
  "data": {
    "totalProducts": 150,
    "totalCategories": 25,
    "totalManufacturers": 10,
    "totalWarehouses": 5,
    "lowStockProducts": 8,
    "outOfStockProducts": 3,
    "recentTransactions": 45,
    "recentProducts": 12
  }
}
```

#### GET /api/dashboard/recent-activity
Получение недавней активности.

**Response:**
```json
{
  "success": true,
  "data": {
    "recentTransactions": [
      {
        "id": 1,
        "productName": "Laptop Dell XPS 13",
        "productSku": "DELL-XPS13-001",
        "type": "Income",
        "quantity": 10,
        "date": "2024-01-15T10:30:00Z",
        "userName": "admin",
        "warehouseName": "Main Warehouse",
        "description": "New stock arrival"
      }
    ],
    "recentProducts": [
      {
        "id": 1,
        "name": "Laptop Dell XPS 13",
        "sku": "DELL-XPS13-001",
        "quantity": 50,
        "categoryName": "Electronics",
        "manufacturerName": "Dell",
        "createdAt": "2024-01-15T09:00:00Z"
      }
    ]
  }
}
```

#### GET /api/dashboard/low-stock-products
Получение товаров с низким запасом.

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "id": 1,
      "name": "Laptop Dell XPS 13",
      "sku": "DELL-XPS13-001",
      "currentQuantity": 5,
      "minStock": 10,
      "maxStock": 100,
      "categoryName": "Electronics",
      "manufacturerName": "Dell",
      "unit": "pcs"
    }
  ]
}
```

### Products

#### GET /api/products
Получение списка товаров с пагинацией и фильтрацией.

**Query Parameters:**
- `page` (int) — номер страницы (по умолчанию: 1)
- `pageSize` (int) — размер страницы (по умолчанию: 10)
- `search` (string) — поисковый запрос
- `categoryId` (int) — фильтр по категории
- `manufacturerId` (int) — фильтр по производителю
- `isActive` (bool) — фильтр по активности

**Response:**
```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": 1,
        "name": "Laptop Dell XPS 13",
        "sku": "DELL-XPS13-001",
        "description": "High-performance laptop",
        "quantity": 50,
        "unit": "pcs",
        "minStock": 10,
        "maxStock": 100,
        "isActive": true,
        "categoryId": 1,
        "categoryName": "Electronics",
        "manufacturerId": 1,
        "manufacturerName": "Dell",
        "productModelId": 1,
        "productModelName": "XPS 13",
        "productGroupId": 1,
        "productGroupName": "Laptops",
        "createdAt": "2024-01-15T09:00:00Z",
        "updatedAt": "2024-01-15T09:00:00Z"
      }
    ],
    "totalCount": 150,
    "pageNumber": 1,
    "pageSize": 10,
    "totalPages": 15
  }
}
```

#### GET /api/products/{id}
Получение товара по ID.

**Response:**
```json
{
  "success": true,
  "data": {
    "id": 1,
    "name": "Laptop Dell XPS 13",
    "sku": "DELL-XPS13-001",
    "description": "High-performance laptop",
    "quantity": 50,
    "unit": "pcs",
    "minStock": 10,
    "maxStock": 100,
    "isActive": true,
    "categoryId": 1,
    "manufacturerId": 1,
    "productModelId": 1,
    "productGroupId": 1,
    "createdAt": "2024-01-15T09:00:00Z",
    "updatedAt": "2024-01-15T09:00:00Z"
  }
}
```

#### GET /api/products/sku/{sku}
Получение товара по SKU.

#### POST /api/products
Создание нового товара.

**Request:**
```json
{
  "name": "New Product",
  "sku": "NEW-PRODUCT-001",
  "description": "Product description",
  "quantity": 0,
  "unit": "pcs",
  "minStock": 5,
  "maxStock": 50,
  "categoryId": 1,
  "manufacturerId": 1,
  "productModelId": 1,
  "productGroupId": 1,
  "note": "Additional notes"
}
```

#### PUT /api/products/{id}
Обновление товара.

#### DELETE /api/products/{id}
Удаление товара (только Admin).

#### POST /api/products/{id}/stock/adjust
Корректировка количества товара.

**Request:**
```json
{
  "quantity": 10,
  "type": "Income",
  "description": "Stock adjustment",
  "warehouseId": 1
}
```

### Categories

#### GET /api/categories
Получение списка категорий.

**Query Parameters:**
- `includeInactive` (bool) — включить неактивные категории
- `parentId` (int) — фильтр по родительской категории

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "id": 1,
      "name": "Electronics",
      "description": "Electronic devices",
      "parentCategoryId": null,
      "parentCategoryName": null,
      "isActive": true,
      "createdAt": "2024-01-15T09:00:00Z"
    }
  ]
}
```

#### GET /api/categories/{id}
Получение категории по ID.

#### GET /api/categories/root
Получение корневых категорий.

#### GET /api/categories/{parentId}/sub
Получение подкатегорий.

#### POST /api/categories
Создание новой категории (только Admin).

**Request:**
```json
{
  "name": "New Category",
  "description": "Category description",
  "parentCategoryId": 1,
  "isActive": true
}
```

#### PUT /api/categories/{id}
Обновление категории (только Admin).

#### DELETE /api/categories/{id}
Удаление категории (только Admin).

### Manufacturers

#### GET /api/manufacturers
Получение списка производителей.

#### GET /api/manufacturers/{id}
Получение производителя по ID.

#### POST /api/manufacturers
Создание нового производителя (только Admin).

**Request:**
```json
{
  "name": "New Manufacturer"
}
```

#### PUT /api/manufacturers/{id}
Обновление производителя (только Admin).

#### DELETE /api/manufacturers/{id}
Удаление производителя (только Admin).

### Warehouses

#### GET /api/warehouses
Получение списка складов.

#### GET /api/warehouses/{id}
Получение склада по ID.

#### POST /api/warehouses
Создание нового склада (только Admin).

**Request:**
```json
{
  "name": "New Warehouse",
  "location": "Warehouse location",
  "isActive": true
}
```

#### PUT /api/warehouses/{id}
Обновление склада (только Admin).

#### DELETE /api/warehouses/{id}
Удаление склада (только Admin).

### Unit of Measures

#### GET /api/unitofmeasure
Получение списка единиц измерения с пагинацией и фильтрацией.

**Query Parameters:**
- `page` (int) — номер страницы (по умолчанию: 1)
- `pageSize` (int) — размер страницы (по умолчанию: 10)
- `search` (string) — поисковый запрос по названию, символу или описанию
- `isActive` (bool) — фильтр по активности

**Response:**
```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": 1,
        "name": "Pieces",
        "symbol": "pcs",
        "description": "Individual items",
        "isActive": true,
        "createdAt": "2024-01-15T09:00:00Z",
        "updatedAt": "2024-01-15T09:00:00Z"
      }
    ],
    "totalCount": 10,
    "pageNumber": 1,
    "pageSize": 10,
    "totalPages": 1
  }
}
```

#### GET /api/unitofmeasure/{id}
Получение единицы измерения по ID.

**Response:**
```json
{
  "success": true,
  "data": {
    "id": 1,
    "name": "Pieces",
    "symbol": "pcs",
    "description": "Individual items",
    "isActive": true,
    "createdAt": "2024-01-15T09:00:00Z",
    "updatedAt": "2024-01-15T09:00:00Z"
  }
}
```

#### GET /api/unitofmeasure/all
Получение всех единиц измерения (для выпадающих списков).

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "id": 1,
      "name": "Pieces",
      "symbol": "pcs",
      "description": "Individual items",
      "isActive": true,
      "createdAt": "2024-01-15T09:00:00Z",
      "updatedAt": "2024-01-15T09:00:00Z"
    }
  ]
}
```

#### POST /api/unitofmeasure
Создание новой единицы измерения (только Admin).

**Request:**
```json
{
  "name": "Kilograms",
  "symbol": "kg",
  "description": "Weight measurement unit"
}
```

#### PUT /api/unitofmeasure/{id}
Обновление единицы измерения (только Admin).

**Request:**
```json
{
  "name": "Kilograms",
  "symbol": "kg",
  "description": "Weight measurement unit",
  "isActive": true
}
```

#### DELETE /api/unitofmeasure/{id}
Удаление единицы измерения (только Admin, soft delete).

**Response:**
```json
{
  "success": true,
  "data": {
    "message": "Unit of measure deleted successfully"
  }
}
```

### Reference Data (Generic)

#### GET /api/referencedata
Базовый endpoint для работы со справочными данными (generic controller).

**Query Parameters:**
- `page` (int) — номер страницы
- `pageSize` (int) — размер страницы
- `search` (string) — поисковый запрос
- `isActive` (bool) — фильтр по активности

#### GET /api/referencedata/all
Получение всех элементов справочных данных.

#### GET /api/referencedata/{id}
Получение элемента справочных данных по ID.

#### POST /api/referencedata
Создание нового элемента справочных данных (только Admin).

#### PUT /api/referencedata/{id}
Обновление элемента справочных данных (только Admin).

#### DELETE /api/referencedata/{id}
Удаление элемента справочных данных (только Admin).

#### GET /api/referencedata/exists
Проверка существования элемента по идентификатору.

**Query Parameters:**
- `identifier` (string) — идентификатор для проверки

#### GET /api/referencedata/count
Получение количества элементов.

**Query Parameters:**
- `isActive` (bool) — фильтр по активности

### Admin Reference Data Management

#### GET /admin/reference-data
Страница администратора для управления всеми справочниками.

**Доступ:** Только для роли Admin

**Функции:**
- Управление категориями (с иерархией)
- Управление производителями
- Управление единицами измерения
- Управление группами товаров
- Управление моделями товаров
- Управление складами

**Особенности:**
- Табы для переключения между типами справочников
- Поиск и фильтрация
- Пагинация
- Создание, редактирование, удаление
- Проверка связей перед удалением
- Soft delete для некоторых справочников

### Transactions

#### GET /api/transactions
Получение списка транзакций.

**Query Parameters:**
- `page` (int) — номер страницы
- `pageSize` (int) — размер страницы
- `productId` (int) — фильтр по товару
- `warehouseId` (int) — фильтр по складу
- `type` (string) — фильтр по типу (Income/Outcome/Install)
- `startDate` (datetime) — дата начала
- `endDate` (datetime) — дата окончания

#### GET /api/transactions/{id}
Получение транзакции по ID.

#### POST /api/transactions
Создание новой транзакции.

**Request:**
```json
{
  "productId": 1,
  "warehouseId": 1,
  "type": "Income",
  "quantity": 10,
  "description": "Stock received",
  "locationId": 1
}
```

## 📊 Общие типы данных

### ApiResponse<T>
Стандартный формат ответа API.

```json
{
  "success": true,
  "data": { /* данные */ },
  "errorMessage": null,
  "timestamp": "2024-01-15T10:30:00Z"
}
```

### UnitOfMeasureDto
DTO для единиц измерения.

```json
{
  "id": 1,
  "name": "Pieces",
  "symbol": "pcs",
  "description": "Individual items",
  "isActive": true,
  "createdAt": "2024-01-15T09:00:00Z",
  "updatedAt": "2024-01-15T09:00:00Z"
}
```

### CreateUnitOfMeasureDto
DTO для создания единицы измерения.

```json
{
  "name": "Kilograms",
  "symbol": "kg",
  "description": "Weight measurement unit"
}
```

### UpdateUnitOfMeasureDto
DTO для обновления единицы измерения.

```json
{
  "name": "Kilograms",
  "symbol": "kg",
  "description": "Weight measurement unit",
  "isActive": true
}
```

### PagedResponse<T>
Формат ответа с пагинацией.

```json
{
  "success": true,
  "data": {
    "items": [ /* массив элементов */ ],
    "totalCount": 150,
    "pageNumber": 1,
    "pageSize": 10,
    "totalPages": 15
  }
}
```

### Error Response
Формат ответа об ошибке.

```json
{
  "success": false,
  "data": null,
  "errorMessage": "Validation failed",
  "errors": {
    "name": ["Name is required"],
    "sku": ["SKU must be unique"]
  },
  "timestamp": "2024-01-15T10:30:00Z"
}
```

## 🔒 Коды ответов

### Успешные ответы
- **200 OK** — успешный запрос
- **201 Created** — ресурс создан
- **204 No Content** — успешный запрос без содержимого

### Ошибки клиента
- **400 Bad Request** — некорректный запрос
- **401 Unauthorized** — не авторизован
- **403 Forbidden** — нет доступа
- **404 Not Found** — ресурс не найден
- **409 Conflict** — конфликт данных
- **422 Unprocessable Entity** — ошибка валидации

### Ошибки сервера
- **500 Internal Server Error** — внутренняя ошибка сервера
- **503 Service Unavailable** — сервис недоступен

## 🧪 Тестирование API

### Swagger UI
Откройте https://localhost:7000/swagger для интерактивного тестирования API.

### Примеры запросов с curl

#### Авторизация
```bash
# Вход в систему
curl -X POST "https://localhost:7000/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"Admin123!"}'

# Использование токена
curl -X GET "https://localhost:7000/api/products" \
  -H "Authorization: Bearer <jwt-token>"
```

#### Работа с товарами
```bash
# Получение товаров
curl -X GET "https://localhost:7000/api/products?page=1&pageSize=10" \
  -H "Authorization: Bearer <jwt-token>"

# Создание товара
curl -X POST "https://localhost:7000/api/products" \
  -H "Authorization: Bearer <jwt-token>" \
  -H "Content-Type: application/json" \
  -d '{"name":"Test Product","sku":"TEST-001","quantity":0,"categoryId":1,"manufacturerId":1}'
```

## 📈 Мониторинг и метрики

### Health Check
```http
GET /health
```

### Логирование
Все API запросы логируются с использованием Serilog:
- **Request/Response** — HTTP запросы и ответы
- **Authentication** — события аутентификации
- **Errors** — ошибки и исключения
- **Performance** — метрики производительности

### Rate Limiting
API защищен от злоупотреблений:
- **100 requests/minute** — лимит на пользователя
- **1000 requests/minute** — лимит на IP адрес

---

> 💡 **Совет**: Используйте Swagger UI для интерактивного тестирования API и изучения схем данных.
