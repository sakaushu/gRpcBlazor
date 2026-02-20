#!/bin/bash

# ============================
# WSL2の場合
# BuildKit 無効化
# ============================

build_and_up() {
  # BuildKit OFF でビルド（NuGet restore の不具合回避）
  COMPOSE_DOCKER_CLI_BUILD=0 DOCKER_BUILDKIT=0 docker compose build --no-cache

  # ビルド結果を起動
  docker compose up -d
}

# ============================
# パラメータ有無チェック
# ============================

if [ $# -eq 0 ]; then
    # パラメータ無し
    build_and_up
else
    # パラメータ有り
    if [ "$1" = "debug" ]; then
        # パラメータがdebugの場合
        DEBUG=true build_and_up
    else
        # パラメータがdebugではない場合
        build_and_up
    fi
fi
