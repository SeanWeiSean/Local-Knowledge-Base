# 高级MSI安装包构建脚本（需要WiX Toolset）
param(
    [string]$Version = "1.0.0"
)

Write-Host "🔧 检查WiX Toolset..." -ForegroundColor Yellow

# 检查是否安装了WiX
$wixPath = ""
$possiblePaths = @(
    "${env:ProgramFiles(x86)}\WiX Toolset v3.11\bin\candle.exe",
    "${env:ProgramFiles}\WiX Toolset v3.11\bin\candle.exe",
    "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2022\BuildTools\MSBuild\Microsoft\WiX\v3.x\bin\candle.exe"
)

foreach ($path in $possiblePaths) {
    if (Test-Path $path) {
        $wixPath = Split-Path $path -Parent
        break
    }
}

if ($wixPath -eq "") {
    Write-Host "❌ 未找到 WiX Toolset" -ForegroundColor Red
    Write-Host ""
    Write-Host "请选择安装选项：" -ForegroundColor Yellow
    Write-Host "1. 安装 WiX Toolset (推荐) - 可创建专业MSI安装包"
    Write-Host "2. 使用简易安装方案 - PowerShell脚本安装"
    Write-Host ""
    $choice = Read-Host "请选择 (1/2)"
    
    if ($choice -eq "1") {
        Write-Host "正在下载 WiX Toolset..." -ForegroundColor Yellow
        Start-Process "https://github.com/wixtoolset/wix3/releases/download/wix3112rtm/wix311.exe"
        Write-Host "请安装WiX后重新运行此脚本" -ForegroundColor Cyan
        Read-Host "按任意键退出"
        exit 0
    } else {
        Write-Host "使用简易安装方案..." -ForegroundColor Green
        & ".\build-installer.ps1" -Version $Version
        exit 0
    }
}

Write-Host "✅ 找到 WiX Toolset：$wixPath" -ForegroundColor Green

# 创建WiX源文件
Write-Host "📝 创建MSI配置文件..." -ForegroundColor Yellow

$wixContent = @"
<?xml version="1.0" encoding="UTF-8"?>
<Wix xmlns="http://schemas.microsoft.com/wix/2006/wi">
  <Product Id="*" 
           Name="本地知识库助手" 
           Language="2052" 
           Version="$Version.0" 
           Manufacturer="LocalKnowledgeBase" 
           UpgradeCode="12345678-1234-1234-1234-123456789012">
           
    <Package InstallerVersion="200" 
             Compressed="yes" 
             InstallScope="perMachine" 
             Description="基于Ollama的本地知识库WPF应用程序"
             Comments="支持Word/Excel文档的智能问答" />

    <MajorUpgrade DowngradeErrorMessage="已安装更新的版本。" />
    <MediaTemplate EmbedCab="yes" />

    <Feature Id="ProductFeature" Title="本地知识库助手" Level="1">
      <ComponentGroupRef Id="ProductComponents" />
      <ComponentRef Id="ApplicationShortcut" />
      <ComponentRef Id="DesktopShortcut" />
    </Feature>

    <!-- 目录结构 -->
    <Directory Id="TARGETDIR" Name="SourceDir">
      <Directory Id="ProgramFilesFolder">
        <Directory Id="INSTALLFOLDER" Name="LocalKnowledgeBase" />
      </Directory>
      <Directory Id="ProgramMenuFolder">
        <Directory Id="ApplicationProgramsFolder" Name="本地知识库助手"/>
      </Directory>
      <Directory Id="DesktopFolder" Name="Desktop"/>
    </Directory>

    <!-- 程序文件 -->
    <ComponentGroup Id="ProductComponents" Directory="INSTALLFOLDER">
      <Component Id="LocalKnowledgeBase.exe">
        <File Id="LocalKnowledgeBase.exe" 
              Source="publish\LocalKnowledgeBase.exe" 
              KeyPath="yes" 
              Checksum="yes"/>
      </Component>
    </ComponentGroup>

    <!-- 开始菜单快捷方式 -->
    <DirectoryRef Id="ApplicationProgramsFolder">
      <Component Id="ApplicationShortcut" Guid="12345678-1234-1234-1234-123456789013">
        <Shortcut Id="ApplicationStartMenuShortcut"
                  Name="本地知识库助手"
                  Description="基于Ollama的智能问答系统"
                  Target="[#LocalKnowledgeBase.exe]"
                  WorkingDirectory="INSTALLFOLDER"/>
        <RemoveFolder Id="ApplicationProgramsFolder" On="uninstall"/>
        <RegistryValue Root="HKCU" 
                       Key="Software\LocalKnowledgeBase" 
                       Name="installed" 
                       Type="integer" 
                       Value="1" 
                       KeyPath="yes"/>
      </Component>
    </DirectoryRef>

    <!-- 桌面快捷方式 -->
    <DirectoryRef Id="DesktopFolder">
      <Component Id="DesktopShortcut" Guid="12345678-1234-1234-1234-123456789014">
        <Shortcut Id="ApplicationDesktopShortcut"
                  Name="本地知识库助手"
                  Description="基于Ollama的智能问答系统"
                  Target="[#LocalKnowledgeBase.exe]"
                  WorkingDirectory="INSTALLFOLDER"/>
        <RegistryValue Root="HKCU" 
                       Key="Software\LocalKnowledgeBase" 
                       Name="desktop" 
                       Type="integer" 
                       Value="1" 
                       KeyPath="yes"/>
      </Component>
    </DirectoryRef>

    <!-- 自定义UI -->
    <UI>
      <UIRef Id="WixUI_InstallDir" />
      <Publish Dialog="WelcomeDlg" Control="Next" Event="NewDialog" Value="InstallDirDlg" Order="2">1</Publish>
      <Publish Dialog="InstallDirDlg" Control="Back" Event="NewDialog" Value="WelcomeDlg" Order="2">1</Publish>
    </UI>

    <Property Id="WIXUI_INSTALLDIR" Value="INSTALLFOLDER" />
    
    <!-- 许可协议 -->
    <WixVariable Id="WixUILicenseRtf" Value="license.rtf" />
    
  </Product>
