#!/bin/bash
set -euo pipefail

# load_images.sh — load saved image tars from release/docker_images or from archive
INPUT_DIR=release/docker_images
ARCHIVE=release/release-images.tar.gz

if [ -d "$INPUT_DIR" ] && compgen -G "$INPUT_DIR/*.tar" > /dev/null; then
    echo "Loading individual tars from $INPUT_DIR"
    for f in "$INPUT_DIR"/*.tar; do
        echo " - docker load $f"
        docker load -i "$f"
    done
elif [ -f "$ARCHIVE" ]; then
    echo "Extracting archive $ARCHIVE to temporary dir and loading"
    TMPDIR=$(mktemp -d)
    tar -xzf "$ARCHIVE" -C "$TMPDIR"
    for f in "$TMPDIR"/*.tar; do
        [ -e "$f" ] || continue
        echo " - docker load $f"
        docker load -i "$f"
    done
    rm -rf "$TMPDIR"
else
    echo "No images found in $INPUT_DIR and archive $ARCHIVE"
    exit 1
fi

echo "Loaded images."
docker images | grep -E "robocross/gateway|nginx" || true

echo "To start services run:"
echo "  docker compose -f docker-compose.release.deploy.yml up -d"

# Start services using release deploy compose
COMPOSE_FILE=docker-compose.release.deploy.yml
if [ -f "$COMPOSE_FILE" ]; then
    echo "Starting services via $COMPOSE_FILE"
    docker compose -f "$COMPOSE_FILE" up -d --remove-orphans
    docker compose -f "$COMPOSE_FILE" ps
else
    echo "Warning: $COMPOSE_FILE not found — skip starting services"
fi
