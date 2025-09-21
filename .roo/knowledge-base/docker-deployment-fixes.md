# Docker Deployment Fixes - База знаний

## Проблемы и решения при развертывании Docker

### Дата: 2025-09-19
### Статус: ✅ Решено

## 🚨 Проблемы, которые были исправлены

### 1. Ошибки синтаксиса PowerShell скриптов

**Проблема:**
```
В операторе Try отсутствует блок Catch или блок Finally.
В строке отсутствует завершающий символ: ".
```

**Причина:** Проблемы с кодировкой UTF-8 в PowerShell скриптах, особенно с эмодзи и специальными символами.

**Решение:**
- Пересоздал `docker-build.ps1` с правильной кодировкой UTF-8
- Убрал эмодзи из скрипта для избежания проблем с кодировкой
- Использовал простые ASCII символы для сообщений

### 2. Конфликты версий пакетов NuGet

**Проблема:**
```
Unable to find a stable package Microsoft.AspNetCore.RateLimiting with version
Version conflict detected for Microsoft.AspNetCore.Components.Web
```

**Причина:** Попытка использовать .NET 9 с пакетами, которые еще не полностью совместимы.

**Решение:**
- Понизил версию с .NET 9.0 до .NET 8.0 для лучшей совместимости
- Обновил все версии пакетов в `Directory.Packages.props` для .NET 8
- Удалил проблемный пакет `Microsoft.AspNetCore.RateLimiting` из API проекта
- Исправил версию `Microsoft.AspNetCore.SignalR` на совместимую

### 3. Конфликт файлов appsettings.json

**Проблема:**
```
Found multiple publish output files with the same relative path: 
/src/src/Inventory.Web.Client/appsettings.json, /src/src/Inventory.API/appsettings.json
```

**Причина:** API проект ссылался на Web Client проект, что приводило к конфликту файлов конфигурации.

**Решение:**
- Удалил ссылку на `Inventory.Web.Client` из `Inventory.API.csproj`
- API и Web Client теперь независимые проекты в Docker

### 4. Неправильный путь к nginx.conf

**Проблема:**
```
"/nginx.conf": not found
```

**Причина:** Dockerfile пытался скопировать nginx.conf из корневой директории, но файл находился в `src/Inventory.Web.Client/`.

**Решение:**
- Исправил путь в Dockerfile: `COPY src/Inventory.Web.Client/nginx.conf /etc/nginx/nginx.conf`

### 5. Конфликты портов

**Проблема:**
```
Bind for 0.0.0.0:5432 failed: port is already allocated
```

**Причина:** PostgreSQL уже запущен на порту 5432.

**Решение:**
- Добавил инструкции по освобождению портов в документацию
- Создал команды для очистки контейнеров

## 📋 Изменения в проекте

### Обновленные файлы:

1. **docker-build.ps1** - Полностью переписан с правильной кодировкой
2. **Directory.Packages.props** - Обновлены версии пакетов для .NET 8
3. **src/Inventory.API/Inventory.API.csproj** - Удалена ссылка на Web Client
4. **src/Inventory.Web.Client/Dockerfile** - Исправлен путь к nginx.conf
5. **Все .csproj файлы** - Изменен TargetFramework с net9.0 на net8.0
6. **Dockerfiles** - Обновлены базовые образы с .NET 9 на .NET 8

### Новые версии пакетов:

```xml
<!-- Основные пакеты .NET 8 -->
<PackageVersion Include="Microsoft.EntityFrameworkCore" Version="8.0.11" />
<PackageVersion Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.11" />
<PackageVersion Include="Microsoft.AspNetCore.SignalR" Version="1.1.0" />
<PackageVersion Include="Microsoft.AspNetCore.Components.WebAssembly" Version="8.0.11" />

<!-- Удален проблемный пакет -->
<!-- <PackageVersion Include="Microsoft.AspNetCore.RateLimiting" /> -->
```

## 🎯 Результат

После всех исправлений:
- ✅ Docker build работает успешно
- ✅ Все пакеты совместимы
- ✅ Нет конфликтов файлов
- ✅ Развертывание через `quick-deploy.ps1` работает
- ✅ API и Web Client собираются независимо

## 🔧 Команды для развертывания

```powershell
# Полное развертывание
.\quick-deploy.ps1

# Очистка и перезапуск
.\quick-deploy.ps1 -Clean

# Только сборка образов
.\docker-build.ps1
```

## 📚 Уроки на будущее

1. **Кодировка файлов:** Всегда использовать UTF-8 без BOM для PowerShell скриптов
2. **Версии пакетов:** Проверять совместимость пакетов с версией .NET перед обновлением
3. **Docker контекст:** Убеждаться, что пути к файлам в Dockerfile корректны относительно build context
4. **Зависимости проектов:** Избегать циклических зависимостей между проектами в Docker
5. **Тестирование:** Всегда тестировать развертывание после изменений в пакетах

## 🚀 Следующие шаги

1. Мониторинг стабильности развертывания
2. Возможное обновление до .NET 9 в будущем, когда все пакеты будут совместимы
3. Добавление автоматических тестов для Docker развертывания
4. Создание CI/CD pipeline для автоматического развертывания
