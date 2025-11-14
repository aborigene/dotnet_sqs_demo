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
    echo "  IMAGE_NAME     - Docker image name base (default: igoroschsimoes/simpledotnetsqs)"
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
IMAGE_NAME_BASE="${3:-igoroschsimoes/simpledotnetsqs}"
VERSION="${4:-v1}"
PLATFORMS="linux/amd64,linux/arm64"

# Validate service parameter
if [ "$SERVICE" != "producer" ] && [ "$SERVICE" != "consumer" ] && [ "$SERVICE" != "all" ]; then
    echo "Error: Invalid service '$SERVICE'"
    echo "Must be either 'producer', 'consumer', or 'all'"
    echo ""
    usage
fi

echo "Container tool: ${CONTAINER_TOOL}"
echo "Building service(s): ${SERVICE}"
echo "Platforms: ${PLATFORMS}"
echo ""

# Function to build a service
build_service() {
    local service_name=$1
    local image_name="${IMAGE_NAME_BASE}-${service_name}:${VERSION}"
    local context_dir="./${service_name}"
    
    echo "================================================"
    echo "Building ${service_name} service"
    echo "Image: ${image_name}"
    echo "Context: ${context_dir}"
    echo "================================================"
    echo ""
    
    if [ ! -d "$context_dir" ]; then
        echo "Error: Directory ${context_dir} does not exist"
        exit 1
    fi
    
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
          -t ${image_name} \
          --push \
          ${context_dir}
        
        echo ""
        echo "✅ ${service_name} build and push completed successfully!"
        echo "   Image: ${image_name}"
        echo ""
        
    else
        # Podman method
        echo "Using Podman for multi-platform build..."
        echo "⚠️  Warning: Building AMD64 on ARM64 Mac will be slow due to emulation"
        echo ""
        
        # Build for multiple architectures
        podman build --platform ${PLATFORMS} \
          --manifest ${image_name} \
          ${context_dir}
        
        echo ""
        echo "Inspecting manifest..."
        podman manifest inspect ${image_name}
        
        echo ""
        echo "Pushing manifest to registry..."
        podman manifest push ${image_name} \
          docker://${image_name}
        
        echo ""
        echo "✅ ${service_name} build and push completed successfully!"
        echo "   Image: ${image_name}"
        echo ""
    fi
}

# Build based on service parameter
if [ "$SERVICE" = "all" ]; then
    echo "Building all services..."
    echo ""
    build_service "producer"
    build_service "consumer"
    echo "================================================"
    echo "✅ All services built successfully!"
    echo "================================================"
elif [ "$SERVICE" = "producer" ]; then
    build_service "producer"
elif [ "$SERVICE" = "consumer" ]; then
    build_service "consumer"
fi

echo ""
echo "Build completed at: $(date)"
