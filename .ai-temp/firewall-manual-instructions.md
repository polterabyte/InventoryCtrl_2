# Инструкции по настройке файрвола Windows

## Текущая ситуация

✅ **Хорошие новости:**
- Файрвол Windows включен
- Docker контейнеры работают и используют порты 80 и 5000
- Порт 80 имеет некоторые правила файрвола
- Порт 5000 НЕ имеет правил файрвола (это проблема!)

## Решение проблемы

### Вариант 1: Через PowerShell (требует права администратора)

1. **Запустите PowerShell от имени администратора:**
   - Нажмите `Win + X`
   - Выберите "Windows PowerShell (Admin)" или "Терминал (Admin)"

2. **Выполните следующие команды:**

```powershell
# Для порта 80 (веб-клиент)
New-NetFirewallRule -DisplayName "Inventory_App_Port_80_Inbound" -Direction Inbound -Protocol TCP -LocalPort 80 -Action Allow -Profile Any
New-NetFirewallRule -DisplayName "Inventory_App_Port_80_Outbound" -Direction Outbound -Protocol TCP -LocalPort 80 -Action Allow -Profile Any

# Для порта 5000 (API)
New-NetFirewallRule -DisplayName "Inventory_App_Port_5000_Inbound" -Direction Inbound -Protocol TCP -LocalPort 5000 -Action Allow -Profile Any
New-NetFirewallRule -DisplayName "Inventory_App_Port_5000_Outbound" -Direction Outbound -Protocol TCP -LocalPort 5000 -Action Allow -Profile Any
```

### Вариант 2: Через графический интерфейс

1. **Откройте "Брандмауэр Защитника Windows":**
   - Нажмите `Win + R`
   - Введите `wf.msc` и нажмите Enter

2. **Создайте правило для порта 80:**
   - Нажмите "Правила для входящих подключений" → "Создать правило"
   - Выберите "Порт" → Далее
   - Выберите "TCP" и "Определенные локальные порты" → введите `80`
   - Выберите "Разрешить подключение" → Далее
   - Отметьте все профили (Домен, Частный, Публичный) → Далее
   - Имя: "Inventory App Port 80 Inbound" → Готово

3. **Создайте правило для порта 5000:**
   - Повторите шаги выше, но используйте порт `5000`
   - Имя: "Inventory App Port 5000 Inbound"

4. **Создайте исходящие правила:**
   - Повторите для "Правила для исходящих подключений"
   - Порт 80: "Inventory App Port 80 Outbound"
   - Порт 5000: "Inventory App Port 5000 Outbound"

## Проверка результата

После добавления правил выполните:

```powershell
# Запустите от имени администратора
.\firewall-check-simple.ps1
```

Вы должны увидеть:
- Port 80: Inbound rules: 3+ (вместо 2)
- Port 5000: Inbound rules: 2+ (вместо 0)

## Тестирование внешнего доступа

1. **Узнайте IP адрес вашего компьютера:**
```powershell
ipconfig | findstr "IPv4"
```

2. **Протестируйте с другого компьютера:**
```powershell
# Замените [YOUR_IP] на ваш IP адрес
.\test-ports-external.ps1 -ServerIP [YOUR_IP]
```

3. **Или используйте telnet:**
```cmd
telnet [YOUR_IP] 80
telnet [YOUR_IP] 5000
```

## Альтернативные решения

### Если файрвол не помогает:

1. **Проверьте роутер/маршрутизатор:**
   - Убедитесь, что порты 80 и 5000 проброшены
   - Проверьте настройки NAT

2. **Временно отключите файрвол для тестирования:**
   ```powershell
   # ВНИМАНИЕ: Только для тестирования!
   Set-NetFirewallProfile -Profile Domain,Private,Public -Enabled False
   ```

3. **Используйте другой порт:**
   - Измените порты в `docker-compose.yml`
   - Используйте порты, которые точно открыты (например, 8080, 8081)

## Полезные команды

```powershell
# Просмотр всех правил файрвола
Get-NetFirewallRule | Where-Object {$_.DisplayName -like "*Inventory*"}

# Удаление правил (если нужно)
Remove-NetFirewallRule -DisplayName "*Inventory*"

# Проверка открытых портов
netstat -an | findstr ":80\|:5000"
```
