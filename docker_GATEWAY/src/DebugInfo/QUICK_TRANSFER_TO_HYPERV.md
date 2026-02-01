# Hyper-V へのクイック転送ガイド

`docker_GATEWAY` プロジェクト全体を Hyper-V Linux VM に転送する手順です。

---

## ?? 前提条件

- ? Hyper-V VM (Linux/Debian/Ubuntu) が起動している
- ? SSH がインストール・起動している
- ? SSH キー認証が設定されている（またはパスワード認証）
- ? Docker がインストール済み

---

## ?? ステップ1: Hyper-V VM の IP アドレスを確認

### PowerShell で確認

```powershell
# 管理者権限で実行
Get-VM -Name "VM-NAME" | Get-VMNetworkAdapter | Select-Object IPAddresses
```

**例出力:**
```
IPAddresses
-----------
{172.18.81.73}
```

### または SSH で確認

```bash
# SSH で接続後に実行
hostname -I
```

---

## ?? ステップ2: プロジェクトを転送

開発環境（Windows）から以下を実行します。

### 方法A: SCP で転送（シンプル）

```bash
cd C:\workspace\kabe\gRpcBlazor

scp -r docker_GATEWAY <USER>@<VM-IP>:/home/<USER>/
```

**例:**
```bash
scp -r docker_GATEWAY robocross@172.18.81.73:/home/robocross/
```

### 方法B: rsync で転送（高速・差分）

WSL2 または Git Bash で実行：

```bash
cd C:\workspace\kabe\gRpcBlazor

wsl rsync -avz --progress docker_GATEWAY/ <USER>@<VM-IP>:/home/<USER>/docker_GATEWAY/
```

**例:**
```bash
wsl rsync -avz --progress docker_GATEWAY/ robocross@172.18.81.73:/home/robocross/docker_GATEWAY/
```

---

## ? ステップ3: 転送確認

SSH で VM に接続して確認：

```bash
ssh robocross@172.18.81.73

# 転送確認
cd /home/robocross/docker_GATEWAY
ls -la

# 重要ファイルの確認
ls -la docker-compose.debug.yml start_debug.sh docker/
```

**期待される出力:**
```
-rw-r--r-- docker-compose.debug.yml
-rwxr-xr-x start_debug.sh
drwxr-xr-x docker/
drwxr-xr-x src/
```

---

## ?? ステップ4: Docker コンテナを起動

VM 上で以下を実行：

```bash
cd /home/robocross/docker_GATEWAY

# 実行権限を付与
chmod +x *.sh

# デバッグモードで起動（推奨）
./start_debug.sh

# または
docker compose -f docker-compose.debug.yml up --build -d
```

### 起動確認

```bash
# コンテナ状態確認
docker compose -f docker-compose.debug.yml ps

# ログ確認
docker compose -f docker-compose.debug.yml logs -f
```

**期待される出力:**
```
NAME              STATUS      PORTS
grpc-server-debug Up 2 minutes 0.0.0.0:5001->5001/tcp, 0.0.0.0:4020->4020/tcp
blazor-client-debug Up 2 minutes 0.0.0.0:5000->5000/tcp, 0.0.0.0:4021->4021/tcp
nginx-proxy       Up 2 minutes 0.0.0.0:80->80/tcp
```

---

## ?? ステップ5: Visual Studio でデバッグ接続

### ローカルネットワーク接続の場合

1. Visual Studio を開く
2. **デバッグ** → **プロセスにアタッチ** (Ctrl+Alt+P)
3. **接続型:** `TCP/IP (マネージド - .NET Core)`
4. **接続先:** `<VM-IP>:4020` (gRPC) または `<VM-IP>:4021` (Blazor)
5. **更新** をクリック
6. 表示されたプロセスから `dotnet` を選択
7. **アタッチ** をクリック

**例:**
```
接続先: 172.18.81.73:4020
```

### Hyper-V NAT の場合

ポート転送を設定（管理者 PowerShell）：

```powershell
# gRPC Debug ポート
Add-NetNatStaticMapping -NatName "Hyper-V NAT" `
  -Protocol TCP -ExternalIPAddress 0.0.0.0 `
  -ExternalPort 4020 -InternalIPAddress 172.18.81.73 -InternalPort 4020

# Blazor Debug ポート
Add-NetNatStaticMapping -NatName "Hyper-V NAT" `
  -Protocol TCP -ExternalIPAddress 0.0.0.0 `
  -ExternalPort 4021 -InternalIPAddress 172.18.81.73 -InternalPort 4021

# 確認
Get-NetNatStaticMapping
```

その後、Visual Studio で `localhost:4020` または `localhost:4021` に接続

---

## ?? トラブルシューティング

### SSH 接続拒否

```bash
# SSH キー権限を確認
chmod 600 ~/.ssh/id_rsa
chmod 700 ~/.ssh

# SSH デバッグ
ssh -vvv robocross@172.18.81.73
```

### Docker が起動していない

```bash
# VM 上で実行
sudo service docker start
# または
sudo systemctl start docker
```

### ポートが使用中

```bash
# ポート確認
ss -tlnp | grep -E "5001|5000|4020|4021"

# 既存コンテナを停止
docker compose down
```

### 転送がハング

- ネットワーク接続を確認
- ファイアウォール設定を確認
- `Ctrl+C` で中断後、`rsync` で再開（差分転送）

---

## ?? 参考

- 詳細な手順: `REMOTE_DEBUG_GUIDE.md`
- 自動セットアップ: `setup_remote_debug.ps1` / `setup_remote_debug.sh`
