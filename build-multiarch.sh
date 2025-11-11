#!/bin/bash

# Multi-architecture build script for Podman
# This script builds and pushes a multi-arch container image

set -e

IMAGE_NAME="${1:-igoroschsimoes/simpledotnetsqs}"
VERSION="${2:-v1}"
PLATFORMS="linux/amd64,linux/arm64"

echo "Building multi-architecture image: ${IMAGE_NAME}:${VERSION}"
echo "Platforms: ${PLATFORMS}"

# Method 1: Build all architectures at once (requires podman 4.0+)
echo "Building for multiple platforms..."
podman build --platform ${PLATFORMS} \
  --manifest ${IMAGE_NAME}:${VERSION} \
  .

echo "Inspecting manifest..."
podman manifest inspect ${IMAGE_NAME}:${VERSION}

echo "Pushing manifest to registry..."
podman manifest push ${IMAGE_NAME}:${VERSION} \
  docker://${IMAGE_NAME}:${VERSION}

echo "Build and push completed successfully!"
echo "Image: ${IMAGE_NAME}:${VERSION}"
