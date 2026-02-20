#!/bin/bash

# Dockerイメージを保存するスクリプト
# ビルド済みのイメージをtarファイルとして保存します

# 保存先ディレクトリ
OUTPUT_DIR="./docker_images"
mkdir -p "$OUTPUT_DIR"

echo "==================================="
echo "Dockerイメージの保存を開始します"
echo "==================================="

# docker-composeでビルド
echo ""
echo "[1/4] Dockerイメージをビルドしています..."
docker compose build

if [ $? -ne 0 ]; then
    echo "エラー: ビルドに失敗しました"
    exit 1
fi

# イメージ名の取得
GRPC_IMAGE=$(docker compose images -q grpc-server)
BLAZOR_IMAGE=$(docker compose images -q blazor-client)
NGINX_IMAGE="nginx:latest"

echo ""
echo "[2/4] gRPC Serverイメージを保存しています..."
docker save -o "$OUTPUT_DIR/grpc-server.tar" docker_gateway-grpc-server:latest

echo ""
echo "[3/4] Blazor Clientイメージを保存しています..."
docker save -o "$OUTPUT_DIR/blazor-client.tar" docker_gateway-blazor-client:latest

echo ""
echo "[4/4] Nginxイメージを保存しています..."
docker save -o "$OUTPUT_DIR/nginx.tar" $NGINX_IMAGE

echo ""
echo "==================================="
echo "保存が完了しました！"
echo "==================================="
echo ""
echo "保存されたファイル:"
ls -lh "$OUTPUT_DIR"/*.tar

echo ""
echo "ファイルサイズの合計:"
du -sh "$OUTPUT_DIR"

echo ""
echo "-----------------------------------"
echo "実機への転送方法:"
echo "-----------------------------------"
echo "1. docker_imagesフォルダを実機にコピー"
echo "   例: scp -r docker_images/ user@target-machine:/path/to/destination/"
echo ""
echo "2. 実機で load_images.sh を実行"
echo "   ./load_images.sh"
echo "-----------------------------------"
