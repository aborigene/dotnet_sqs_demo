#!/bin/bash

# Local build script for development
# This script builds Docker images locally without pushing

set -e

SERVICE="${1:-all}"
VERSION="${2:-latest}"

echo "Building service(s): ${SERVICE}"
echo "Version: ${VERSION}"
echo ""

# Function to build a service locally
build_service_local() {
    local service_name=$1
    local image_name="simpledotnetkafka-${service_name}:${VERSION}"
    local context_dir="./${service_name}"
    
    echo "================================================"
    echo "Building ${service_name} service"
    echo "Image: ${image_name}"
    echo "================================================"
    
    if [ ! -d "$context_dir" ]; then
        echo "Error: Directory ${context_dir} does not exist"
        exit 1
    fi
    
    docker build -t ${image_name} ${context_dir}
    
    echo "✅ ${service_name} built successfully!"
    echo ""
}

# Build based on service parameter
case "$SERVICE" in
    producer)
        build_service_local "producer"
        ;;
    consumer)
        build_service_local "consumer"
        ;;
    all)
        build_service_local "producer"
        build_service_local "consumer"
        echo "================================================"
        echo "✅ All services built successfully!"
        echo "================================================"
        echo ""
        echo "To run the services locally:"
        echo "  docker run -p 8080:8080 simpledotnetkafka-producer:${VERSION}"
        echo "  docker run simpledotnetkafka-consumer:${VERSION}"
        ;;
    *)
        echo "Error: Unknown service '${SERVICE}'"
        echo "Usage: $0 [producer|consumer|all] [version]"
        echo ""
        echo "Examples:"
        echo "  $0              # Build all services with 'latest' tag"
        echo "  $0 producer     # Build only producer"
        echo "  $0 consumer v2  # Build only consumer with v2 tag"
        exit 1
        ;;
esac

echo ""
echo "Available images:"
docker images | grep simpledotnetkafka
