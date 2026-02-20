#!/bin/bash
# デバッグ用Docker環境を起動（--buildで自動的にデバッグビルド）
docker compose -f docker-compose.debug.yml up --build -d
echo "Debug containers started."
echo "SSH: robox@$(hostname -I | awk '{print $1}') (password: robocross)"
docker ps --filter "name=grpc-server-debug\|blazor-client-debug" --format "table {{.Names}}\t{{.Status}}"
