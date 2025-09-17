# Исправление проблемы с пустым списком категорий

## Проблема
На странице http://localhost:5001/admin/reference-data вкладка Categories показывает пустой список, хотя в базе данных категории присутствуют.

## Обнаруженные причины
1. **Проблема с аутентификацией**: Пользователь не может войти как администратор
2. **Проблема с фильтрацией по ролям**: CategoryController неправильно определял роли пользователя

## Внесенные исправления

### 1. Исправлена логика определения ролей в CategoryController
```csharp
// Старый код (неправильный):
var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
if (userRole != "Admin")

// Новый код (правильный):
var userRoles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
var isAdmin = userRoles.Contains("Admin") || userRoles.Contains("SuperUser");
if (!isAdmin)
```

### 2. Добавлено подробное логирование для отладки
- В CategoryController добавлены логи пользователя и количества найденных категорий
- В AuthController добавлены логи процесса аутентификации
- В CategoryApiService добавлены логи API запросов
- В DbInitializer добавлены логи создания суперпользователя

## Учетные данные суперпользователя
По умолчанию система создает суперпользователя с следующими данными:
- **Email**: admin@localhost
- **Username**: superadmin
- **Password**: Admin123!
- **Roles**: SuperUser, Admin

## Как исправить проблему

### Шаг 1: Перезапустите API сервер
```bash
cd src/Inventory.API
dotnet run
```

### Шаг 2: Проверьте логи создания суперпользователя
В логах должно появиться сообщение:
```
Superuser created: admin@localhost with username: superadmin and password: Admin123!
```
или
```
Superuser already exists: admin@localhost with username: superadmin
```

### Шаг 3: Войдите в систему как администратор
1. Перейдите на http://localhost:5001
2. Используйте следующие учетные данные:
   - **Username**: superadmin
   - **Password**: Admin123!

### Шаг 4: Проверьте страницу администрирования
1. После успешного входа перейдите на http://localhost:5001/admin/reference-data
2. Откройте вкладку "Categories"
3. Теперь список категорий должен отображаться

### Шаг 5: Проверьте логи для отладки
В логах API должны появиться сообщения:
```
GetCategories called by user: [UserId], Name: [UserName], Roles: Admin, SuperUser
Found [X] categories after filtering
Returning [X] categories for page 1
```

## Дополнительные проверки

### Если проблема с аутентификацией продолжается:
1. Проверьте логи API на предмет ошибок аутентификации
2. Убедитесь, что база данных инициализирована правильно
3. Проверьте, что CORS настроен правильно для портов 5001 и 7000

### Если список категорий все еще пуст:
1. Проверьте, что в базе данных есть категории с IsActive = true
2. Проверьте логи CategoryController для понимания процесса фильтрации
3. Проверьте, что API возвращает данные (проверьте логи CategoryApiService)

## Проверка базы данных
Для проверки категорий в базе данных выполните SQL запрос:
```sql
SELECT Id, Name, Description, IsActive, ParentCategoryId, CreatedAt 
FROM "Categories" 
ORDER BY CreatedAt DESC;
```

Если категории есть, но IsActive = false, их можно активировать:
```sql
UPDATE "Categories" SET "IsActive" = true WHERE "IsActive" = false;
```

## Контакты для поддержки
Если проблема не решена, предоставьте:
1. Логи API сервера
2. Результат SQL запроса к таблице Categories
3. Скриншоты ошибок в браузере (F12 -> Console)
