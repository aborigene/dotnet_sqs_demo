#!/bin/bash

# Multi-architecture build script for Docker or Podman
# This script builds and pushes a multi-arch container image

set -e

# Function to display usage
usage() {
    echo "Usage: $0 <docker|podman> [IMAGE_NAME] [VERSION]"
    echo ""
    echo "Parameters:"
    echo "  docker|podman  - Container tool to use (required)"
    echo "  IMAGE_NAME     - Docker image name (default: igoroschsimoes/simpledotnetsqs)"
    echo "  VERSION        - Image version tag (default: v1)"
    echo ""
    echo "Examples:"
    echo "  $0 podman"
    echo "  $0 docker igoroschsimoes/simpledotnetsqs v2"
    exit 1
}

# Check if first parameter is provided
if [ -z "$1" ]; then
    echo "Error: Container tool not specified"
    echo ""
    usage
fi

CONTAINER_TOOL="$1"

# Validate container tool
if [ "$CONTAINER_TOOL" != "docker" ] && [ "$CONTAINER_TOOL" != "podman" ]; then
    echo "Error: Invalid container tool '$CONTAINER_TOOL'"
    echo "Must be either 'docker' or 'podman'"
    echo ""
    usage
fi

IMAGE_NAME="${2:-igoroschsimoes/simpledotnetsqs}"
VERSION="${3:-v1}"
PLATFORMS="linux/amd64,linux/arm64"

echo "Container tool: ${CONTAINER_TOOL}"
echo "Building multi-architecture image: ${IMAGE_NAME}:${VERSION}"
echo "Platforms: ${PLATFORMS}"
echo ""

if [ "$CONTAINER_TOOL" = "docker" ]; then
    # Docker buildx method
    echo "Using Docker buildx for multi-platform build..."
    
    # Ensure buildx is available
    if ! docker buildx version &> /dev/null; then
        echo "Error: docker buildx is not available"
        exit 1
    fi
    
    # Build and push for multiple platforms
    docker buildx build --platform ${PLATFORMS} \
      -t ${IMAGE_NAME}:${VERSION} \
      --push \
      .
    
    echo ""
    echo "Build and push completed successfully!"
    echo "Image: ${IMAGE_NAME}:${VERSION}"
    
else
    # Podman method
    echo "Using Podman for multi-platform build..."
    echo "⚠️  Warning: Building AMD64 on ARM64 Mac will be slow due to emulation"
    echo ""
    
    # Build for multiple architectures
    podman build --platform ${PLATFORMS} \
      --manifest ${IMAGE_NAME}:${VERSION} \
      .
    
    echo ""
    echo "Inspecting manifest..."
    podman manifest inspect ${IMAGE_NAME}:${VERSION}
    
    echo ""
    echo "Pushing manifest to registry..."
    podman manifest push ${IMAGE_NAME}:${VERSION} \
      docker://${IMAGE_NAME}:${VERSION}
    
    echo ""
    echo "Build and push completed successfully!"
    echo "Image: ${IMAGE_NAME}:${VERSION}"
fi
