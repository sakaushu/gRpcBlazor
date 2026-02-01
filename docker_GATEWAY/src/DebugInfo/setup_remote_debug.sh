#!/bin/bash

# Hyper-V Linux Docker リモートデバッグセットアップスクリプト
# 使用方法: ./setup_remote_debug.sh <HYPER-V-IP-ADDRESS>

set -e

HYPER_V_IP="${1:-localhost}"
PROJECT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

echo "=========================================="
echo "Hyper-V Docker リモートデバッグセットアップ"
echo "=========================================="
echo "Hyper-V IP: $HYPER_V_IP"
echo "プロジェクトディレクトリ: $PROJECT_DIR"
echo ""

# ステップ1: docker-compose.debug.yml にデバッグポートを追加（確認）
echo "? ステップ1: docker-compose.debug.yml の確認"
if grep -q "4020:4020" docker-compose.debug.yml; then
    echo "  ? gRPC サーバーのデバッグポート (4020) が設定済み"
else
    echo "  ? gRPC サーバーのデバッグポート (4020) が未設定"
fi

# ステップ2: Dockerfile.debug にVSDEBUG_TRANSPORT環境変数追加
echo ""
echo "? ステップ2: デバッグ環境変数の設定を確認"

# ステップ3: ポート転送設定
echo ""
echo "? ステップ3: Hyper-V への接続テスト"
if timeout 2 bash -c "cat < /dev/null > /dev/tcp/$HYPER_V_IP/5001" 2>/dev/null; then
    echo "  ? Hyper-V との接続確認: OK (5001ポート)"
else
    echo "  ? Hyper-V との接続確認: 失敗"
    echo "    確認事項:"
    echo "    1. Hyper-V が起動しているか"
    echo "    2. ファイアウォール設定を確認"
    echo "    3. 正しいIPアドレスか"
fi

# ステップ4: 推奨される接続情報を表示
echo ""
echo "=========================================="
echo "Visual Studio での設定"
echo "=========================================="
echo ""
echo "【リモートデバッガーアタッチ手順】"
echo ""
echo "1. Visual Studio で: デバッグ > プロセスにアタッチ"
echo "2. 接続タイプ: TCP/IP (マネージド、.NET Core 用)"
echo "3. 接続ターゲット:"
echo "   $HYPER_V_IP:4020"
echo ""
echo "4. 接続後、プロセスを選択:"
echo "   - GATEWAY_Core.dll または dotnet.exe"
echo ""
echo "【環境変数の設定】"
echo "  VSDBG_LOG_DIR=/tmp/vsdbg_logs"
echo "  VSDBG_LOG_STDERR=true"
echo ""

# ステップ5: Docker Compose ビルド & 起動
echo "=========================================="
echo "Docker Compose ビルド & 起動"
echo "=========================================="
echo ""
read -p "Docker コンテナを起動しますか？ (y/n) " -n 1 -r
echo
if [[ $REPLY =~ ^[Yy]$ ]]; then
    echo "ビルド開始..."
    docker compose -f docker-compose.debug.yml build
    
    echo ""
    echo "コンテナ起動..."
    docker compose -f docker-compose.debug.yml up -d
    
    echo ""
    echo "? コンテナが起動しました"
    docker compose -f docker-compose.debug.yml ps
    
    echo ""
    echo "ログ確認:"
    echo "  docker compose -f docker-compose.debug.yml logs -f grpc-server"
fi

echo ""
echo "=========================================="
echo "セットアップ完了"
echo "=========================================="
