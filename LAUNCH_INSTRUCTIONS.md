# Инструкции по запуску Inventory Control

## Автоматический запуск

### Windows (PowerShell) - Рекомендуется
```powershell
# Полная версия с проверками (английский текст)
.\start-apps-en.ps1

# Или русская версия (может быть проблема с кодировкой)
.\start-apps.ps1
```

### Windows (Command Prompt)
```cmd
start-apps.bat
```

### Linux/Mac
```bash
./start-apps.sh
```

### Быстрый запуск (без проверок)
```powershell
# Английская версия
.\quick-start-en.ps1

# Русская версия
.\quick-start.ps1
```

## Ручной запуск

### 1. Запуск API сервера
```bash
cd src/Inventory.API
dotnet run
```
**Порты:**
- HTTPS: https://localhost:7000
- HTTP: http://localhost:5000

### 2. Запуск Web клиента (в новом терминале)
```bash
cd src/Inventory.Web
dotnet run
```
**Порты:**
- HTTPS: https://localhost:5001
- HTTP: http://localhost:5142

## Доступ к приложению

После запуска откройте браузер и перейдите по адресу:
- **Web приложение**: https://localhost:5001
- **API документация**: https://localhost:7000/swagger (если настроен)

## Остановка приложений

- **Автоматический запуск**: Нажмите `Ctrl+C` в терминале
- **Ручной запуск**: Нажмите `Ctrl+C` в каждом терминале

## Требования

- .NET 9.0 SDK
- PostgreSQL (для базы данных)
- Браузер с поддержкой HTTPS

## Устранение неполадок

1. **Порт занят**: Убедитесь, что порты 5000, 5001, 5142, 7000 свободны
2. **CORS ошибки**: Проверьте настройки CORS в `appsettings.json`
3. **База данных**: Убедитесь, что PostgreSQL запущен и доступен
