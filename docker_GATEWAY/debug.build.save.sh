#!/bin/bash
set -euo pipefail

# ============================================================
# save_images.sh - Build debug images and save to archive
# ============================================================
COMPOSE_FILE=docker-compose.debug.yml
OUT_DIR=debug/docker_images
ARCHIVE=debug/debug-images.tar.gz
mkdir -p "$OUT_DIR"

echo "[1/3] Building images"
docker compose -f "$COMPOSE_FILE" build

echo "[2/3] Saving images"
declare -A IMAGES=(
  [grpc-server.tar]=robocross/gateway-core:debug
  [blazor-client.tar]=robocross/gateway-blazor:debug
  [nginx.tar]=nginx:latest
)
for fname in "${!IMAGES[@]}"; do
  img=${IMAGES[$fname]}
  docker image inspect "$img" >/dev/null 2>&1 || docker pull "$img" || true
  if docker image inspect "$img" >/dev/null 2>&1; then
    echo "  saving $img -> $OUT_DIR/$fname"
    docker save -o "$OUT_DIR/$fname" "$img"
  else
    echo "  warning: $img not available; skipping"
  fi
done

echo "[3/3] Creating archive: $ARCHIVE"
(cd "$OUT_DIR" && tar -czf "../$(basename "$ARCHIVE")" --remove-files ./*.tar) || true
[ -d "$OUT_DIR" ] && rm -rf "$OUT_DIR"

echo "Done. Archive: $ARCHIVE"