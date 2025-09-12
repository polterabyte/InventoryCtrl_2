#!/bin/bash

# Скрипт запуска Inventory Control приложения
# Запускает API сервер, затем Web клиент

echo "=== Inventory Control Application Launcher ==="
echo ""

# Проверяем, что мы в корневой директории проекта
if [ ! -f "InventoryCtrl_2.sln" ]; then
    echo "Ошибка: Запустите скрипт из корневой директории проекта (где находится InventoryCtrl_2.sln)"
    exit 1
fi

# Функция для проверки доступности порта
test_port() {
    local port=$1
    local service_name=$2
    local timeout=30
    local elapsed=0
    
    echo "Ожидание запуска $service_name на порту $port..."
    
    while [ $elapsed -lt $timeout ]; do
        if nc -z localhost $port 2>/dev/null; then
            echo "$service_name успешно запущен на порту $port"
            return 0
        fi
        sleep 2
        elapsed=$((elapsed + 2))
        echo -n "."
    done
    
    echo ""
    echo "Таймаут ожидания запуска $service_name"
    return 1
}

# Функция очистки процессов при выходе
cleanup() {
    echo ""
    echo "Остановка приложений..."
    if [ ! -z "$API_PID" ]; then
        kill $API_PID 2>/dev/null
    fi
    if [ ! -z "$WEB_PID" ]; then
        kill $WEB_PID 2>/dev/null
    fi
    echo "Все приложения остановлены"
    exit 0
}

# Устанавливаем обработчик сигналов
trap cleanup SIGINT SIGTERM

try {
    # Шаг 1: Запуск API сервера
    echo "1. Запуск API сервера..."
    echo "   Порт: https://localhost:7000, http://localhost:5000"
    
    cd src/Inventory.API
    dotnet run &
    API_PID=$!
    cd ../..
    
    # Ждем запуска API сервера
    if ! test_port 7000 "API Server"; then
        echo "Не удалось запустить API сервер"
        cleanup
        exit 1
    fi
    
    echo ""
    
    # Шаг 2: Запуск Web клиента
    echo "2. Запуск Web клиента..."
    echo "   Порт: https://localhost:5001, http://localhost:5142"
    
    cd src/Inventory.Web
    dotnet run &
    WEB_PID=$!
    cd ../..
    
    # Ждем запуска Web клиента
    if ! test_port 5001 "Web Client"; then
        echo "Не удалось запустить Web клиент"
        cleanup
        exit 1
    fi
    
    echo ""
    echo "=== Приложения успешно запущены! ==="
    echo "API Server:  https://localhost:7000"
    echo "Web Client:  https://localhost:5001"
    echo ""
    echo "Нажмите Ctrl+C для остановки всех приложений"
    
    # Ожидание завершения
    wait
}

catch {
    echo "Ошибка при запуске приложений: $1"
    cleanup
    exit 1
}
