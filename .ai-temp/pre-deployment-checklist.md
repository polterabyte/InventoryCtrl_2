# Чек-лист подготовки к развертыванию warehouse.cuby

## 🔧 **1. Системные требования**

### **Сервер:**
- [ ] **OS**: Ubuntu 20.04+ или CentOS 8+
- [ ] **RAM**: Минимум 4GB (рекомендуется 8GB+)
- [ ] **CPU**: 2+ ядра
- [ ] **Диск**: 50GB+ свободного места
- [ ] **Сеть**: Статический IP адрес

### **Порты:**
- [ ] **80** (HTTP) - открыт
- [ ] **443** (HTTPS) - открыт
- [ ] **22** (SSH) - открыт
- [ ] **5432** (PostgreSQL) - только локально

## 🌐 **2. DNS конфигурация**

### **A записи:**
```
warehouse.cuby.          A    YOUR_SERVER_IP
staging.warehouse.cuby.  A    YOUR_SERVER_IP
test.warehouse.cuby.     A    YOUR_SERVER_IP
```

### **CNAME записи (опционально):**
```
www.warehouse.cuby.      CNAME warehouse.cuby.
```

### **Проверка DNS:**
```bash
# Проверить резолвинг
nslookup warehouse.cuby
nslookup staging.warehouse.cuby
nslookup test.warehouse.cuby
```

## 🔐 **3. SSL сертификаты**

### **Вариант A: Let's Encrypt (рекомендуется)**
```bash
# Установить certbot
sudo apt update
sudo apt install certbot python3-certbot-nginx

# Получить сертификаты
sudo certbot certonly --nginx -d warehouse.cuby
sudo certbot certonly --nginx -d staging.warehouse.cuby
sudo certbot certonly --nginx -d test.warehouse.cuby

# Автообновление
sudo crontab -e
# Добавить: 0 12 * * * /usr/bin/certbot renew --quiet
```

### **Вариант B: Self-signed (для тестирования)**
```bash
# Создать директорию
mkdir -p nginx/ssl

# Генерировать сертификаты
openssl req -x509 -newkey rsa:4096 -keyout nginx/ssl/warehouse.cuby.key -out nginx/ssl/warehouse.cuby.crt -days 365 -nodes -subj "/C=US/ST=State/L=City/O=Organization/OU=OrgUnit/CN=warehouse.cuby"
```

## 🐳 **4. Docker и Docker Compose**

### **Установка Docker:**
```bash
# Установить Docker
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh

# Установить Docker Compose
sudo curl -L "https://github.com/docker/compose/releases/latest/download/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
sudo chmod +x /usr/local/bin/docker-compose

# Проверить установку
docker --version
docker-compose --version
```

## 🔑 **5. Переменные окружения**

### **Создать .env файл:**
```bash
# Скопировать нужный env файл
cp env.production .env

# Отредактировать пароли и ключи
nano .env
```

### **Обязательные переменные:**
```bash
# Database
POSTGRES_PASSWORD=your_secure_production_password_here

# JWT
JWT_KEY=your_very_long_and_secure_jwt_key_for_production_at_least_32_characters

# Domains
DOMAIN=warehouse.cuby
STAGING_DOMAIN=staging.warehouse.cuby
TEST_DOMAIN=test.warehouse.cuby
```

## 🛡️ **6. Безопасность**

### **Firewall (UFW):**
```bash
# Установить UFW
sudo apt install ufw

# Настроить правила
sudo ufw default deny incoming
sudo ufw default allow outgoing
sudo ufw allow ssh
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp

# Включить firewall
sudo ufw enable
```

### **SSH безопасность:**
```bash
# Отключить root login
sudo nano /etc/ssh/sshd_config
# Установить: PermitRootLogin no

# Перезапустить SSH
sudo systemctl restart ssh
```

## 📁 **7. Файловая система**

### **Создать директории:**
```bash
# Создать директории для приложения
sudo mkdir -p /opt/warehouse
sudo mkdir -p /opt/warehouse/logs
sudo mkdir -p /opt/warehouse/ssl
sudo mkdir -p /opt/warehouse/data

# Установить права
sudo chown -R $USER:$USER /opt/warehouse
```