</Wix>
"@

$wixContent | Out-File -FilePath "installer.wxs" -Encoding UTF8

# 创建许可协议文件
@"
{\rtf1\ansi\deff0 {\fonttbl {\f0 Times New Roman;}}
{\colortbl;\red0\green0\blue0;\red0\green0\blue255;}
\f0\fs24
\par \b 本地知识库助手 软件许可协议\b0
\par 
\par 本软件基于开源协议提供，用户可以自由使用、修改和分发。
\par 
\par \b 使用条款：\b0
\par 1. 本软件按"原样"提供，不提供任何明示或暗示的保证
\par 2. 用户可以自由使用本软件进行个人或商业用途
\par 3. 请确保系统已安装Ollama并下载相应的AI模型
\par 4. 本软件需要.NET 7.0运行环境
\par 
\par \b 免责声明：\b0
\par 作者不承担因使用本软件而导致的任何直接或间接损失。
\par 
\par 如果您同意以上条款，请点击"我接受"继续安装。
}
"@ | Out-File -FilePath "license.rtf" -Encoding ASCII

# 发布应用程序
Write-Host "⚙️ 发布应用程序..." -ForegroundColor Yellow
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o "publish"

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ 发布失败" -ForegroundColor Red
    exit 1
}

# 编译WiX
Write-Host "🔨 编译MSI安装包..." -ForegroundColor Yellow
& "$wixPath\candle.exe" installer.wxs -out installer.wixobj

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ WiX编译失败" -ForegroundColor Red
    exit 1
}

& "$wixPath\light.exe" installer.wixobj -ext WixUIExtension -out "LocalKnowledgeBase-v$Version.msi"

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ MSI生成失败" -ForegroundColor Red
    exit 1
}

# 清理临时文件
Remove-Item "installer.wxs", "installer.wixobj", "license.rtf" -Force -ErrorAction SilentlyContinue

Write-Host "✅ MSI安装包创建完成！" -ForegroundColor Green
Write-Host "📁 文件位置：LocalKnowledgeBase-v$Version.msi" -ForegroundColor Cyan

$msiSize = (Get-Item "LocalKnowledgeBase-v$Version.msi").Length / 1MB
Write-Host "📋 文件大小：$($msiSize.ToString("F1")) MB" -ForegroundColor Cyan

Write-Host ""
Write-Host "🎉 可以分发MSI安装包了！" -ForegroundColor Green
Write-Host "用户双击MSI文件即可安装应用程序" -ForegroundColor White
