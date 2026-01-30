#!/bin/bash

# 実機用起動スクリプト
# ビルド済みイメージを使用してコンテナを起動します

echo "==================================="
echo "実機環境でコンテナを起動します"
echo "==================================="

# イメージの存在確認
echo ""
echo "イメージの確認..."
GRPC_IMAGE=$(docker images -q docker_gateway-grpc-server:latest)
BLAZOR_IMAGE=$(docker images -q docker_gateway-blazor-client:latest)
NGINX_IMAGE=$(docker images -q nginx:latest)

if [ -z "$GRPC_IMAGE" ]; then
    echo "警告: docker_gateway-grpc-server:latest が見つかりません"
    echo "先に ./load_images.sh を実行してください"
    exit 1
fi

if [ -z "$BLAZOR_IMAGE" ]; then
    echo "警告: docker_gateway-blazor-client:latest が見つかりません"
    echo "先に ./load_images.sh を実行してください"
    exit 1
fi

if [ -z "$NGINX_IMAGE" ]; then
    echo "警告: nginx:latest が見つかりません"
    echo "先に ./load_images.sh を実行してください"
    exit 1
fi

echo "✓ 必要なイメージが揃っています"

# コンテナ起動
echo ""
echo "コンテナを起動しています..."
docker compose -f docker-compose.prod.yml up -d

if [ $? -eq 0 ]; then
    echo ""
    echo "==================================="
    echo "✓ 起動に成功しました！"
    echo "==================================="
    echo ""
    docker compose -f docker-compose.prod.yml ps
    echo ""
    echo "ブラウザでアクセス: http://localhost"
else
    echo ""
    echo "エラー: 起動に失敗しました"
    exit 1
fi
