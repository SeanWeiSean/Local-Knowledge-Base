# 本地知识库助手安装包构建脚本
param(
    [string]$Version = "1.0.0"
)

Write-Host "🚀 开始构建本地知识库助手 v$Version" -ForegroundColor Green

# 清理之前的构建
Write-Host "📁 清理构建目录..." -ForegroundColor Yellow
if (Test-Path "publish") { Remove-Item "publish" -Recurse -Force }
if (Test-Path "installer") { Remove-Item "installer" -Recurse -Force }

New-Item -ItemType Directory -Force -Path "publish" | Out-Null
New-Item -ItemType Directory -Force -Path "installer" | Out-Null

# 发布应用程序
Write-Host "⚙️ 发布应用程序..." -ForegroundColor Yellow
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o "publish"

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ 发布失败" -ForegroundColor Red
    exit 1
}

# 创建便携版
Write-Host "📦 创建便携版..." -ForegroundColor Yellow
$portableDir = "installer\LocalKnowledgeBase-Portable-v$Version"
New-Item -ItemType Directory -Force -Path $portableDir | Out-Null

# 复制必要文件
Copy-Item "publish\LocalKnowledgeBase.exe" -Destination $portableDir
Copy-Item "README.md" -Destination $portableDir
Copy-Item "SampleDoc\*" -Destination "$portableDir\SampleDoc\" -Recurse -Force

# 创建便携版说明文件
@"
本地知识库助手 v$Version - 便携版
=====================================

运行说明：
1. 确保已安装并运行 Ollama (https://ollama.ai)
2. 下载AI模型：ollama pull qwen2.5:1.5b
3. 双击 LocalKnowledgeBase.exe 启动应用程序

功能特点：
- 支持 Word/Excel 文档导入
- 智能文档摘要生成
- 基于知识库的问答
- 现代化 Material Design 界面
- 可配置AI模型

使用方法：
1. 点击左侧"添加文件"导入文档
2. 点击"生成摘要"处理文档
3. 在右侧输入框中提问
4. 点击标题栏⚙️按钮配置模型

技术支持：
- GitHub: https://github.com/SeanWeiSean/Local-Knowledge-Base
- 需要问题请在GitHub提交Issue

版本信息：
- 版本：v$Version
- 构建时间：$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
- 运行环境：Windows x64
- .NET版本：7.0
"@ | Out-File -FilePath "$portableDir\使用说明.txt" -Encoding UTF8

# 压缩便携版
Write-Host "🗜️ 压缩便携版..." -ForegroundColor Yellow
Compress-Archive -Path $portableDir -DestinationPath "installer\LocalKnowledgeBase-Portable-v$Version.zip" -Force

Write-Host "✅ 构建完成！" -ForegroundColor Green
Write-Host ""
Write-Host "📁 输出文件位置：" -ForegroundColor Cyan
Write-Host "   便携版ZIP：installer\LocalKnowledgeBase-Portable-v$Version.zip" -ForegroundColor White
Write-Host "   单文件EXE：publish\LocalKnowledgeBase.exe" -ForegroundColor White
Write-Host ""
Write-Host "📋 文件大小：" -ForegroundColor Cyan
$exeSize = (Get-Item "publish\LocalKnowledgeBase.exe").Length / 1MB
$zipSize = (Get-Item "installer\LocalKnowledgeBase-Portable-v$Version.zip").Length / 1MB
Write-Host "   EXE文件：$($exeSize.ToString("F1")) MB" -ForegroundColor White
Write-Host "   ZIP文件：$($zipSize.ToString("F1")) MB" -ForegroundColor White

Write-Host ""
Write-Host "🎉 可以开始分发了！建议同时提供：" -ForegroundColor Green
Write-Host "   1. 便携版ZIP（无需安装，解压即用）" -ForegroundColor White
Write-Host "   2. 单文件EXE（直接运行）" -ForegroundColor White
