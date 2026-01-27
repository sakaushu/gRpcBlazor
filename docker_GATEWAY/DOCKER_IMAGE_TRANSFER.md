# Dockerイメージの保存・転送手順

このドキュメントでは、Dockerイメージをファイルとして保存し、実機に転送する方法を説明します。

## 前提条件

- Docker、Docker Composeがインストールされていること
- 開発環境でイメージがビルドできること
- 実機にもDockerがインストールされていること

## 手順

### 1. 開発環境でイメージを保存

```bash
# 実行権限を付与（初回のみ）
chmod +x save_images.sh load_images.sh

# イメージをビルドして保存
./save_images.sh
```

このスクリプトは以下を実行します：
- `docker compose build` でイメージをビルド
- 各イメージを `docker_images/` ディレクトリに `.tar` ファイルとして保存
  - `grpc-server.tar` (gRPC Server)
  - `blazor-client.tar` (Blazor Client)
  - `nginx.tar` (Nginx)

### 2. 実機にファイルを転送

#### 方法A: SCP/SFTP経由で転送

```bash
# プロジェクト全体を転送
scp -r /home/shu2syu2/gRpcBlazor2/docker_GATEWAY user@target-machine:/path/to/destination/

# または、必要なファイルのみ転送
cd /home/shu2syu2/gRpcBlazor2/docker_GATEWAY
tar czf gateway.tar.gz docker_images/ docker-compose.yml load_images.sh start.sh stop.sh docker/ logs/ src/
scp gateway.tar.gz user@target-machine:/path/to/destination/
```

#### 方法B: USBメモリ経由で転送

```bash
# USBメモリをマウント（例：/media/usb）
cp -r docker_images /media/usb/
cp docker-compose.yml load_images.sh start.sh stop.sh /media/usb/
cp -r docker /media/usb/
cp -r logs /media/usb/
```

#### 方法C: rsync経由で転送

```bash
rsync -avz --progress /home/shu2syu2/gRpcBlazor2/docker_GATEWAY/ user@target-machine:/path/to/destination/
```

### 3. 実機でイメージを読み込み

実機に接続して以下を実行：

```bash
# 転送したディレクトリに移動
cd /path/to/destination/docker_GATEWAY

# 実行権限を付与（必要な場合）
chmod +x load_images.sh start.sh stop.sh

# イメージを読み込む
./load_images.sh
```

### 4. コンテナを起動

```bash
# コンテナを起動
./start.sh

# または
docker compose up -d
```

### 5. 動作確認

```bash
# コンテナの状態確認
docker compose ps

# ログ確認
docker compose logs -f

# ブラウザで確認
# http://実機のIPアドレス
```

## ファイルサイズの目安

各イメージファイルのサイズ目安：
- `grpc-server.tar`: 約200-500MB (.NETランタイム含む)
- `blazor-client.tar`: 約200-500MB (.NETランタイム含む)
- `nginx.tar`: 約140-180MB

合計: 約500MB-1.2GB

## トラブルシューティング

### イメージの確認

```bash
# 読み込まれたイメージの一覧
docker images | grep docker_gateway

# または
docker images
```

### イメージの削除（再転送する場合）

```bash
# 特定のイメージを削除
docker rmi docker_gateway-grpc-server:latest
docker rmi docker_gateway-blazor-client:latest

# または全て削除
docker compose down --rmi all
```

### ディスク容量の確認

```bash
# Docker使用状況の確認
docker system df

# 不要なデータのクリーンアップ
docker system prune -a
```

## 自動化（オプション）

### 定期的なイメージ更新

```bash
# crontabで定期実行する例（開発環境）
0 2 * * * cd /home/shu2syu2/gRpcBlazor2/docker_GATEWAY && ./save_images.sh
```

### ワンライナーでの転送

```bash
# 保存→圧縮→転送を一度に実行
./save_images.sh && tar czf gateway_images.tar.gz docker_images/ && scp gateway_images.tar.gz user@target:/tmp/
```

## 注意事項

- イメージファイルは大きいため、転送には時間がかかる場合があります
- 実機のディスク容量を事前に確認してください
- セキュリティ上、SSH鍵認証を使用することを推奨します
- 実機のネットワーク設定（ポート、ファイアウォール）を確認してください

## 参考コマンド

```bash
# イメージを手動で保存
docker save -o image.tar イメージ名:タグ

# イメージを手動で読み込み
docker load -i image.tar

# 複数のイメージを一度に保存
docker save -o all-images.tar イメージ1 イメージ2 イメージ3

# イメージの詳細情報
docker inspect イメージ名
```
