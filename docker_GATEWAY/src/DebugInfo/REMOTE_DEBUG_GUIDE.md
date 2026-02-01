# Hyper-V Docker リモートデバッグセットアップガイド

このガイドは、Windows上の Visual Studio から Hyper-V上のLinuxで実行しているDockerコンテナの .NET 10 Blazor アプリケーションをリモートデバッグする手順を説明します。

## ?? 前提条件

- ? Windows 10/11 with Hyper-V 有効化
- ? Hyper-V上のLinux VM (Ubuntu/Debian) にDockerがインストール済み
- ? Visual Studio 2022 (.NET 10 サポート)
- ? gRpcBlazor プロジェクト構造がセットアップ済み

## ??? アーキテクチャ

```
┌─────────────────────────────────────┐
│        Windows 開発環境              │
│   (Visual Studio 2022 .NET 10)      │
│                                     │
│  Port 4020 (gRPC Debug)             │
│  Port 4021 (Blazor Debug)           │
│  Port 5001 (gRPC API)               │
│  Port 5000 (Blazor Web)             │
└──────────────┬──────────────────────┘
               │ TCP/IP
               │ (ネットワーク)
┌──────────────▼──────────────────────┐
│       Hyper-V Linux VM               │
│   (Docker Container Runtime)         │
│                                      │
│  ┌────────────────────────────────┐ │
│  │   grpc-server-debug            │ │
│  │  (GATEWAY_Core)                │ │
│  │  Port 5001 → 5001              │ │
│  │  Port 4020 → 4020 (Debug)      │ │
│  │  VSDBG: /vsdbg                 │ │
│  └────────────────────────────────┘ │
│                                      │
│  ┌────────────────────────────────┐ │
│  │   blazor-client-debug          │ │
│  │  (GATEWAY_Launcher)            │ │
│  │  Port 5000 → 5000              │ │
│  │  Port 4021 → 4021 (Debug)      │ │
│  │  VSDBG: /vsdbg                 │ │
│  └────────────────────────────────┘ │
└─────────────────────────────────────┘
```

## ?? セットアップ手順

### ステップ 1: ワークスペースの構成を確認

```powershell
cd C:\workspace\kabe\gRpcBlazor
dir docker_GATEWAY\
```

必要なファイル:
- `docker-compose.debug.yml`
- `docker/GATEWAY_Core/Dockerfile.debug`
- `docker/GATEWAY_Launcher/Dockerfile.debug`
- `setup_remote_debug.ps1` (セットアップスクリプト)

### ステップ 2: docker-compose.debug.yml にデバッグポートを追加

必要な設定:

```yaml
services:
  grpc-server:
    ports:
      - "5001:5001"
      - "4020:4020"    # ← デバッグポート追加
    cap_add:
      - SYS_PTRACE     # デバッグに必須
    security_opt:
      - seccomp:unconfined

  blazor-client:
    ports:
      - "5000:5000"
      - "4021:4021"    # ← デバッグポート追加
    cap_add:
      - SYS_PTRACE
    security_opt:
      - seccomp:unconfined
```

### ステップ 3: Dockerfile.debug にEXPOSEを追加

**docker/GATEWAY_Core/Dockerfile.debug:**
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS base
WORKDIR /app
EXPOSE 5001 4020  # ← デバッグポート追加
```

**docker/GATEWAY_Launcher/Dockerfile.debug:**
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS base
WORKDIR /app
EXPOSE 5000 4021  # ← デバッグポート追加
```

### ステップ 4: Hyper-V VM の IP アドレスを確認

**Windows PowerShell (管理者権限):**

```powershell
# Hyper-V VM のIPアドレスを確認
Get-VM -Name "Linux-Docker-VM" | Get-VMNetworkAdapter | Select-Object IPAddresses

# または SSH経由で確認
ssh user@<VM-IP> "hostname -I"
```

### ステップ 5: プロジェクトファイルを Hyper-V に転送

**Windows PowerShell から実行:**

```powershell
# プロジェクトフォルダ全体を Hyper-V の Debian Linux に転送
cd C:\workspace\kabe\gRpcBlazor
scp -r docker_GATEWAY robocross@172.18.81.73:/home/robocross/

# または rsync を使用（差分転送、高速）
# ※ Windows で rsync を使う場合は WSL2 または Git Bash が必要
wsl rsync -avz --progress docker_GATEWAY/ robocross@172.18.81.73:/home/robocross/docker_GATEWAY/
```

**転送されるフォルダ・ファイル:**
```
docker_GATEWAY/
├── docker/               # Dockerfile が含まれる
├── src/                  # C# ソースコード全体
├── docker-compose.debug.yml
├── start_debug.sh        # Debian で実行するスクリプト
├── stop_debug.sh
└── その他の .sh ファイル
```

**転送の確認（Debian 側で実行）:**
```bash
ssh robocross@172.18.81.73
cd /home/robocross/docker_GATEWAY
ls -la
```

### ステップ 6: ネットワークを設定（NAT使用の場合）

Hyper-V NAT ネットワークを使用する場合、ポート転送を設定:

**PowerShell (管理者権限):**

```powershell
# NAT を確認
Get-NetNat

# ポート転送ルールを追加
# gRPC Debug (4020)
Add-NetNatStaticMapping -NatName "Hyper-V NAT" `
  -Protocol TCP `
  -ExternalIPAddress 0.0.0.0 `
  -ExternalPort 4020 `
  -InternalIPAddress <HYPER-V-IP> `
  -InternalPort 4020

