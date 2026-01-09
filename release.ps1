# GitHub Release 自动化发布脚本
param(
    [string]$Version = "1.0.0",
    [string]$Token = "",
    [string]$RepoOwner = "SeanWeiSean",
    [string]$RepoName = "Local-Knowledge-Base"
)

if ($Token -eq "") {
    Write-Host "❌ 需要 GitHub Personal Access Token" -ForegroundColor Red
    Write-Host "请在 https://github.com/settings/tokens 创建token" -ForegroundColor Yellow
    Write-Host "使用方式：.\release.ps1 -Version '1.0.0' -Token 'your_token'" -ForegroundColor Cyan
    exit 1
}

Write-Host "🚀 自动发布到 GitHub Releases v$Version" -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Green

# 构建所有版本
Write-Host "📦 构建安装包..." -ForegroundColor Yellow
& ".\build-installer.ps1" -Version $Version

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ 构建失败" -ForegroundColor Red
    exit 1
}

# 准备发布文件
$files = @()
if (Test-Path "installer\LocalKnowledgeBase-Portable-v$Version.zip") {
    $files += "installer\LocalKnowledgeBase-Portable-v$Version.zip"
}
if (Test-Path "publish\LocalKnowledgeBase.exe") {
    # 重命名单文件版本
    Copy-Item "publish\LocalKnowledgeBase.exe" -Destination "LocalKnowledgeBase-SingleFile-v$Version.exe"
    $files += "LocalKnowledgeBase-SingleFile-v$Version.exe"
}

if ($files.Count -eq 0) {
    Write-Host "❌ 没有找到要发布的文件" -ForegroundColor Red
    exit 1
}

# 创建Release
Write-Host "📝 创建 GitHub Release..." -ForegroundColor Yellow

$releaseData = @{
    tag_name = "v$Version"
    target_commitish = "main"
    name = "本地知识库助手 v$Version"
    body = @"
## 🎉 本地知识库助手 v$Version

### ✨ 新功能
- 🎨 现代化的Material Design深色主题界面
- ⚙️ 可配置的AI模型设置（支持多种Ollama模型）
- 📚 支持Word (.docx, .doc) 和 Excel (.xlsx, .xls) 文档
- 🤖 智能文档摘要生成
- 💬 基于知识库的智能问答
- 🔄 对话历史压缩，保持上下文连贯
- 📱 响应式界面设计

### 📦 下载选项

#### 🚀 推荐下载（便携版）
- **LocalKnowledgeBase-Portable-v$Version.zip** - 便携版，解压即用，无需安装

#### 💻 其他选项  
- **LocalKnowledgeBase-SingleFile-v$Version.exe** - 单文件版本，直接运行

### 🛠️ 系统要求
- Windows 10/11 (x64)
- .NET 7.0 运行环境（便携版已包含）
- Ollama 本地服务

### 📋 安装说明

#### 方式一：便携版（推荐）
1. 下载 `LocalKnowledgeBase-Portable-v$Version.zip`
2. 解压到任意目录
3. 双击 `LocalKnowledgeBase.exe` 运行

#### 方式二：单文件版
1. 下载 `LocalKnowledgeBase-SingleFile-v$Version.exe`
2. 直接双击运行

### ⚡ 快速开始

1. **安装 Ollama**
   ```bash
   # 下载：https://ollama.ai
   # 安装后运行：
   ollama pull qwen2.5:1.5b
   ```

2. **启动应用程序**
   - 双击运行下载的程序文件

3. **配置模型（可选）**
   - 点击标题栏右侧 ⚙️ 设置按钮
   - 修改模型名称和API端点
   - 支持的模型：qwen2.5:1.5b, llama2, mistral 等

4. **开始使用**
   - 点击左侧"添加文件"导入文档
   - 点击"生成摘要"处理文档  
   - 在右侧对话框中提问

### 🔧 技术栈
- .NET 7.0 WPF
- Material Design UI
- Ollama API
- DocumentFormat.OpenXml
- EPPlus

### 📞 技术支持
如有问题请提交 [Issue](https://github.com/$RepoOwner/$RepoName/issues)

---
**构建时间：** $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')  
**版本号：** v$Version  
**平台：** Windows x64
"@
    draft = $false
    prerelease = $false
} | ConvertTo-Json -Depth 3

$headers = @{
    'Authorization' = "Bearer $Token"
    'Accept' = 'application/vnd.github.v3+json'
    'Content-Type' = 'application/json'
}

try {
    $response = Invoke-RestMethod -Uri "https://api.github.com/repos/$RepoOwner/$RepoName/releases" -Method POST -Headers $headers -Body $releaseData
    $releaseId = $response.id
    $uploadUrl = $response.upload_url -replace '\{\?name,label\}', ''
    
    Write-Host "✅ Release 创建成功，ID: $releaseId" -ForegroundColor Green

    # 上传文件
    foreach ($file in $files) {
        if (Test-Path $file) {
            $fileName = Split-Path $file -Leaf
            Write-Host "📤 上传文件: $fileName..." -ForegroundColor Yellow
            
            $uploadHeaders = @{
                'Authorization' = "Bearer $Token"
                'Content-Type' = 'application/octet-stream'
            }
            
            $fileContent = [System.IO.File]::ReadAllBytes($file)
            $uploadResponse = Invoke-RestMethod -Uri "$uploadUrl?name=$fileName" -Method POST -Headers $uploadHeaders -Body $fileContent
            
            Write-Host "✅ $fileName 上传完成" -ForegroundColor Green
        }
    }

    Write-Host ""
    Write-Host "🎉 发布完成！" -ForegroundColor Green
    Write-Host "🔗 Release 页面: https://github.com/$RepoOwner/$RepoName/releases/tag/v$Version" -ForegroundColor Cyan

} catch {
    Write-Host "❌ 发布失败: $($_.Exception.Message)" -ForegroundColor Red
    
    if ($_.Exception.Response.StatusCode -eq 422) {
        Write-Host "可能原因：版本号已存在，请使用不同的版本号" -ForegroundColor Yellow
    }
}

# 清理临时文件
if (Test-Path "LocalKnowledgeBase-SingleFile-v$Version.exe") {
    Remove-Item "LocalKnowledgeBase-SingleFile-v$Version.exe" -Force
}
