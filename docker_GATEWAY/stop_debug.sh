#!/bin/bash
# デバッグ用Docker環境を停止
docker-compose -f docker-compose.debug.yml down
echo "Debug containers stopped."
