# CI/CD Quick Start Guide

## 🚀 Быстрый старт

### 1. GitHub Actions (Рекомендуется)

Просто загрузите код в GitHub - workflow автоматически запустится при:
- Push в ветки `main` или `develop`
- Создании Pull Request
- Еженедельно по расписанию

**Что происходит автоматически:**
- ✅ Запуск всех тестов (Unit, Integration, Component)
- ✅ Генерация отчетов о покрытии кода
- ✅ Сканирование безопасности
- ✅ Сборка приложений
- ✅ Публикация результатов

### 2. Локальный запуск тестов

```powershell
# Простой запуск
cd test
.\run-tests.ps1

# С покрытием кода
.\run-tests.ps1 -Coverage

# Конкретный тип тестов
.\run-tests.ps1 -Project unit
```

### 3. Docker тестирование

```powershell
# Установите Docker Desktop
# Запустите тесты в контейнерах
.\scripts\Run-Tests-Docker.ps1

# С покрытием кода
.\scripts\Run-Tests-Docker.ps1 -Coverage
```

### 4. Генерация отчетов

```powershell
# HTML отчет с автоматическим открытием
.\scripts\Generate-Coverage-Report.ps1 -OpenReport

# JSON отчет
.\scripts\Generate-Coverage-Report.ps1 -OutputFormat json
```

## 📊 Мониторинг

### GitHub Actions
- Перейдите в раздел **Actions** вашего репозитория
- Просматривайте результаты тестов и отчеты
- Настройте уведомления о статусе

### Azure DevOps
1. Импортируйте проект в Azure DevOps
2. Создайте pipeline из файла `azure-pipelines.yml`
3. Запустите pipeline

### Отчеты о покрытии
- HTML отчеты сохраняются в папке `coverage-reports/`
- Интеграция с Codecov для GitHub
- TRX файлы для детального анализа

## 🛠 Настройка

### Переменные окружения
```bash
# Для интеграционных тестов
ConnectionStrings__DefaultConnection="Host=localhost;Database=inventory_test;Username=postgres;Password=postgres"
```

### Исключения из покрытия
Настройте в `test/coverlet.runsettings`:
- Тестовые проекты
- Миграции Entity Framework
- Автогенерированные файлы

## 🔧 Troubleshooting

### Проблемы с Docker
```powershell
# Очистка контейнеров
.\scripts\Run-Tests-Docker.ps1 -Clean

# Проверка статуса Docker
docker ps
```

### Проблемы с покрытием
```powershell
# Переустановка инструментов
dotnet tool install --global dotnet-reportgenerator-globaltool --force

# Проверка файлов покрытия
Get-ChildItem test-results -Recurse -Filter "*.xml"
```

### Проблемы с тестами
```powershell
# Подробный вывод
.\run-tests.ps1 -Verbose

# Только один тип тестов
.\run-tests.ps1 -Project unit -Verbose
```

## 📈 Метрики качества

### Целевые показатели
- **Покрытие кода**: > 80%
- **Время выполнения тестов**: < 5 минут
- **Успешность тестов**: 100%

### Отслеживание
- GitHub Actions показывает статус в реальном времени
- Отчеты о покрытии обновляются при каждом запуске
- Уведомления о критических изменениях

## 🎯 Следующие шаги

1. **Настройте уведомления** в Slack/Teams
2. **Интегрируйте SonarQube** для анализа качества
3. **Добавьте performance тесты** для критических функций
4. **Настройте автоматическое развертывание** в staging

---

> 📖 **Полная документация**: [.ai-agents/reports/cicd-setup-report.md](.ai-agents/reports/cicd-setup-report.md)
> 
> 🧪 **Тестирование**: [test/README.md](test/README.md)