### **Скопировать файлы:**
```bash
# Скопировать проект
cp -r /path/to/InventoryCtrl_2/* /opt/warehouse/

# Установить права на SSL
chmod 600 /opt/warehouse/nginx/ssl/*.key
chmod 644 /opt/warehouse/nginx/ssl/*.crt
```

## 🔄 **8. Системные сервисы**

### **Создать systemd сервис:**
```bash
# Создать сервис файл
sudo nano /etc/systemd/system/warehouse.service
```

**Содержимое /etc/systemd/system/warehouse.service:**
```ini
[Unit]
Description=Warehouse Inventory Management
Requires=docker.service
After=docker.service

[Service]
Type=oneshot
RemainAfterExit=yes
WorkingDirectory=/opt/warehouse
ExecStart=/usr/local/bin/docker-compose -f docker-compose.prod.yml up -d
ExecStop=/usr/local/bin/docker-compose -f docker-compose.prod.yml down
TimeoutStartSec=0

[Install]
WantedBy=multi-user.target
```

### **Включить автозапуск:**
```bash
# Перезагрузить systemd
sudo systemctl daemon-reload

# Включить сервис
sudo systemctl enable warehouse.service

# Запустить сервис
sudo systemctl start warehouse.service
```

## 📊 **9. Мониторинг**

### **Установить мониторинг:**
```bash
# Установить htop для мониторинга
sudo apt install htop

# Установить logrotate для логов
sudo apt install logrotate
```

### **Создать logrotate конфигурацию:**
```bash
sudo nano /etc/logrotate.d/warehouse
```

**Содержимое /etc/logrotate.d/warehouse:**
```
/opt/warehouse/logs/*.log {
    daily
    missingok
    rotate 30
    compress
    delaycompress
    notifempty
    create 644 root root
}
```

## 🧪 **10. Тестирование**

### **Проверка готовности:**
```bash
# Проверить Docker
docker ps

# Проверить порты
sudo netstat -tlnp | grep :80
sudo netstat -tlnp | grep :443

# Проверить DNS
nslookup warehouse.cuby

# Проверить SSL
openssl s_client -connect warehouse.cuby:443 -servername warehouse.cuby
```

## 🚀 **11. Развертывание**

### **Production развертывание:**
```bash
# Перейти в директорию
cd /opt/warehouse

# Загрузить переменные
cp env.production .env

# Развернуть
./deploy-production.ps1

# Проверить статус
docker ps --filter "name=inventory"
```

### **Проверка после развертывания:**
```bash
# Health check
curl -k https://warehouse.cuby/health

# API test
curl -k https://warehouse.cuby/api/health

# Web app
curl -k https://warehouse.cuby/
```

## 🔧 **12. Post-deployment настройки**

### **Настроить резервное копирование:**
```bash
# Создать скрипт бэкапа
nano /opt/warehouse/backup.sh
```

**Содержимое backup.sh:**
```bash
#!/bin/bash
DATE=$(date +%Y%m%d_%H%M%S)
BACKUP_DIR="/opt/warehouse/backups"
mkdir -p $BACKUP_DIR

# Бэкап базы данных
docker exec inventory-postgres-prod pg_dump -U postgres inventorydb > $BACKUP_DIR/db_$DATE.sql

# Бэкап конфигураций
tar -czf $BACKUP_DIR/config_$DATE.tar.gz nginx/ *.yml *.env

# Удалить старые бэкапы (старше 30 дней)
find $BACKUP_DIR -name "*.sql" -mtime +30 -delete
find $BACKUP_DIR -name "*.tar.gz" -mtime +30 -delete
```

### **Настроить cron для бэкапов:**
```bash
# Добавить в crontab
crontab -e
# Добавить: 0 2 * * * /opt/warehouse/backup.sh
```

## ✅ **Финальная проверка**

### **Чек-лист готовности:**
- [ ] DNS настроен и резолвится
- [ ] SSL сертификаты установлены
- [ ] Docker и Docker Compose установлены
- [ ] Переменные окружения настроены
- [ ] Firewall настроен
- [ ] Файлы скопированы
- [ ] Systemd сервис создан
- [ ] Мониторинг настроен
- [ ] Резервное копирование настроено

### **Тестирование:**
- [ ] Health check проходит
- [ ] API отвечает
- [ ] Web приложение загружается
- [ ] SSL работает
- [ ] Логи пишутся
- [ ] Автозапуск работает

После выполнения всех пунктов ваше приложение готово к развертыванию! 🎉
