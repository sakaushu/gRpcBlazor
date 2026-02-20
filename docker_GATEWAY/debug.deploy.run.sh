#!/bin/bash
set -euo pipefail
set -x

# load_images.sh — load saved image tars from debug/docker_images or from archive
INPUT_DIR=debug/docker_images
ARCHIVE=debug/debug-images.tar.gz
MANIFEST=debug/debug-images.manifest.txt

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

# optional: verify digests if manifest is present
if [ -f "$MANIFEST" ]; then
  echo "=== Checking image digests ==="
  while read -r line; do
    img=$(echo "$line" | awk '{print $1}')
    exp=$(echo "$line" | sed -n 's/.*digest=\([^ ]*\).*/\1/p')
    cur=$(docker image inspect "$img" -f '{{index .RepoDigests 0}}' || true)
    echo " - $img : expected=$exp current=$cur"
  done < "$MANIFEST"
fi

echo "To start services run:"
echo " docker compose -f docker-compose.debug.deploy.yml up -d"

# Start services using debug deploy compose
COMPOSE_FILE=docker-compose.debug.deploy.yml
if [ -f "$COMPOSE_FILE" ]; then
  echo "Starting services via $COMPOSE_FILE"
  docker compose -f "$COMPOSE_FILE" up -d --remove-orphans
  docker compose -f "$COMPOSE_FILE" ps

  echo "=== Diagnostics: host ptrace_scope ==="
  if [ -r /proc/sys/kernel/yama/ptrace_scope ]; then
    cat /proc/sys/kernel/yama/ptrace_scope || true
  fi

  echo "=== Diagnostics: container users & dotnet/vsdbg processes ==="
  for SVC in grpc-server-debug blazor-client-debug ; do
    if docker ps --format '{{.Names}}' | grep -qx "$SVC"; then
      echo "--- $SVC ---"
      docker exec "$SVC" sh -lc 'whoami; id -u; uname -a || true'
      docker exec "$SVC" sh -lc 'cat /proc/sys/kernel/yama/ptrace_scope 2>/dev/null || true'
      docker exec "$SVC" sh -lc 'ps -ef | grep -E "dotnet|vsdbg" | grep -v grep || true'
      docker exec "$SVC" sh -lc 'ls -ld /root /root/.vsdbg 2>/dev/null || true'
      docker exec "$SVC" sh -lc 'test -r /tmp/vsdbg.log && head -n 80 /tmp/vsdbg.log || echo "(no /tmp/vsdbg.log)"'
    fi
  done
else
  echo "Warning: $COMPOSE_FILE not found — skip starting services"
fi