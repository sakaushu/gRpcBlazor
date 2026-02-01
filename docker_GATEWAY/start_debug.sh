#!/bin/bash
# デバッグ用Docker環境を起動（--buildで自動的にデバッグビルド）
echo "Starting debug Docker containers..."
docker-compose -f docker-compose.debug.yml up --build -d
echo "Debug containers started. You can now attach Visual Studio debugger."
echo "grpc-server: container name 'grpc-server-debug'"
echo "blazor-client: container name 'blazor-client-debug'"
