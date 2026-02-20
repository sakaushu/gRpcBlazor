#!/bin/bash
set -euo pipefail

# save_images.sh — build and save debug images to debug/docker_images
COMPOSE_BUILD_FILE=docker-compose.debug.build.yml
OUT_DIR=debug/docker_images
ARCHIVE=debug/debug-images.tar.gz
MANIFEST=debug/debug-images.manifest.txt

rm -f "$MANIFEST"
mkdir -p "$OUT_DIR"

echo "[1/2] Building images ($COMPOSE_BUILD_FILE)"
docker compose -f "$COMPOSE_BUILD_FILE" build

# fixed image mapping: filename -> image
declare -A IMAP
IMAP[grpc-server.tar]=robocross/gateway-core:debug
IMAP[blazor-client.tar]=robocross/gateway-blazor:debug
IMAP[nginx.tar]=nginx:latest

echo "[2/2] Saving images to $OUT_DIR"
for fname in "${!IMAP[@]}"; do
  img=${IMAP[$fname]}
  outpath="$OUT_DIR/$fname"
  if ! docker image inspect "$img" >/dev/null 2>&1; then
    echo "Image $img not found locally — attempting pull"
    docker pull "$img" || true
  fi
  if docker image inspect "$img" >/dev/null 2>&1; then
    echo " - saving $img -> $outpath"
    docker save -o "$outpath" "$img"
    # record content digest & image id
    dig=$(docker image inspect "$img" -f '{{index .RepoDigests 0}}' || true)
    id=$(docker image inspect "$img" -f '{{.Id}}' || true)
    echo "$img  digest=$dig  id=$id  saved=$(basename "$outpath")" >> "$MANIFEST"
  else
    echo " - warning: $img not available; skipping"
  fi
done

echo "Creating combined archive: $ARCHIVE"
# create archive from files inside OUT_DIR so glob expands there
( cd "$OUT_DIR" && tar -czf "../$(basename "$ARCHIVE")" --remove-files ./*.tar ) || true
echo "Manifest written: $MANIFEST"
echo "Saved files in $OUT_DIR and archive $ARCHIVE"

# remove the temporary docker_images directory if empty (or force remove if still contains unexpected files)
if [ -d "$OUT_DIR" ]; then
  if [ "$(ls -A "$OUT_DIR")" ]; then
    echo "Note: $OUT_DIR not empty after archiving — removing contents and directory"
    rm -rf "$OUT_DIR"
  else
    rmdir "$OUT_DIR" || true
  fi
fi
echo "Done."