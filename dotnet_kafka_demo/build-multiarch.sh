#!/bin/bash

# Build script for multi-architecture Docker images
# Supports amd64 and arm64 platforms

IMAGE_NAME="kafka-demo"
IMAGE_TAG="latest"
REGISTRY="your-registry" # Change this to your Docker registry

echo "Building multi-architecture Docker image..."
echo "Image: $REGISTRY/$IMAGE_NAME:$IMAGE_TAG"

# Build and push for multiple platforms
docker buildx build \
    --platform linux/amd64,linux/arm64 \
    -t $REGISTRY/$IMAGE_NAME:$IMAGE_TAG \
    --push \
    .

echo "Build complete!"