# Blazor Debug (4021)
Add-NetNatStaticMapping -NatName "Hyper-V NAT" `
  -Protocol TCP `
  -ExternalIPAddress 0.0.0.0 `
  -ExternalPort 4021 `
  -InternalIPAddress <HYPER-V-IP> `
  -InternalPort 4021

# gRPC API (5001)
Add-NetNatStaticMapping -NatName "Hyper-V NAT" `
  -Protocol TCP `
  -ExternalIPAddress 0.0.0.0 `
  -ExternalPort 5001 `
  -InternalIPAddress <HYPER-V-IP> `
  -InternalPort 5001

# 設定を確認
Get-NetNatStaticMapping
```

### ステップ 7: Hyper-V VM でDockerを起動

**SSH経由でVM に接続:**

```bash
ssh robocross@172.18.81.73

# 転送したディレクトリに移動
cd /home/robocross/docker_GATEWAY

# スクリプトに実行権限を付与
chmod +x *.sh

# start_debug.sh でビルド・起動（推奨）
./start_debug.sh

# または手動で docker-compose を実行
docker compose -f docker-compose.debug.yml build
docker compose -f docker-compose.debug.yml up -d

# コンテナが起動したか確認
docker compose -f docker-compose.debug.yml ps

# ログ確認
docker compose -f docker-compose.debug.yml logs -f grpc-server
```

### ステップ 8: Visual Studio でリモートデバッガーにアタッチ

1. **Visual Studio で GATEWAY_Core プロジェクトを開く**

2. **デバッグ メニュー → プロセスにアタッチ** (Ctrl+Alt+P)

3. **接続タイプを選択:**
   - `TCP/IP (マネージド、.NET Core 用)`

4. **接続ターゲットを入力:**
   ```
   <HYPER-V-IP>:4020
   ```

5. **更新 ボタンをクリック**
   
   利用可能なプロセスが表示されます:
   ```
   [ID] GATEWAY_Core.dll
   [ID] dotnet (GATEWAY_Core)
   ```

6. **プロセスを選択 → アタッチ**

7. **ブレークポイントを設定**

   GATEWAY_Core 側のコード (`gRPC Services` など) にブレークポイントを設定します。

8. **クライアント側から API を呼び出す**

   Blazor クライアントから gRPC を呼び出すと、デバッガーがブレークポイントで停止します。

## ?? トラブルシューティング

### 接続できない

**確認事項:**

1. **ネットワーク確認**
   ```powershell
   Test-NetConnection -ComputerName <HYPER-V-IP> -Port 4020
   ```

2. **ファイアウォール確認**
   ```powershell
   Get-NetFirewallRule -DisplayName "*Remote Debugger*" | Select-Object Name, Enabled
   ```

3. **ポート転送ルール確認**
   ```powershell
   Get-NetNatStaticMapping
   ```

### VSDBG が見つからない

Dockerfile.debug で VSDBG のインストールが失敗している可能性があります:

```dockerfile
# デバッグ用ラッパースクリプトで確認
RUN if [ ! -d "/vsdbg" ]; then \
      echo "VSDBG インストール失敗"; \
      exit 1; \
    fi
```

### デバッガーがシンボルを読み込めない

`.pdb` ファイルが含まれていることを確認:

```dockerfile
# publish 時に Debug 設定を使用
RUN dotnet publish "GATEWAY_Core/GATEWAY_Core.csproj" -c Debug -o /app/publish
```

## ?? 動作確認

### コンテナログの確認

```bash
# gRPC サーバー
docker compose -f docker-compose.debug.yml logs -f grpc-server

# Blazor クライアント
docker compose -f docker-compose.debug.yml logs -f blazor-client
```

### ネットワーク接続の確認

```powershell
# gRPC API の確認
Invoke-WebRequest -Uri "http://<HYPER-V-IP>:5001" -SkipCertificateCheck

# Blazor Web の確認
Start-Process "http://<HYPER-V-IP>:5000"
```

## ?? 停止・クリーンアップ

```bash
# コンテナの停止
docker compose -f docker-compose.debug.yml down

# イメージの削除（必要な場合）
docker compose -f docker-compose.debug.yml down --rmi all
```

## ?? 参考資料

- [Visual Studio リモートデバッガー](https://learn.microsoft.com/ja-jp/visualstudio/debugger/remote-debugging?view=vs-2022)
- [Docker での .NET アプリケーション](https://learn.microsoft.com/ja-jp/dotnet/core/docker/)
- [gRPC と .NET](https://learn.microsoft.com/ja-jp/aspnet/core/grpc/)

## ? チェックリスト

- [ ] docker-compose.debug.yml にデバッグポートを追加
- [ ] Dockerfile.debug に EXPOSE でポート記述
- [ ] cap_add と security_opt をセット
- [ ] Hyper-V VM が起動している
- [ ] ネットワーク接続が確立している
- [ ] ポート転送ルールが設定済み
- [ ] Docker コンテナが起動している
- [ ] Visual Studio でリモートデバッガーにアタッチ
- [ ] ブレークポイントが設定されている

---

**最後更新**: 2026年2月1日
**対応環境**: .NET 10, Docker, Hyper-V, Visual Studio 2022
