# Docker и Nginx конфигурация для warehouse.cuby

## Обзор обновлений

Все конфигурации Docker и Nginx обновлены для поддержки домена `warehouse.cuby` с тремя окружениями:
- **Production**: `https://warehouse.cuby`
- **Staging**: `https://staging.warehouse.cuby`
- **Test**: `https://test.warehouse.cuby`

## Структура файлов

### Docker Compose файлы
```
docker-compose.yml          # Development (обновлен)
docker-compose.prod.yml     # Production (обновлен)
docker-compose.staging.yml  # Staging (новый)
docker-compose.test.yml     # Test (новый)
```

### Nginx конфигурации
```
nginx/
├── nginx.conf              # Основная конфигурация (обновлена)
└── conf.d/
    └── locations.conf      # Маршрутизация (обновлена)
```

### Environment файлы
```
env.production              # Production переменные
env.staging                 # Staging переменные
env.test                    # Test переменные
```

### Скрипты развертывания
```
deploy-production.ps1       # Развертывание production
deploy-staging.ps1          # Развертывание staging
deploy-test.ps1             # Развертывание test
generate-ssl-warehouse.ps1  # Генерация SSL сертификатов
```

## Ключевые особенности

### 1. **SSL/TLS конфигурация**
- Автоматическое перенаправление HTTP → HTTPS
- Отдельные SSL сертификаты для каждого домена
- Современные SSL протоколы (TLS 1.2, TLS 1.3)
- Безопасные cipher suites

### 2. **CORS настройки**
- Правильные CORS заголовки для warehouse.cuby
- Поддержка preflight запросов
- Настройки для SignalR

### 3. **Безопасность**
- Security headers (HSTS, CSP, X-Frame-Options, etc.)
- Rate limiting для API и веб-приложения
- Защита от XSS и clickjacking

### 4. **Производительность**
- Кэширование статических файлов (1 год)
- Gzip сжатие
- HTTP/2 поддержка
- Оптимизированные upstream конфигурации

## Развертывание

### 1. **Production развертывание**
```powershell
# Загрузить переменные окружения
Copy-Item env.production .env

# Развернуть
.\deploy-production.ps1
```

### 2. **Staging развертывание**
```powershell
# Загрузить переменные окружения
Copy-Item env.staging .env

# Развернуть
.\deploy-staging.ps1
```

### 3. **Test развертывание**
```powershell
# Загрузить переменные окружения
Copy-Item env.test .env

# Развернуть
.\deploy-test.ps1
```

## SSL сертификаты

### Генерация сертификатов
```powershell
# Self-signed (для разработки)
.\generate-ssl-warehouse.ps1

# Let's Encrypt (для production)
.\generate-ssl-warehouse.ps1 -UseLetsEncrypt -Email "admin@warehouse.cuby"
```

### Структура SSL файлов
```
nginx/ssl/
├── warehouse.cuby.crt
├── warehouse.cuby.key
├── staging.warehouse.cuby.crt
├── staging.warehouse.cuby.key
├── test.warehouse.cuby.crt
└── test.warehouse.cuby.key
```

## Мониторинг

### Health Check endpoints
- `https://warehouse.cuby/health`
- `https://staging.warehouse.cuby/health`
- `https://test.warehouse.cuby/health`

### Логирование
- Nginx access/error logs
- Application logs в контейнерах
- Health check мониторинг

## Конфигурация доменов

### DNS настройки
```
warehouse.cuby.          A    YOUR_SERVER_IP
staging.warehouse.cuby.  A    YOUR_SERVER_IP
test.warehouse.cuby.     A    YOUR_SERVER_IP
```

### Firewall правила
```bash
# Открыть порты
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp
sudo ufw allow 22/tcp  # SSH
```

## Переменные окружения

### Обязательные переменные
```bash
# Database
POSTGRES_PASSWORD=your_secure_password

# JWT
JWT_KEY=your_very_long_and_secure_jwt_key

# Domains
DOMAIN=warehouse.cuby
STAGING_DOMAIN=staging.warehouse.cuby
TEST_DOMAIN=test.warehouse.cuby
```

## Troubleshooting

### Проверка контейнеров
```bash
# Статус контейнеров
docker ps --filter "name=inventory"

# Логи контейнеров
docker logs inventory-nginx-prod
docker logs inventory-api-prod
docker logs inventory-web-prod
```

### Проверка nginx конфигурации
```bash
# Тест конфигурации
docker exec inventory-nginx-prod nginx -t

# Перезагрузка nginx
docker exec inventory-nginx-prod nginx -s reload
```

### Проверка SSL
```bash
# Проверка сертификата
openssl s_client -connect warehouse.cuby:443 -servername warehouse.cuby

# Проверка срока действия
echo | openssl s_client -connect warehouse.cuby:443 2>/dev/null | openssl x509 -noout -dates
```

## Обновления

### Обновление приложения
```bash
# Остановить сервисы
docker-compose -f docker-compose.prod.yml down

# Обновить код
git pull

# Пересобрать и запустить
docker-compose -f docker-compose.prod.yml up -d --build
```

### Обновление SSL сертификатов
```bash
# Let's Encrypt автообновление
sudo certbot renew --quiet

# Перезапустить nginx
docker-compose restart nginx-proxy
```

## Безопасность

### Рекомендации
1. Используйте сильные пароли для базы данных
2. Регулярно обновляйте SSL сертификаты
3. Мониторьте логи на предмет подозрительной активности
4. Используйте firewall для ограничения доступа
5. Регулярно обновляйте Docker образы

### Мониторинг безопасности
- Проверка SSL рейтинга: https://www.ssllabs.com/ssltest/
- Мониторинг логов nginx
- Проверка health check endpoints
