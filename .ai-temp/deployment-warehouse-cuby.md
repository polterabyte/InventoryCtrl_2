# Развертывание на warehouse.cuby

## Структура доменов

### Production
- **Основной сайт**: `https://warehouse.cuby`
- **API**: `https://warehouse.cuby/api`
- **SignalR Hub**: `https://warehouse.cuby/notificationHub`
- **Health Check**: `https://warehouse.cuby/health`

### Staging
- **Основной сайт**: `https://staging.warehouse.cuby`
- **API**: `https://staging.warehouse.cuby/api`
- **SignalR Hub**: `https://staging.warehouse.cuby/notificationHub`
- **Health Check**: `https://staging.warehouse.cuby/health`

### Test
- **Основной сайт**: `https://test.warehouse.cuby`
- **API**: `https://test.warehouse.cuby/api`
- **SignalR Hub**: `https://test.warehouse.cuby/notificationHub`
- **Health Check**: `https://test.warehouse.cuby/health`

## Конфигурация сервера

### Nginx конфигурация
```nginx
# Основной домен warehouse.cuby
server {
    listen 443 ssl http2;
    server_name warehouse.cuby;
    
    # SSL сертификат
    ssl_certificate /path/to/warehouse.cuby.crt;
    ssl_certificate_key /path/to/warehouse.cuby.key;
    
    # API проксирование
    location /api/ {
        proxy_pass http://localhost:5000/api/;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
    
    # SignalR Hub
    location /notificationHub {
        proxy_pass http://localhost:5000/notificationHub;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
    
    # Health check
    location /health {
        proxy_pass http://localhost:5000/health;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
    
    # Blazor WebAssembly приложение
    location / {
        root /var/www/warehouse.cuby;
        try_files $uri $uri/ /index.html;
        
        # Кэширование статических файлов
        location ~* \.(js|css|png|jpg|jpeg|gif|ico|svg)$ {
            expires 1y;
            add_header Cache-Control "public, immutable";
        }
    }
}

# Staging домен
server {
    listen 443 ssl http2;
    server_name staging.warehouse.cuby;
    
    # Аналогичная конфигурация для staging
    # ...
}

# Test домен
server {
    listen 443 ssl http2;
    server_name test.warehouse.cuby;
    
    # Аналогичная конфигурация для test
    # ...
}
```

### CORS настройки для API
```csharp
// В Program.cs API проекта
builder.Services.AddCors(options =>
{
    options.AddPolicy("WarehouseCubyPolicy", policy =>
    {
        policy.WithOrigins(
                "https://warehouse.cuby",
                "https://staging.warehouse.cuby", 
                "https://test.warehouse.cuby",
                "https://localhost:5001" // для development
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials(); // Для SignalR
    });
});
```

## Переменные окружения

### Production
```bash
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=https://localhost:5000
```

### Staging
```bash
ASPNETCORE_ENVIRONMENT=Staging
ASPNETCORE_URLS=https://localhost:5001
```

### Test
```bash
ASPNETCORE_ENVIRONMENT=Test
ASPNETCORE_URLS=https://localhost:5002
```

## SSL сертификаты

### Let's Encrypt (рекомендуется)
```bash
# Установка certbot
sudo apt install certbot python3-certbot-nginx

# Получение сертификата для основного домена
sudo certbot --nginx -d warehouse.cuby

# Получение сертификата для поддоменов
sudo certbot --nginx -d staging.warehouse.cuby
sudo certbot --nginx -d test.warehouse.cuby

# Автоматическое обновление
sudo crontab -e
# Добавить: 0 12 * * * /usr/bin/certbot renew --quiet
```

## Мониторинг и логирование

### Health Check endpoints
- `https://warehouse.cuby/health` - основной health check
- `https://staging.warehouse.cuby/health` - staging health check
- `https://test.warehouse.cuby/health` - test health check

### Логирование
```csharp
// Настройка логирования для разных окружений
if (builder.Environment.IsProduction())
{
    builder.Logging.AddFile("logs/warehouse-{Date}.log");
}
else if (builder.Environment.IsStaging())
{
    builder.Logging.AddFile("logs/staging-{Date}.log");
}
```

## Развертывание

### 1. Подготовка сервера
```bash
# Установка .NET 8
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt-get update
sudo apt-get install -y dotnet-sdk-8.0

# Установка Nginx
sudo apt install nginx

# Установка SSL
sudo apt install certbot python3-certbot-nginx
```

### 2. Развертывание приложения
```bash
# Клонирование репозитория
git clone https://github.com/your-repo/InventoryCtrl_2.git
cd InventoryCtrl_2

# Сборка и публикация
dotnet publish -c Release -o /var/www/warehouse.cuby

# Настройка systemd сервиса
sudo systemctl enable warehouse-api
sudo systemctl start warehouse-api
```

### 3. Настройка Nginx
```bash
# Копирование конфигурации
sudo cp nginx/warehouse.cuby.conf /etc/nginx/sites-available/
sudo ln -s /etc/nginx/sites-available/warehouse.cuby.conf /etc/nginx/sites-enabled/

# Перезапуск Nginx
sudo systemctl reload nginx
```

## Проверка развертывания

### 1. Проверка API
```bash
curl -k https://warehouse.cuby/health
curl -k https://staging.warehouse.cuby/health
curl -k https://test.warehouse.cuby/health
```

### 2. Проверка SignalR
```javascript
// В браузере на warehouse.cuby
const connection = new signalR.HubConnectionBuilder()
    .withUrl("https://warehouse.cuby/notificationHub")
    .build();
```

### 3. Проверка CORS
```bash
curl -H "Origin: https://warehouse.cuby" \
     -H "Access-Control-Request-Method: GET" \
     -H "Access-Control-Request-Headers: X-Requested-With" \
     -X OPTIONS \
     https://warehouse.cuby/api/products
```
