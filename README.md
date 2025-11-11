# .NET SQS Demo Application

A simple .NET 8 Web API that receives POST requests with an ID and sends them to an AWS SQS queue.

## Prerequisites

- .NET 8 SDK
- AWS Account with SQS access
- AWS credentials configured (via AWS CLI, environment variables, or IAM role)

## Configuration

Update `appsettings.json` with your SQS queue URL:

```json
{
  "AWS": {
    "SQS": {
      "QueueUrl": "https://sqs.REGION.amazonaws.com/ACCOUNT_ID/QUEUE_NAME"
    }
  }
}
```

## AWS Credentials

The application uses the AWS SDK for .NET, which looks for credentials in the following order:

1. Environment variables (`AWS_ACCESS_KEY_ID`, `AWS_SECRET_ACCESS_KEY`)
2. AWS credentials file (`~/.aws/credentials`)
3. IAM role (if running on AWS infrastructure)

## Running the Application

```bash
dotnet restore
dotnet run
```

The API will be available at `https://localhost:5001` (or the port specified in the console output).

## API Endpoint

### POST /api/message

Send an ID to the SQS queue.

**Request Body:**
```json
{
  "id": "your-unique-id"
}
```

**Success Response (200):**
```json
{
  "success": true,
  "messageId": "aws-message-id",
  "sentId": "your-unique-id"
}
```

**Error Response (400):**
```json
{
  "error": "ID is required"
}
```

**Error Response (500):**
```json
{
  "error": "Failed to send message to SQS",
  "details": "error details"
}
```

## Testing with curl

```bash
curl -X POST https://localhost:5001/api/message \
  -H "Content-Type: application/json" \
  -d '{"id": "test-123"}'
```

## Testing with Swagger

Navigate to `https://localhost:5001/swagger` in your browser to use the interactive API documentation.

## Docker Build

### Building with Docker (Multi-Architecture)

Build the Docker image for multiple architectures (AMD64 and ARM64):

```bash
# Build for multiple platforms
docker buildx build --platform linux/amd64,linux/arm64 \
  -t igoroschsimoes/simpledotnetsqs:v1 \
  --push .

# Or build locally for current platform
docker build -t igoroschsimoes/simpledotnetsqs:v1 .
```

### Building with Podman (Multi-Architecture)

Podman supports multi-architecture builds using manifest lists:

```bash
# Build for AMD64 (x86_64)
podman build --platform linux/amd64 \
  -t igoroschsimoes/simpledotnetsqs:v1-amd64 .

# Build for ARM64 (Apple Silicon compatibility)
podman build --platform linux/arm64 \
  -t igoroschsimoes/simpledotnetsqs:v1-arm64 .

# Create and push manifest list
podman manifest create igoroschsimoes/simpledotnetsqs:v1

podman manifest add igoroschsimoes/simpledotnetsqs:v1 \
  igoroschsimoes/simpledotnetsqs:v1-amd64

podman manifest add igoroschsimoes/simpledotnetsqs:v1 \
  igoroschsimoes/simpledotnetsqs:v1-arm64

# Push the manifest list (includes all architectures)
podman manifest push igoroschsimoes/simpledotnetsqs:v1 \
  docker://igoroschsimoes/simpledotnetsqs:v1
```

**Alternative: Build and push in one command with Podman**

```bash
podman build --platform linux/amd64,linux/arm64 \
  --manifest igoroschsimoes/simpledotnetsqs:v1 .

podman manifest push igoroschsimoes/simpledotnetsqs:v1 \
  docker://igoroschsimoes/simpledotnetsqs:v1
```

Run the container locally:

