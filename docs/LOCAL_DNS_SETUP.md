# Настройка локального DNS для разработки

## Проблема

При разработке и тестировании приложения возникают проблемы с доступом к доменам:
- `staging.warehouse.cuby` 
- `test.warehouse.cuby`
- `warehouse.cuby`

Эти домены не настроены в глобальном DNS, поэтому браузер не может их разрешить.

## Решения

### 1. Автоматическая настройка (рекомендуется)

Запустите скрипт для автоматической настройки hosts файла:

```powershell
# Как администратор
.\deploy\setup-local-dns.ps1

# Или с указанием IP адреса
.\deploy\setup-local-dns.ps1 -ServerIP 192.168.1.100

# Удалить записи
.\deploy\setup-local-dns.ps1 -Remove
```

### 2. Ручная настройка hosts файла

Добавьте в файл `C:\Windows\System32\drivers\etc\hosts`:

```
# Inventory Control System - Local Development
192.168.139.96    warehouse.cuby
192.168.139.96    staging.warehouse.cuby  
192.168.139.96    test.warehouse.cuby
```

### 3. Локальный DNS сервер

Для более продвинутого решения можно настроить локальный DNS сервер:

#### Pi-hole (рекомендуется)
```bash
# Docker команда для Pi-hole
docker run -d \
  --name pihole \
  -p 53:53/tcp -p 53:53/udp \
  -p 80:80 \
  -e TZ=Europe/Moscow \
  -v "$(pwd)/etc-pihole:/etc/pihole" \
  -v "$(pwd)/etc-dnsmasq.d:/etc/dnsmasq.d" \
  --dns=127.0.0.1 --dns=1.1.1.1 \
  --restart=unless-stopped \
  pihole/pihole:latest
```

Затем добавить записи через веб-интерфейс Pi-hole.

#### AdGuard Home
```bash
docker run -d \
  --name adguardhome \
  -v /my/own/workdir:/opt/adguardhome/work \
  -v /my/own/confdir:/opt/adguardhome/conf \
  -p 53:53/tcp -p 53:53/udp \
  -p 80:80/tcp -p 443:443/tcp \
  --restart unless-stopped \
  adguard/adguardhome
```

## Преимущества каждого подхода

### Hosts файл
✅ Простота настройки  
✅ Работает без дополнительного ПО  
✅ Мгновенное применение изменений  
❌ Нужно настраивать на каждом компьютере  
❌ Нет централизованного управления  

### Локальный DNS
✅ Централизованное управление  
✅ Работает для всей сети  
✅ Можно настроить дополнительные записи  
❌ Требует настройки DNS сервера  
❌ Более сложная конфигурация  

## Обновление IP адреса

Если IP адрес сервера изменился (например, из-за DHCP):

1. **Автоматически:**
   ```powershell
   .\deploy\setup-local-dns.ps1 -ServerIP [новый_ip]
   ```

2. **Вручную:** Обновите записи в hosts файле

3. **Для DNS сервера:** Обновите A-записи через веб-интерфейс

## Проверка настройки

```powershell
# Проверить разрешение домена
nslookup staging.warehouse.cuby

# Или через ping
ping staging.warehouse.cuby
```

## Troubleshooting

### Домен не разрешается
1. Проверьте hosts файл: `type C:\Windows\System32\drivers\etc\hosts`
2. Очистите DNS кэш: `ipconfig /flushdns`
3. Перезапустите браузер

### SSL ошибки
- Используйте самоподписанные сертификаты для разработки
- Добавьте исключение в браузере для домена
- Или используйте HTTP для локального тестирования

### Порты заняты
```powershell
# Проверить занятые порты
netstat -an | findstr ":80\|:443"

# Освободить порты
docker-compose down
```
