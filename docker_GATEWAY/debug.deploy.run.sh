#!/bin/bash
set -euo pipefail

# ============================================================
# deploy_run.sh - Load saved images and start services
# ============================================================
COMPOSE_FILE=docker-compose.debug.yml
ARCHIVE=debug/debug-images.tar.gz
INPUT_DIR=debug/docker_images

# Load images
if [ -d "$INPUT_DIR" ] && compgen -G "$INPUT_DIR/*.tar" > /dev/null; then
  echo "Loading from $INPUT_DIR"
  for f in "$INPUT_DIR"/*.tar; do
    echo "  docker load $f"
    docker load -i "$f"
  done
elif [ -f "$ARCHIVE" ]; then
  echo "Extracting $ARCHIVE"
  TMPDIR=$(mktemp -d)
  tar -xzf "$ARCHIVE" -C "$TMPDIR"
  for f in "$TMPDIR"/*.tar; do
    [ -e "$f" ] || continue
    echo "  docker load $f"
    docker load -i "$f"
  done
  rm -rf "$TMPDIR"
else
  echo "No images found. Run debug.build.save.sh first."
  exit 1
fi

echo "Loaded images:"
docker images | grep -E "robocross/gateway|nginx" || true

# Start services
echo "Starting services..."
docker compose -f "$COMPOSE_FILE" up -d --remove-orphans
docker compose -f "$COMPOSE_FILE" ps