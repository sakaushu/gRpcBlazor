#!/bin/bash

# ===============================================
# Docker Compose 停止スクリプト
# 使用方法:
#   ./stop.sh            → 停止のみ
#   ./stop.sh clean      → 停止 + コンテナ削除
#   ./stop.sh full       → 停止 + コンテナ/イメージ/ボリューム削除
# ===============================================

MODE="${1}"

case "${MODE}" in
  "")
    echo "Stopping containers only..."
    docker compose -f docker-compose.debug.yml stop
    ;;

  clean)
    echo "Stopping and removing containers..."
    docker compose -f docker-compose.debug.yml down
    ;;

  full)
    echo "FULL CLEAN: stopping + removing containers, images, and volumes..."
    docker compose -f docker-compose.debug.yml down --rmi all --volumes
    ;;

  *)
    echo "Unknown parameter: ${MODE}"
    echo "Usage: ./stop.sh [ clean | full ]"
    exit 1
    ;;
esac

echo "Done."
