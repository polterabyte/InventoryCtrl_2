# Быстрые исправления - Справочник

## 🚨 Критические проблемы и их решения

### 1. Docker не запускается
```powershell
# Проблема: Docker Desktop не отвечает
# Решение: Перезапустить Docker Desktop
# Время: 30 секунд
```

### 2. Порт PostgreSQL занят
```powershell
# Проблема: Port 5432 already in use
# Решение:
netstat -ano | findstr :5432
taskkill /F /PID <PID>
# Время: 10 секунд
```

### 3. PowerShell скрипты не работают
```powershell
# Проблема: Execution policy error
# Решение:
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
# Время: 5 секунд
```

### 4. Ошибки сборки Docker
```powershell
# Проблема: Build failed
# Решение:
docker-compose down -v --remove-orphans
.\quick-deploy.ps1 -Clean
# Время: 2 минуты
```

### 5. Пакеты NuGet не восстанавливаются
```powershell
# Проблема: Package restore failed
# Решение:
dotnet nuget locals all --clear
dotnet restore
# Время: 1 минута
```

## ⚡ Команды для экстренного восстановления

### Полная очистка и перезапуск
```powershell
# Остановить все
docker-compose down -v --remove-orphans
docker system prune -a

# Очистить .NET кэш
dotnet nuget locals all --clear
dotnet clean

# Перезапустить
.\quick-deploy.ps1 -Clean
```

### Проверка системы
```powershell
# Проверить Docker
docker version

# Проверить .NET
dotnet --version

# Проверить порты
netstat -ano | findstr ":80\|:5000\|:5432"

# Проверить процессы
Get-Process dotnet
```

## 🔧 Частые проблемы

### Проблема: "Container name already in use"
**Решение:**
```powershell
docker container prune -f
```

### Проблема: "Port already allocated"
**Решение:**
```powershell
# Найти процесс
netstat -ano | findstr :<PORT>
# Остановить процесс
taskkill /F /PID <PID>
```

### Проблема: "PowerShell execution policy"
**Решение:**
```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

### Проблема: "Docker build failed"
**Решение:**
```powershell
# Очистить Docker
docker system prune -a
# Пересобрать
.\docker-build.ps1
```

## 📋 Чек-лист для диагностики

### ✅ Система готова к работе
- [ ] Docker Desktop запущен и работает
- [ ] .NET 8.0 SDK установлен
- [ ] PowerShell execution policy настроен
- [ ] Порты 80, 5000, 5432 свободны
- [ ] Нет конфликтующих контейнеров

### ✅ Развертывание работает
- [ ] `.\docker-build.ps1` выполняется без ошибок
- [ ] `.\quick-deploy.ps1` запускает все сервисы
- [ ] Web приложение доступно на http://localhost
- [ ] API доступно на http://localhost:5000
- [ ] Swagger доступен на http://localhost:5000/swagger

## 🆘 Когда обращаться за помощью

### Собрать информацию для отчета:
```powershell
# Создать диагностический отчет
$report = @{
    Timestamp = Get-Date
    OS = (Get-ComputerInfo).WindowsProductName
    PowerShell = $PSVersionTable.PSVersion
    Docker = docker version 2>&1
    DotNet = dotnet --version 2>&1
    Ports = netstat -ano | findstr ":80\|:5000\|:5432"
    Processes = Get-Process dotnet
    Containers = docker ps -a
}

$report | ConvertTo-Json -Depth 3 | Out-File "diagnostic-report.json"
```

### Приложить к отчету:
- Файл `diagnostic-report.json`
- Логи из `src/Inventory.API/logs/`
- Вывод команды `docker-compose logs`
- Скриншот ошибки

## 📞 Контакты для поддержки

- **Документация**: [docs/TROUBLESHOOTING.md](../docs/TROUBLESHOOTING.md)
- **База знаний**: [.roo/knowledge-base/](../.roo/knowledge-base/)
- **GitHub Issues**: Создать issue с диагностическим отчетом

---

> 💡 **Совет**: 90% проблем решается перезапуском Docker Desktop и очисткой кэша.
