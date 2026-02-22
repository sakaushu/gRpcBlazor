# ROBOCROS GATEWAY — README

概要
- このリポジトリは Docker Compose ベースで Blazor UI（Launcher）と gRPC サーバ（Core）、および nginx プロキシをまとめたゲートウェイ実装です。

主要ファイル（ルート）
|||
|-|-|
| `10-docker-gateway.rules` | udev / system 配置用ルール（運用環境向け）|
| `docker-compose.yml` | 開発用 compose（Linux 実機でビルド＆起動）|
| `docker-compose.release.deploy.yml` | リリース用デプロイ定義（配布先での起動）|
| `start.sh` / `stop.sh` | 開発用起動／停止スクリプト（Linux 実機）|
| `release.build.save.sh` | リリース用イメージをビルドして保存（WSL で実行）|
| `release.deploy.run.sh` | 配布先 Linux 実機でアーカイブを読み込み、サービスを起動|

## クイックスタート（開発 — Linux 実機）
```bash
# 開発用コンテナをビルド＆起動（Development 固定）
./start.sh

# 停止（全コンテナ）
./stop.sh full
```

## リリース作成（WSL）
```bash
chmod +x release.build.save.sh

# イメージをビルドしてまとめて圧縮（出力: release/release-images.tar.gz）
./release.build.save.sh
```

## リリース配布先での展開と起動（Linux 実機）
```bash
chmod +x release.deploy.run.sh

# アーカイブを読み込み、サービスを起動（Production 固定）
./release.deploy.run.sh
```

## 注意点 / 運用メモ
- D-Bus 連携: `grpc-server` はホストの D-Bus ソケットへアクセスする必要があります。配布先で動作させる場合、`docker-compose.release.deploy.yml` の `volumes:` に `/run/dbus/system_bus_socket` と `/etc/machine-id:ro` をマウントする設定を確認してください。
- ログ永続化: 開発用では `./logs/` をホストにマウントしています。リリース運用では `/share/gateway/...` 等、上書きされない共有領域を使うことを推奨します。
- イメージ整合: 配布アーカイブに含めるイメージタグ（例: `robocross/gateway-blazor:release`）と `docker-compose` の `image:` 設定が一致している必要があります。

### トラブルシュート
- nginx の設定警告（`listen 80 http2`）は警告であり動作に致命的ではありません。必要なら `http2` 指定に修正してください。
- `docker save` / `docker load` でエラーになる場合は、まずローカルで `docker images` を確認してください。
