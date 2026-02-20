# ROBOCROS GATEWAY — README

概要
- このリポジトリは Docker Compose ベースで Blazor UI（Launcher）と gRPC サーバ（Core）、および nginx プロキシをまとめたゲートウェイ実装です。

主要ファイル（ルート）
|||
|-|-|
| `10-docker-gateway.rules` | udev / system 配置用ルール（運用環境向け）|
| `docker-compose.yml` | 開発用 compose（ホストのログをマウント）|
| `docker-compose.release.build.yml` | リリース用ビルド定義（イメージをビルド）|
| `docker-compose.release.deploy.yml` | リリース用デプロイ定義（配布先での起動）|
| `start.sh` / `stop.sh` | 開発用起動／停止スクリプト|
| `release.build.save.sh` | リリース用イメージをビルドして保存（`release/release-images.tar.gz` を作成）|
| `release.deploy.run.sh` | 配布先でアーカイブを読み込み、サービスを起動／停止するスクリプト|
| `save_images.sh` / `load_images.sh` | 補助スクリプト（イメージの個別保存／読み込み）|

## クイックスタート（開発）
```bash
# 開発用コンテナをビルド＆起動
./start.sh

# 停止（全コンテナ）
./stop.sh full
```

## リリース作成（ローカルまたは CI）
```bash
# 実行権付与
chmod +x release.build.save.sh

# リリースイメージをビルドしてまとめて圧縮（出力: release/release-images.tar.gz）
./release.build.save.sh
```

## リリース配布先での展開と起動
```bash
# 配布先でアーカイブを配置（scp 等で転送）
# 実行権を付与
chmod +x release.deploy.run.sh

# アーカイブを読み込み、サービスを起動
./release.deploy.run.sh up

# 停止
./release.deploy.run.sh down
```

## 注意点 / 運用メモ
- D-Bus 連携: `grpc-server` はホストの D-Bus ソケットへアクセスする必要があります。配布先で動作させる場合、`docker-compose.release.deploy.yml` の `volumes:` に `/run/dbus/system_bus_socket` と `/etc/machine-id:ro` をマウントする設定を確認してください。
- ログ永続化: 開発用では `./logs/` をホストにマウントしています。リリース運用では `/share/gateway/...` 等、上書きされない共有領域を使うことを推奨します。
- イメージ整合: 配布アーカイブに含めるイメージタグ（例: `robocross/gateway-blazor:release`）と `docker-compose` の `image:` 設定が一致している必要があります。

### トラブルシュート
- nginx の設定警告（`listen 80 http2`）は警告であり動作に致命的ではありません。必要なら `http2` 指定に修正してください。
- `docker save` / `docker load` でエラーになる場合は、まずローカルで `docker images` を確認してください。
