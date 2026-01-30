#!/bin/bash

# 実機用停止スクリプト
# 実機環境のコンテナを停止します

echo "==================================="
echo "実機環境のコンテナを停止します"
echo "==================================="

docker compose -f docker-compose.prod.yml down

echo ""
echo "✓ コンテナを停止しました"
