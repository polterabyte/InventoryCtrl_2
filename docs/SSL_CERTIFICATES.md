# SSL Сертификаты

Руководство по настройке и управлению SSL сертификатами для Inventory Control System.

## 🔐 Обзор

Система использует самоподписанные SSL сертификаты для разработки и тестирования. В production рекомендуется использовать сертификаты от доверенного CA.

## 📁 Структура сертификатов

```
nginx/ssl/
├── warehouse.cuby.crt          # Production сертификат
├── warehouse.cuby.key          # Production приватный ключ
├── staging.warehouse.cuby.crt  # Staging сертификат
├── staging.warehouse.cuby.key  # Staging приватный ключ
├── test.warehouse.cuby.crt     # Test сертификат
├── test.warehouse.cuby.key     # Test приватный ключ
├── localhost.crt               # Localhost сертификат
├── localhost.key               # Localhost приватный ключ
├── 192.168.139.96.crt         # IP адрес сертификат
└── 192.168.139.96.key         # IP адрес приватный ключ
```

## 🚀 Автоматическая генерация

### Использование скрипта
```powershell
.\generate-ssl-warehouse.ps1
```

Скрипт создаст сертификаты для всех доменов:
- `warehouse.cuby`
- `staging.warehouse.cuby`
- `test.warehouse.cuby`

## 🔧 Ручная генерация

### 1. Создание сертификата для домена
```powershell
docker run --rm -v "${PWD}/nginx/ssl:/ssl" alpine/openssl req -x509 -newkey rsa:4096 -keyout /ssl/domain.key -out /ssl/domain.crt -days 365 -nodes -subj "/C=US/ST=State/L=City/O=Organization/OU=OrgUnit/CN=domain.com"
```

### 2. Создание сертификата для localhost
```powershell
docker run --rm -v "${PWD}/nginx/ssl:/ssl" alpine/openssl req -x509 -newkey rsa:4096 -keyout /ssl/localhost.key -out /ssl/localhost.crt -days 365 -nodes -subj "/C=US/ST=State/L=City/O=Organization/OU=OrgUnit/CN=localhost" -addext "subjectAltName=DNS:localhost,DNS:127.0.0.1,IP:127.0.0.1"
```

### 3. Создание сертификата для IP адреса
```powershell
docker run --rm -v "${PWD}/nginx/ssl:/ssl" alpine/openssl req -x509 -newkey rsa:4096 -keyout /ssl/192.168.139.96.key -out /ssl/192.168.139.96.crt -days 365 -nodes -subj "/C=US/ST=State/L=City/O=Organization/OU=OrgUnit/CN=192.168.139.96" -addext "subjectAltName=DNS:localhost,DNS:192.168.139.96,IP:192.168.139.96,IP:127.0.0.1"
```

## 🌐 Настройка nginx

### Конфигурация для localhost
```nginx
server {
    listen 443 ssl;
    http2 on;
    server_name localhost 127.0.0.1;

    ssl_certificate /etc/nginx/ssl/localhost.crt;
    ssl_certificate_key /etc/nginx/ssl/localhost.key;
    
    # Остальная конфигурация...
}
```

### Конфигурация для IP адреса
```nginx
server {
    listen 443 ssl;
    http2 on;
    server_name 192.168.139.96;

    ssl_certificate /etc/nginx/ssl/192.168.139.96.crt;
    ssl_certificate_key /etc/nginx/ssl/192.168.139.96.key;
    
    # Остальная конфигурация...
}
```

## 🔒 Безопасность

### Рекомендации для production
1. **Используйте сертификаты от доверенного CA** (Let's Encrypt, DigiCert, etc.)
2. **Настройте автоматическое обновление** сертификатов
3. **Используйте сильные ключи** (минимум 2048 бит, рекомендуется 4096)
4. **Настройте HSTS** заголовки
5. **Регулярно обновляйте** сертификаты

### Настройка Let's Encrypt (для production)
```bash
# Установка certbot
sudo apt-get install certbot

# Получение сертификата
sudo certbot certonly --standalone -d warehouse.cuby

# Автоматическое обновление
sudo crontab -e
# Добавить: 0 12 * * * /usr/bin/certbot renew --quiet
```

## 🐛 Устранение проблем

### Проблема: Сертификат не доверенный
**Решение:** Добавьте сертификат в доверенные в браузере или используйте `-k` флаг в curl.

### Проблема: Сертификат не подходит для домена
**Решение:** Убедитесь, что в сертификате указан правильный CN и SAN.

### Проблема: Сертификат истек
**Решение:** Сгенерируйте новый сертификат с актуальной датой.

## 📋 Проверка сертификата

### Проверка содержимого
```bash
openssl x509 -in nginx/ssl/warehouse.cuby.crt -text -noout
```

### Проверка срока действия
```bash
openssl x509 -in nginx/ssl/warehouse.cuby.crt -dates -noout
```

### Проверка приватного ключа
```bash
openssl rsa -in nginx/ssl/warehouse.cuby.key -check
```

## 🔄 Обновление сертификатов

### Автоматическое обновление (cron)
```bash
# Добавить в crontab
0 2 * * 0 /path/to/update-ssl.sh
```

### Скрипт обновления
```bash
#!/bin/bash
cd /path/to/project
./generate-ssl-warehouse.ps1
docker restart inventory-nginx-staging
```

## 📚 Дополнительные ресурсы

- [OpenSSL Documentation](https://www.openssl.org/docs/)
- [Let's Encrypt Documentation](https://letsencrypt.org/docs/)
- [Nginx SSL Configuration](https://nginx.org/en/docs/http/configuring_https_servers.html)
