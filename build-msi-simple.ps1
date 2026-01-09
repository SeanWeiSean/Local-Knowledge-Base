# 构建MSI安装包脚本
param(
    [string]$Configuration = "Release",
    [string]$Platform = "x64"
)

Write-Host "正在构建本地知识库助手MSI安装包..." -ForegroundColor Green

# 设置工作目录
$ProjectDir = Get-Location
$PublishDir = Join-Path $ProjectDir "bin\$Configuration\net7.0-windows\publish"
$SetupDir = Join-Path $ProjectDir "Setup"

try {
    # 清理之前的构建
    Write-Host "清理之前的构建文件..." -ForegroundColor Yellow
    if (Test-Path $PublishDir) {
        Remove-Item $PublishDir -Recurse -Force
    }
    if (Test-Path $SetupDir) {
        Remove-Item $SetupDir -Recurse -Force
    }
    
    # 发布应用程序
    Write-Host "发布应用程序..." -ForegroundColor Yellow
    dotnet publish LocalKnowledgeBase.csproj `
        --configuration $Configuration `
        --runtime win-$Platform `
        --self-contained true `
        --output $PublishDir `
        /p:PublishSingleFile=true `
        /p:IncludeNativeLibrariesForSelfExtract=true `
        /p:PublishTrimmed=false

    if ($LASTEXITCODE -ne 0) {
        throw "应用程序发布失败"
    }

    Write-Host "应用程序发布完成: $PublishDir" -ForegroundColor Green

    # 创建Heat文件来自动生成组件
    Write-Host "生成WiX组件文件..." -ForegroundColor Yellow
    $HeatOutput = Join-Path $ProjectDir "HarvestedFiles.wxs"
    
    wix extension add WixToolset.Heat.wixext
    wix heat dir $PublishDir `
        -out $HeatOutput `
        -cg PublishedFiles `
        -dr INSTALLFOLDER `
        -scom -sreg -srd -var var.PublishDir `
        -gg -sfrag -template fragment

    if ($LASTEXITCODE -ne 0) {
        throw "WiX Heat 组件生成失败"
    }

    # 创建简化的Product.wxs文件
    $ProductWxs = @"
<?xml version="1.0" encoding="UTF-8"?>
<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs">
  <Package Name="本地知识库助手" 
           Manufacturer="LocalKnowledgeBase" 
           Version="1.0.0.0" 
           UpgradeCode="{A1B2C3D4-E5F6-7890-ABCD-123456789012}"
           Language="2052">
    
    <MajorUpgrade DowngradeErrorMessage="已安装更新的版本。" />
    <MediaTemplate EmbedCab="yes" />
    
    <Feature Id="ProductFeature" Title="本地知识库助手" Level="1">
      <ComponentGroupRef Id="PublishedFiles" />
      <ComponentRef Id="DesktopShortcut" />
    </Feature>

    <Property Id="WixShellExecTarget" Value="[INSTALLFOLDER]LocalKnowledgeBase.exe" />
    <CustomAction Id="LaunchApplication" BinaryKey="WixCA" DllEntry="WixShellExec" Impersonate="yes" />
    
    <Property Id="ARPPRODUCTICON" Value="icon.ico" />
  </Package>

  <Fragment>
    <Directory Id="TARGETDIR" Name="SourceDir">
      <Directory Id="ProgramFilesFolder">
        <Directory Id="INSTALLFOLDER" Name="本地知识库助手" />
      </Directory>
      <Directory Id="DesktopFolder" Name="Desktop" />
    </Directory>
  </Fragment>

  <Fragment>
    <Component Id="DesktopShortcut" Directory="DesktopFolder">
      <Shortcut Id="DesktopShortcut"
                Name="本地知识库助手"
                Target="[INSTALLFOLDER]LocalKnowledgeBase.exe"
                WorkingDirectory="INSTALLFOLDER" />
      <RemoveFolder Id="RemoveDesktopFolder" Directory="DesktopFolder" On="uninstall" />
      <RegistryValue Root="HKCU"
                     Key="Software\LocalKnowledgeBase"
                     Name="DesktopShortcut"
                     Type="integer"
                     Value="1"
                     KeyPath="yes" />
    </Component>
  </Fragment>
</Wix>
"@

    $ProductWxs | Out-File -FilePath (Join-Path $ProjectDir "SimpleProduct.wxs") -Encoding UTF8

    # 构建MSI
    Write-Host "构建MSI安装包..." -ForegroundColor Yellow
    New-Item -ItemType Directory -Path $SetupDir -Force | Out-Null
    
    $MsiOutput = Join-Path $SetupDir "LocalKnowledgeBaseSetup-v1.0.0-$Platform.msi"
    
    wix build `
        (Join-Path $ProjectDir "SimpleProduct.wxs") `
        (Join-Path $ProjectDir "HarvestedFiles.wxs") `
        -out $MsiOutput `
        -d PublishDir=$PublishDir `
        -ext WixToolset.UI.wixext

    if ($LASTEXITCODE -ne 0) {
        throw "MSI构建失败"
    }

    Write-Host "MSI安装包构建成功!" -ForegroundColor Green
    Write-Host "安装包位置: $MsiOutput" -ForegroundColor Cyan
    Write-Host "文件大小: $([Math]::Round((Get-Item $MsiOutput).Length / 1MB, 2)) MB" -ForegroundColor Cyan

    # 清理临时文件
    if (Test-Path $HeatOutput) {
        Remove-Item $HeatOutput -Force
    }
    if (Test-Path (Join-Path $ProjectDir "SimpleProduct.wxs")) {
        Remove-Item (Join-Path $ProjectDir "SimpleProduct.wxs") -Force
    }

    Write-Host "`n安装包创建完成! 🎉" -ForegroundColor Green
    Write-Host "你可以双击 $MsiOutput 来安装应用程序" -ForegroundColor Yellow

} catch {
    Write-Error "构建过程中发生错误: $($_.Exception.Message)"
    exit 1
}
