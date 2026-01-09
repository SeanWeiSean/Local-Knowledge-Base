# 本地知识库助手 - 简易安装器
param(
    [string]$InstallPath = "$env:ProgramFiles\LocalKnowledgeBase"
)

# 检查管理员权限
if (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Host "❌ 需要管理员权限来安装应用程序" -ForegroundColor Red
    Write-Host "请右键点击PowerShell，选择'以管理员身份运行'，然后重新执行安装" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "或者使用便携版，无需安装：LocalKnowledgeBase-Portable-v1.0.0.zip" -ForegroundColor Cyan
    Read-Host "按任意键退出"
    exit 1
}

Write-Host "🚀 本地知识库助手安装程序" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""

# 检查是否已安装
if (Test-Path "$InstallPath\LocalKnowledgeBase.exe") {
    $response = Read-Host "检测到已安装的版本，是否覆盖安装？(Y/N)"
    if ($response -ne "Y" -and $response -ne "y") {
        Write-Host "安装已取消" -ForegroundColor Yellow
        Read-Host "按任意键退出"
        exit 0
    }
}

try {
    Write-Host "📁 创建安装目录：$InstallPath" -ForegroundColor Yellow
    New-Item -ItemType Directory -Force -Path $InstallPath | Out-Null

    Write-Host "📋 复制程序文件..." -ForegroundColor Yellow
    if (Test-Path "LocalKnowledgeBase.exe") {
        Copy-Item "LocalKnowledgeBase.exe" -Destination $InstallPath -Force
    } else {
        Write-Host "❌ 找不到 LocalKnowledgeBase.exe 文件" -ForegroundColor Red
        Write-Host "请确保在包含应用程序文件的目录中运行此安装脚本" -ForegroundColor Yellow
        Read-Host "按任意键退出"
        exit 1
    }
    
    if (Test-Path "SampleDoc") {
        Copy-Item "SampleDoc" -Destination "$InstallPath\SampleDoc" -Recurse -Force
    }
    
    if (Test-Path "使用说明.txt") {
        Copy-Item "使用说明.txt" -Destination $InstallPath -Force
    }

    Write-Host "🔗 创建开始菜单快捷方式..." -ForegroundColor Yellow
    $startMenuPath = "$env:ProgramData\Microsoft\Windows\Start Menu\Programs"
    $shortcutPath = "$startMenuPath\本地知识库助手.lnk"
    
    $shell = New-Object -ComObject WScript.Shell
    $shortcut = $shell.CreateShortcut($shortcutPath)
    $shortcut.TargetPath = "$InstallPath\LocalKnowledgeBase.exe"
    $shortcut.WorkingDirectory = $InstallPath
    $shortcut.Description = "本地知识库助手 - 基于Ollama的智能问答系统"
    $shortcut.Save()

    Write-Host "🖥️ 创建桌面快捷方式..." -ForegroundColor Yellow
    $desktopPath = "$env:PUBLIC\Desktop\本地知识库助手.lnk"
    $desktopShortcut = $shell.CreateShortcut($desktopPath)
    $desktopShortcut.TargetPath = "$InstallPath\LocalKnowledgeBase.exe"
    $desktopShortcut.WorkingDirectory = $InstallPath
    $desktopShortcut.Description = "本地知识库助手 - 基于Ollama的智能问答系统"
    $desktopShortcut.Save()

    Write-Host "📝 创建卸载脚本..." -ForegroundColor Yellow
    @"
# 本地知识库助手卸载程序
Write-Host "🗑️ 卸载本地知识库助手" -ForegroundColor Red
Write-Host "==============================" -ForegroundColor Red

`$response = Read-Host "确定要卸载本地知识库助手吗？(Y/N)"
if (`$response -eq "Y" -or `$response -eq "y") {
    Write-Host "正在卸载..." -ForegroundColor Yellow
    
    # 删除程序文件
    if (Test-Path "$InstallPath") {
        Remove-Item "$InstallPath" -Recurse -Force
        Write-Host "✅ 程序文件已删除" -ForegroundColor Green
    }
    
    # 删除快捷方式
    if (Test-Path "$env:ProgramData\Microsoft\Windows\Start Menu\Programs\本地知识库助手.lnk") {
        Remove-Item "$env:ProgramData\Microsoft\Windows\Start Menu\Programs\本地知识库助手.lnk" -Force
    }
    if (Test-Path "$env:PUBLIC\Desktop\本地知识库助手.lnk") {
        Remove-Item "$env:PUBLIC\Desktop\本地知识库助手.lnk" -Force
    }
    Write-Host "✅ 快捷方式已删除" -ForegroundColor Green
    
    Write-Host "✅ 卸载完成！" -ForegroundColor Green
    Write-Host "注意：用户数据（缓存文件）保留在：%LocalAppData%\LocalKnowledgeBase" -ForegroundColor Cyan
} else {
    Write-Host "卸载已取消" -ForegroundColor Yellow
}

Read-Host "按任意键退出"
"@ | Out-File -FilePath "$InstallPath\卸载.ps1" -Encoding UTF8

    Write-Host ""
    Write-Host "✅ 安装完成！" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "📁 安装位置：$InstallPath" -ForegroundColor Cyan
    Write-Host "🖥️ 桌面快捷方式：已创建" -ForegroundColor Cyan
    Write-Host "📋 开始菜单：已添加" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "⚠️  使用前请确保：" -ForegroundColor Yellow
    Write-Host "   1. 已安装 Ollama (https://ollama.ai)" -ForegroundColor White
    Write-Host "   2. 已下载AI模型：ollama pull qwen2.5:1.5b" -ForegroundColor White
    Write-Host ""
    Write-Host "🚀 现在可以从桌面或开始菜单启动应用程序" -ForegroundColor Green
    
    $response = Read-Host "是否立即启动应用程序？(Y/N)"
    if ($response -eq "Y" -or $response -eq "y") {
        Start-Process "$InstallPath\LocalKnowledgeBase.exe"
    }

} catch {
    Write-Host "❌ 安装失败：$($_.Exception.Message)" -ForegroundColor Red
    Read-Host "按任意键退出"
    exit 1
}

Read-Host "按任意键退出"
