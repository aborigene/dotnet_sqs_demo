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
    local image_name="simpledotnetsqs-${service_name}:${VERSION}"
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
        ;;
    *)
        echo "Error: Invalid service '$SERVICE'"
        echo "Usage: $0 [producer|consumer|all] [version]"
        exit 1
        ;;
esac

echo ""
echo "To run the services locally:"
echo ""
echo "Producer:"
echo "docker run -d -p 8080:8080 \\"
echo "  -e AWS_ACCESS_KEY_ID=your-key \\"
echo "  -e AWS_SECRET_ACCESS_KEY=your-secret \\"
echo "  -e AWS_REGION=us-east-1 \\"
echo "  -e AWS__SQS__QueueUrl=your-queue-url \\"
echo "  --name sqs-producer simpledotnetsqs-producer:${VERSION}"
echo ""
echo "Consumer:"
echo "docker run -d \\"
echo "  -e AWS_ACCESS_KEY_ID=your-key \\"
echo "  -e AWS_SECRET_ACCESS_KEY=your-secret \\"
echo "  -e AWS_REGION=us-east-1 \\"
echo "  -e AWS__SQS__QueueUrl=your-queue-url \\"
echo "  --name sqs-consumer simpledotnetsqs-consumer:${VERSION}"
