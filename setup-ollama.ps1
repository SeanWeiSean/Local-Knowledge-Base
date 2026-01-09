# Ollama 安装和配置脚本
# 用于安装 Ollama 并运行 qwen2.5:1.5b 模型

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Ollama 安装和配置脚本" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# 检查是否以管理员权限运行
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "⚠️  建议以管理员权限运行此脚本" -ForegroundColor Yellow
}

# 检查 Ollama 是否已安装
$ollamaPath = Get-Command ollama -ErrorAction SilentlyContinue

if ($ollamaPath) {
    Write-Host "✅ Ollama 已安装: $($ollamaPath.Source)" -ForegroundColor Green
} else {
    Write-Host "📥 正在使用 winget 安装 Ollama..." -ForegroundColor Yellow
    
    try {
        # 使用 winget 安装 Ollama
        winget install Ollama.Ollama --accept-package-agreements --accept-source-agreements
        
        # 刷新环境变量
        $env:Path = [System.Environment]::GetEnvironmentVariable("Path", "Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path", "User")
        
        Write-Host "✅ Ollama 安装完成" -ForegroundColor Green
    }
    catch {
        Write-Host "❌ 安装失败: $_" -ForegroundColor Red
        Write-Host "请手动运行: winget install Ollama.Ollama" -ForegroundColor Yellow
        exit 1
    }
}

# 等待 Ollama 服务启动
Write-Host ""
Write-Host "⏳ 等待 Ollama 服务启动..." -ForegroundColor Yellow
Start-Sleep -Seconds 3

# 检查 Ollama 服务是否运行
$maxRetries = 10
$retryCount = 0
$serviceRunning = $false

while ($retryCount -lt $maxRetries -and -not $serviceRunning) {
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:11434/api/tags" -UseBasicParsing -TimeoutSec 5 -ErrorAction Stop
        $serviceRunning = $true
        Write-Host "✅ Ollama 服务已运行" -ForegroundColor Green
    }
    catch {
        $retryCount++
        Write-Host "⏳ 等待服务启动... ($retryCount/$maxRetries)" -ForegroundColor Gray
        
        # 尝试启动 Ollama
        if ($retryCount -eq 3) {
            Write-Host "🔄 尝试启动 Ollama 服务..." -ForegroundColor Yellow
            Start-Process "ollama" -ArgumentList "serve" -WindowStyle Hidden -ErrorAction SilentlyContinue
        }
        
        Start-Sleep -Seconds 2
    }
}

if (-not $serviceRunning) {
    Write-Host "❌ Ollama 服务启动失败，请手动启动" -ForegroundColor Red
    exit 1
}

# 下载并运行 qwen2.5:1.5b 模型
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  下载并运行 qwen2.5:1.5b 模型" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "📥 正在下载 qwen2.5:1.5b 模型（首次下载可能需要几分钟）..." -ForegroundColor Yellow
Write-Host ""

# 使用 ollama pull 下载模型
$pullProcess = Start-Process -FilePath "ollama" -ArgumentList "pull qwen2.5:1.5b" -NoNewWindow -Wait -PassThru

if ($pullProcess.ExitCode -eq 0) {
    Write-Host ""
    Write-Host "✅ 模型下载完成" -ForegroundColor Green
} else {
    Write-Host "❌ 模型下载失败" -ForegroundColor Red
    exit 1
}

# 验证模型
Write-Host ""
Write-Host "🔍 验证模型安装..." -ForegroundColor Yellow
ollama list

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "  ✅ 安装完成！" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "现在可以启动本地知识库助手了！" -ForegroundColor Cyan
Write-Host ""
Write-Host "提示：" -ForegroundColor Yellow
Write-Host "  - Ollama API 地址: http://localhost:11434" -ForegroundColor Gray
Write-Host "  - 模型名称: qwen2.5:1.5b" -ForegroundColor Gray
Write-Host "  - 运行 'ollama run qwen2.5:1.5b' 可以直接对话测试" -ForegroundColor Gray
Write-Host ""

# 询问是否测试模型
$testModel = Read-Host "是否测试模型？(y/n)"
if ($testModel -eq "y" -or $testModel -eq "Y") {
    Write-Host ""
    Write-Host "🚀 启动模型测试（输入 /bye 退出）..." -ForegroundColor Cyan
    ollama run qwen2.5:1.5b
}

Write-Host ""
Write-Host "脚本执行完毕！" -ForegroundColor Green
