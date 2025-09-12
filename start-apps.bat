@echo off
chcp 65001 >nul
echo === Inventory Control Application Launcher ===
echo.

REM Проверяем, что мы в корневой директории проекта
if not exist "InventoryCtrl_2.sln" (
    echo Ошибка: Запустите скрипт из корневой директории проекта (где находится InventoryCtrl_2.sln)
    pause
    exit /b 1
)

echo 1. Запуск API сервера...
echo    Порт: https://localhost:7000, http://localhost:5000
echo.

REM Запускаем API сервер в новом окне
start "API Server" cmd /k "cd /d src\Inventory.API && dotnet run"

REM Ждем немного для запуска сервера
echo Ожидание запуска API сервера...
timeout /t 10 /nobreak >nul

echo.
echo 2. Запуск Web клиента...
echo    Порт: https://localhost:5001, http://localhost:5142
echo.

REM Запускаем Web клиент в новом окне
start "Web Client" cmd /k "cd /d src\Inventory.Web && dotnet run"

echo.
echo === Приложения запущены! ===
echo API Server:  https://localhost:7000
echo Web Client:  https://localhost:5001
echo.
echo Закройте окна командной строки для остановки приложений
echo.
pause
