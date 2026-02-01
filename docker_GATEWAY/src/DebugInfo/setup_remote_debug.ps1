# Hyper-V Docker リモートデバッグセットアップスクリプト (Windows PowerShell)
# 使用方法: .\setup_remote_debug.ps1 -HyperVIP <IP> -VMName <Hyper-V-VM-Name>

param(
    [Parameter(Mandatory=$false)]
    [string]$HyperVIP = "localhost",
    
    [Parameter(Mandatory=$false)]
    [string]$VMName = "Linux-Docker-VM",
    
    [Parameter(Mandatory=$false)]
    [switch]$BuildAndStart
)

$ErrorActionPreference = "Stop"
$ProjectDir = Split-Path -Parent $MyInvocation.MyCommand.Path

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "Hyper-V Docker リモートデバッグセットアップ" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Hyper-V IP: $HyperVIP" -ForegroundColor Yellow
Write-Host "VM Name: $VMName" -ForegroundColor Yellow
Write-Host "プロジェクトディレクトリ: $ProjectDir" -ForegroundColor Yellow
Write-Host ""

# ステップ1: Hyper-V VM の確認
Write-Host "? ステップ1: Hyper-V VM の確認" -ForegroundColor Green
try {
    $vm = Get-VM -Name $VMName -ErrorAction SilentlyContinue
    if ($vm) {
        Write-Host "  ? VM '$VMName' が見つかりました (状態: $($vm.State))" -ForegroundColor Green
    } else {
        Write-Host "  ? VM '$VMName' が見つかりません" -ForegroundColor Yellow
        Write-Host "  利用可能なVM:" -ForegroundColor Gray
        Get-VM | ForEach-Object { Write-Host "    - $($_.Name)" -ForegroundColor Gray }
    }
} catch {
    Write-Host "  ? Hyper-V にアクセスできません: $_" -ForegroundColor Yellow
}

# ステップ2: ネットワーク接続性テスト
Write-Host ""
Write-Host "? ステップ2: ネットワーク接続テスト" -ForegroundColor Green
$ports = @(5001, 4020)
foreach ($port in $ports) {
    try {
        $tcpClient = New-Object System.Net.Sockets.TcpClient
        $tcpClient.Connect($HyperVIP, $port)
        Write-Host "  ? ポート $port: 接続確認済み" -ForegroundColor Green
        $tcpClient.Close()
    } catch {
        Write-Host "  ? ポート $port: 接続失敗 (接続テスト)" -ForegroundColor Yellow
    }
}

# ステップ3: Hyper-V ポート転送設定（複数接続用）
Write-Host ""
Write-Host "? ステップ3: 推奨ポート転送設定 (PowerShell Admin)" -ForegroundColor Green
Write-Host ""
Write-Host "  【Hyper-V 外部ネットワークへのアクセス】" -ForegroundColor Cyan
Write-Host "  以下をPowerShell（管理者権限）で実行:" -ForegroundColor Gray
Write-Host ""
Write-Host "  # リスナーポート: 4020 -> Hyper-V内部ポート: 4020 (gRPC Debug)" -ForegroundColor Gray
Write-Host "  Add-NetNatStaticMapping -NatName 'Hyper-V NAT' -Protocol TCP -ExternalIPAddress 0.0.0.0 -ExternalPort 4020 -InternalIPAddress $HyperVIP -InternalPort 4020" -ForegroundColor Gray
Write-Host ""
Write-Host "  # リスナーポート: 5001 -> Hyper-V内部ポート: 5001 (gRPC Server)" -ForegroundColor Gray
Write-Host "  Add-NetNatStaticMapping -NatName 'Hyper-V NAT' -Protocol TCP -ExternalIPAddress 0.0.0.0 -ExternalPort 5001 -InternalIPAddress $HyperVIP -InternalPort 5001" -ForegroundColor Gray
Write-Host ""

# ステップ4: Docker の確認
Write-Host "? ステップ4: Docker の確認" -ForegroundColor Green

# ssh経由でHyper-VのDockerを確認
Write-Host "  Hyper-V 上のDocker確認..." -ForegroundColor Gray
Write-Host "  (要: SSH接続設定済み)" -ForegroundColor Gray

# ステップ5: Visual Studio での設定ガイド
Write-Host ""
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "Visual Studio でのリモートデバッガー設定" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "【手順】" -ForegroundColor Green
Write-Host "1. Visual Studio でプロジェクトを開く" -ForegroundColor White
Write-Host "2. デバッグ > プロセスにアタッチ (Ctrl+Alt+P)" -ForegroundColor White
Write-Host "3. 接続タイプ: TCP/IP (マネージド - .NET Core)" -ForegroundColor White
Write-Host "4. 接続ターゲット: $HyperVIP`:4020" -ForegroundColor Cyan
Write-Host "5. 更新をクリック" -ForegroundColor White
Write-Host "6. 表示されたプロセスから dotnet.exe または GATEWAY_Core を選択" -ForegroundColor White
Write-Host "7. アタッチをクリック" -ForegroundColor White
Write-Host ""

# ステップ6: docker-compose ビルド
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "Docker Compose ビルド" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""

if ($BuildAndStart) {
    Write-Host "ビルド開始..." -ForegroundColor Yellow
    $composeFile = Join-Path $ProjectDir "docker_GATEWAY" "docker-compose.debug.yml"
    
    if (Test-Path $composeFile) {
        Set-Location (Split-Path $composeFile -Parent)
        docker compose -f docker-compose.debug.yml build
        
        Write-Host ""
        Write-Host "コンテナ起動..." -ForegroundColor Yellow
        docker compose -f docker-compose.debug.yml up -d
        
        Write-Host ""
        Write-Host "? コンテナが起動しました" -ForegroundColor Green
        docker compose -f docker-compose.debug.yml ps
    } else {
        Write-Host "? docker-compose.debug.yml が見つかりません: $composeFile" -ForegroundColor Red
    }
} else {
    Write-Host "ビルドを実行するには -BuildAndStart フラグを使用してください" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "セットアップ完了" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "次のステップ:" -ForegroundColor Green
Write-Host "  1. Hyper-V VM 上で docker compose を起動" -ForegroundColor White
Write-Host "  2. Visual Studio でリモートデバッガーにアタッチ" -ForegroundColor White
Write-Host "  3. ブレークポイントを設定してデバッグ開始" -ForegroundColor White
