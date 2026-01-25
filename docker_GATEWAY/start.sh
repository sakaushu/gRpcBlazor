#!/bin/bash

# ============================
# パラメータ有無チェック
# ============================

if [ $# -eq 0 ]; then
    # パラメータ無し
    docker compose up -d --build
else
    # パラメータ有り
    if [ "$1" = "debug" ]; then
        # パラメータがdebugの場合
        DEBUG=true docker compose up -d --build
    else
        # パラメータがdebugではない場合
        docker compose up -d --build
    fi
fi