```bash
# Using Docker
docker run -d -p 8080:8080 \
  -e AWS_ACCESS_KEY_ID=your-access-key \
  -e AWS_SECRET_ACCESS_KEY=your-secret-key \
  -e AWS_REGION=us-east-1 \
  -e AWS__SQS__QueueUrl=your-queue-url \
  --name sqs-demo \
  igoroschsimoes/simpledotnetsqs:v1

# Using Podman
podman run -d -p 8080:8080 \
  -e AWS_ACCESS_KEY_ID=your-access-key \
  -e AWS_SECRET_ACCESS_KEY=your-secret-key \
  -e AWS_REGION=us-east-1 \
  -e AWS__SQS__QueueUrl=your-queue-url \
  --name sqs-demo \
  igoroschsimoes/simpledotnetsqs:v1
```

## Kubernetes Deployment

### Prerequisites
- Kubernetes cluster (v1.24+)
- kubectl configured
- Container registry (Docker Hub, ECR, GCR, etc.)

### Steps

1. **Build and push the multi-architecture image:**

**Using Docker:**
```bash
# Build and push for multiple platforms
docker buildx build --platform linux/amd64,linux/arm64 \
  -t igoroschsimoes/simpledotnetsqs:v1 \
  --push .
```

**Using Podman:**
```bash
# Build for multiple architectures
podman build --platform linux/amd64,linux/arm64 \
  --manifest igoroschsimoes/simpledotnetsqs:v1 .

# Push the manifest
podman manifest push igoroschsimoes/simpledotnetsqs:v1 \
  docker://igoroschsimoes/simpledotnetsqs:v1
```

2. **Update Kubernetes manifests:**

Edit `k8s-deployment.yaml`:
- Update the image reference to: `igoroschsimoes/simpledotnetsqs:v1`
- Update AWS credentials in the Secret
- Update SQS Queue URL in the ConfigMap
- Update the Ingress host (optional)

3. **Deploy to Kubernetes:**

```bash
# Apply the deployment
kubectl apply -f k8s-deployment.yaml

# Optional: Apply horizontal pod autoscaler
kubectl apply -f k8s-hpa.yaml
```

4. **Verify the deployment:**

```bash
# Check pods
kubectl get pods -n sqs-demo

# Check service
kubectl get svc -n sqs-demo

# Check logs
kubectl logs -n sqs-demo -l app=sqs-demo --tail=50
```

5. **Test the API:**

```bash
# Port forward to test locally
kubectl port-forward -n sqs-demo svc/sqs-demo-service 8080:80

# Send a test request
curl -X POST http://localhost:8080/api/message \
  -H "Content-Type: application/json" \
  -d '{"id": "test-k8s-123"}'
```

### Kubernetes Resources

The deployment includes:
- **Namespace**: Isolated namespace for the application
- **ConfigMap**: Non-sensitive configuration (Queue URL, environment)
- **Secret**: AWS credentials (consider using IRSA/Workload Identity in production)
- **Deployment**: 2 replicas with health checks and resource limits
- **Service**: ClusterIP service exposing port 80
- **Ingress**: Optional ingress for external access
- **HorizontalPodAutoscaler**: Auto-scaling based on CPU/memory (optional)

### Production Considerations

1. **AWS Authentication**: Use IAM Roles for Service Accounts (IRSA) instead of static credentials
2. **Secrets Management**: Use AWS Secrets Manager, Azure Key Vault, or HashiCorp Vault
3. **Monitoring**: Add Prometheus metrics and integrate with your monitoring stack
4. **Logging**: Configure centralized logging (ELK, Loki, CloudWatch)
5. **TLS**: Configure TLS certificates for the Ingress
6. **Network Policies**: Add Kubernetes Network Policies for security
7. **Resource Limits**: Adjust based on load testing results

## Project Structure

- `Program.cs` - Application entry point and service configuration
- `Controllers/MessageController.cs` - API endpoint for receiving POST requests
- `Services/SqsService.cs` - Service for sending messages to SQS
- `Models/MessageRequest.cs` - Request model
- `appsettings.json` - Application configuration
- `Dockerfile` - Multistage Docker build
- `k8s-deployment.yaml` - Kubernetes deployment manifests
- `k8s-hpa.yaml` - Horizontal Pod Autoscaler configuration
