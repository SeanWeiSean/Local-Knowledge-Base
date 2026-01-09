@echo off
chcp 65001 >nul
title Ollama 安装和配置

echo ========================================
echo   Ollama 安装和配置脚本
echo ========================================
echo.

:: 检查 Ollama 是否已安装
where ollama >nul 2>&1
if %errorlevel% equ 0 (
    echo [√] Ollama 已安装
) else (
    echo [*] 正在使用 winget 安装 Ollama...
    winget install Ollama.Ollama --accept-package-agreements --accept-source-agreements
    
    if %errorlevel% neq 0 (
        echo [X] 安装失败，请手动运行: winget install Ollama.Ollama
        pause
        exit /b 1
    )
    
    echo [√] Ollama 安装完成
    echo [!] 请重新运行此脚本以继续配置
    pause
    exit /b 0
)

echo.
echo [*] 等待 Ollama 服务启动...
timeout /t 3 /nobreak >nul

:: 尝试启动 Ollama 服务
start "" /b ollama serve >nul 2>&1
timeout /t 5 /nobreak >nul

echo [√] Ollama 服务已启动

echo.
echo ========================================
echo   下载并运行 qwen2.5:1.5b 模型
echo ========================================
echo.

echo [*] 正在下载 qwen2.5:1.5b 模型（首次下载可能需要几分钟）...
echo.

ollama pull qwen2.5:1.5b

if %errorlevel% equ 0 (
    echo.
    echo [√] 模型下载完成
) else (
    echo [X] 模型下载失败
    pause
    exit /b 1
)

echo.
echo [*] 验证模型安装...
ollama list

echo.
echo ========================================
echo   [√] 安装完成！
echo ========================================
echo.
echo 现在可以启动本地知识库助手了！
echo.
echo 提示：
echo   - Ollama API 地址: http://localhost:11434
echo   - 模型名称: qwen2.5:1.5b
echo   - 运行 'ollama run qwen2.5:1.5b' 可以直接对话测试
echo.

set /p testModel="是否测试模型？(y/n): "
if /i "%testModel%"=="y" (
    echo.
    echo [*] 启动模型测试（输入 /bye 退出）...
    ollama run qwen2.5:1.5b
)

echo.
echo 脚本执行完毕！
pause
