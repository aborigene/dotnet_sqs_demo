#!/bin/bash

# Multi-architecture build script for Docker or Podman
# This script builds and pushes multi-arch container images for producer and/or consumer services

set -e

# Function to display usage
usage() {
    echo "Usage: $0 <docker|podman> [SERVICE] [IMAGE_NAME] [VERSION]"
    echo ""
    echo "Parameters:"
    echo "  docker|podman  - Container tool to use (required)"
    echo "  SERVICE        - Service to build: 'producer', 'consumer', or 'all' (default: all)"
    echo "  IMAGE_NAME     - Docker image name base (default: igoroschsimoes/simpledotnetkafka)"
    echo "  VERSION        - Image version tag (default: v1)"
    echo ""
    echo "Examples:"
    echo "  $0 docker                                    # Build all services"
    echo "  $0 docker producer                           # Build only producer"
    echo "  $0 docker consumer                           # Build only consumer"
    echo "  $0 podman all myregistry/myapp v2            # Build all with custom settings"
    echo "  $0 docker producer myregistry/producer v1.5  # Build producer only"
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

SERVICE="${2:-all}"
IMAGE_NAME_BASE="${3:-igoroschsimoes/simpledotnetkafka}"
VERSION="${4:-v1}"
PLATFORMS="linux/amd64,linux/arm64"

# Validate service parameter
if [ "$SERVICE" != "producer" ] && [ "$SERVICE" != "consumer" ] && [ "$SERVICE" != "all" ]; then
    echo "Error: Invalid service '${SERVICE}'"
    echo "Must be 'producer', 'consumer', or 'all'"
    echo ""
    usage
fi

echo "=========================================="
echo "Multi-Architecture Build Configuration"
echo "=========================================="
echo "Container Tool: ${CONTAINER_TOOL}"
echo "Service(s):     ${SERVICE}"
echo "Image Base:     ${IMAGE_NAME_BASE}"
echo "Version:        ${VERSION}"
echo "Platforms:      ${PLATFORMS}"
echo "=========================================="
echo ""

# Function to check if buildx/buildah is available
check_build_tools() {
    if [ "$CONTAINER_TOOL" = "docker" ]; then
        if ! docker buildx version &> /dev/null; then
            echo "Error: Docker buildx is not available"
            echo "Please enable Docker buildx or install it"
            exit 1
        fi
        
        # Create and use a new builder instance if it doesn't exist
        if ! docker buildx inspect multiarch-builder &> /dev/null; then
            echo "Creating new buildx builder instance..."
            docker buildx create --name multiarch-builder --use
        else
            docker buildx use multiarch-builder
        fi
    elif [ "$CONTAINER_TOOL" = "podman" ]; then
        if ! podman manifest --version &> /dev/null; then
            echo "Error: Podman manifest command is not available"
            exit 1
        fi
    fi
}

# Function to build and push multi-arch image using Docker
build_docker_multiarch() {
    local service_name=$1
    local image_name="${IMAGE_NAME_BASE}-${service_name}:${VERSION}"
    local context_dir="./${service_name}"
    
    echo "================================================"
    echo "Building ${service_name} service with Docker"
    echo "Image: ${image_name}"
    echo "================================================"
    
    if [ ! -d "$context_dir" ]; then
        echo "Error: Directory ${context_dir} does not exist"
        exit 1
    fi
    
    docker buildx build \
        --platform ${PLATFORMS} \
        --tag ${image_name} \
        --push \
        ${context_dir}
    
    echo "✅ ${service_name} built and pushed successfully!"
    echo ""
}

# Function to build and push multi-arch image using Podman
build_podman_multiarch() {
    local service_name=$1
    local image_name="${IMAGE_NAME_BASE}-${service_name}:${VERSION}"
    local context_dir="./${service_name}"
    local manifest_name="${IMAGE_NAME_BASE}-${service_name}-manifest:${VERSION}"
    
    echo "================================================"
    echo "Building ${service_name} service with Podman"
    echo "Image: ${image_name}"
    echo "================================================"
    
    if [ ! -d "$context_dir" ]; then
        echo "Error: Directory ${context_dir} does not exist"
        exit 1
    fi
    
    # Create manifest
    podman manifest create ${manifest_name} || podman manifest rm ${manifest_name} && podman manifest create ${manifest_name}
    
    # Build for amd64
    echo "Building for linux/amd64..."
    podman build --platform linux/amd64 --tag ${image_name}-amd64 ${context_dir}
    podman manifest add ${manifest_name} ${image_name}-amd64
    
    # Build for arm64
    echo "Building for linux/arm64..."
    podman build --platform linux/arm64 --tag ${image_name}-arm64 ${context_dir}
    podman manifest add ${manifest_name} ${image_name}-arm64
    
    # Push manifest
    echo "Pushing manifest..."
    podman manifest push ${manifest_name} docker://${image_name}
    
    echo "✅ ${service_name} built and pushed successfully!"
    echo ""
}

# Check build tools
check_build_tools

# Build based on service parameter
if [ "$SERVICE" = "all" ]; then
    for svc in producer consumer; do
        if [ "$CONTAINER_TOOL" = "docker" ]; then
            build_docker_multiarch "$svc"
        else
            build_podman_multiarch "$svc"
        fi
    done
    echo "================================================"
    echo "✅ All services built and pushed successfully!"
    echo "================================================"
else
    if [ "$CONTAINER_TOOL" = "docker" ]; then
        build_docker_multiarch "$SERVICE"
    else
        build_podman_multiarch "$SERVICE"
    fi
fi

echo ""
echo "Images pushed:"
echo "  ${IMAGE_NAME_BASE}-producer:${VERSION}"
echo "  ${IMAGE_NAME_BASE}-consumer:${VERSION}"
