# .NET SQS Producer-Consumer Demo

A complete .NET 8 microservices application demonstrating AWS SQS message queue integration with separate producer and consumer services.

## Architecture

This project consists of two independent services:

- **Producer** (`/producer`): Web API that receives HTTP POST requests and sends messages to an SQS queue
- **Consumer** (`/consumer`): Background worker service that polls and processes messages from the SQS queue

```
┌──────────────┐      ┌─────────────┐      ┌──────────────┐
│   Producer   │─────▶│  AWS SQS    │─────▶│   Consumer   │
│   (Web API)  │      │   Queue     │      │   (Worker)   │
└──────────────┘      └─────────────┘      └──────────────┘
```

## Project Structure

```
.
├── producer/                    # Producer Web API Service
│   ├── Controllers/
│   │   └── MessageController.cs
│   ├── Models/
│   │   └── MessageRequest.cs
│   ├── Services/
│   │   ├── ISqsService.cs
│   │   └── SqsService.cs
│   ├── Program.cs
│   ├── SqsDemo.csproj
│   ├── Dockerfile
│   ├── appsettings.json
│   └── appsettings.Development.json
│
├── consumer/                    # Consumer Worker Service
│   ├── Worker.cs
│   ├── Program.cs
│   ├── SqsConsumer.csproj
│   ├── Dockerfile
│   ├── appsettings.json
│   └── appsettings.Development.json
│
├── k8s-deployment.yaml         # Kubernetes manifests
├── k8s-hpa.yaml                # Horizontal Pod Autoscalers
├── build-multiarch.sh          # Multi-arch build script
└── README.md                   # This file
```

## Prerequisites

- .NET 8 SDK
- AWS Account with SQS access
- AWS credentials configured (via AWS CLI, environment variables, or IAM role)
- Docker or Podman (for containerization)
- Kubernetes cluster (for K8s deployment)

## Configuration

Both services need to be configured with your SQS queue URL. Update the respective `appsettings.json` files:

### Producer Configuration (`producer/appsettings.json`)

```json
{
  "AWS": {
    "SQS": {
      "QueueUrl": "https://sqs.REGION.amazonaws.com/ACCOUNT_ID/QUEUE_NAME"
    }
  }
}
```

### Consumer Configuration (`consumer/appsettings.json`)

```json
{
  "AWS": {
    "SQS": {
      "QueueUrl": "https://sqs.REGION.amazonaws.com/ACCOUNT_ID/QUEUE_NAME",
      "MaxNumberOfMessages": 10,
      "WaitTimeSeconds": 20
    }
  }
}
```

## AWS Credentials

Both services use the AWS SDK for .NET, which looks for credentials in the following order:

1. Environment variables (`AWS_ACCESS_KEY_ID`, `AWS_SECRET_ACCESS_KEY`)
2. AWS credentials file (`~/.aws/credentials`)
3. IAM role (if running on AWS infrastructure)

## Running the Services Locally

### Running the Producer

```bash
cd producer
dotnet restore
dotnet run
```

The API will be available at `https://localhost:5001` (or the port specified in the console output).

### Running the Consumer

```bash
cd consumer
dotnet restore
dotnet run
```

The worker will start polling the SQS queue and processing messages in the background.

## Producer API Usage

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

**Testing with curl:**

```bash
curl -X POST https://localhost:5001/api/message \
  -H "Content-Type: application/json" \
  -d '{"id": "test-123"}'
```

**Testing with Swagger:**

Navigate to `https://localhost:5001/swagger` in your browser to use the interactive API documentation.

## Consumer Functionality

The consumer service:
- Uses long polling (20 seconds by default) for efficient message retrieval
- Processes up to 10 messages per batch
- Automatically deletes messages after successful processing
- Logs all processing activities
- Handles errors gracefully with retry logic

### Message Processing

The consumer processes messages with the following structure:

```json
{
  "Id": "your-unique-id",
  "Timestamp": "2025-11-12T10:30:00Z"
}
```

You can customize the business logic in the `Worker.cs` file's `ProcessBusinessLogicAsync` method.

## Docker Build and Run

### Building Individual Services

**Producer:**
```bash
cd producer
docker build -t igoroschsimoes/simpledotnetsqs-producer:v1 .
```

**Consumer:**
```bash
cd consumer
docker build -t igoroschsimoes/simpledotnetsqs-consumer:v1 .
```

### Building Multi-Architecture Images

Use the provided build script to build both services for multiple architectures:

```bash
# Build producer
./build-multiarch.sh docker producer igoroschsimoes/simpledotnetsqs-producer v1

# Build consumer
./build-multiarch.sh docker consumer igoroschsimoes/simpledotnetsqs-consumer v1
```

### Running with Docker

**Producer:**
```bash
docker run -d -p 8080:8080 \
  -e AWS_ACCESS_KEY_ID=your-access-key \
  -e AWS_SECRET_ACCESS_KEY=your-secret-key \
  -e AWS_REGION=us-east-1 \
  -e AWS__SQS__QueueUrl=your-queue-url \
  --name sqs-producer \
  igoroschsimoes/simpledotnetsqs-producer:v1
```

**Consumer:**
```bash
docker run -d \
  -e AWS_ACCESS_KEY_ID=your-access-key \
  -e AWS_SECRET_ACCESS_KEY=your-secret-key \
  -e AWS_REGION=us-east-1 \
  -e AWS__SQS__QueueUrl=your-queue-url \
  --name sqs-consumer \
  igoroschsimoes/simpledotnetsqs-consumer:v1
```

## Kubernetes Deployment

### Prerequisites
- Kubernetes cluster (v1.24+)
- kubectl configured
- Container registry access

### Steps

1. **Build and push the images:**

```bash
# Build producer
docker buildx build --platform linux/amd64,linux/arm64 \
  -t igoroschsimoes/simpledotnetsqs-producer:v1 \
  --push ./producer

# Build consumer
docker buildx build --platform linux/amd64,linux/arm64 \
  -t igoroschsimoes/simpledotnetsqs-consumer:v1 \
  --push ./consumer
```

2. **Update Kubernetes manifests:**

Edit `k8s-deployment.yaml`:
- Update AWS credentials in the Secret
- Update SQS Queue URL in the ConfigMap
- Update image references if using a different registry
- Update the Ingress host (optional)

3. **Deploy to Kubernetes:**

```bash
# Apply the deployment (includes both producer and consumer)
kubectl apply -f k8s-deployment.yaml

# Optional: Apply horizontal pod autoscalers
kubectl apply -f k8s-hpa.yaml
```

4. **Verify the deployment:**

```bash
# Check pods
kubectl get pods -n sqs-demo

# Check services
kubectl get svc -n sqs-demo

# Check producer logs
kubectl logs -n sqs-demo -l app=sqs-producer --tail=50

# Check consumer logs
kubectl logs -n sqs-demo -l app=sqs-consumer --tail=50
```

5. **Test the system:**

```bash
# Port forward to access the producer API
kubectl port-forward -n sqs-demo svc/sqs-producer-service 8080:80

# Send a test message
curl -X POST http://localhost:8080/api/message \
  -H "Content-Type: application/json" \
  -d '{"id": "test-k8s-123"}'

# Watch consumer logs to see message processing
kubectl logs -n sqs-demo -l app=sqs-consumer --follow
```

## Kubernetes Resources

The deployment includes:

### For Both Services:
- **Namespace**: `sqs-demo` - Isolated namespace
- **ConfigMap**: Shared configuration (Queue URL, environment)
- **Secret**: Shared AWS credentials

### Producer:
- **Deployment**: 2 replicas with health checks and resource limits
- **Service**: ClusterIP service exposing port 80
- **Ingress**: External access to the API
- **HPA**: Auto-scaling based on CPU/memory (2-10 replicas)

### Consumer:
- **Deployment**: 2 replicas with resource limits
- **HPA**: Auto-scaling based on CPU/memory (2-10 replicas)
- No Service needed (background worker)

## Scaling Considerations

### Producer Scaling
- Scales based on API request load
- Each replica can handle concurrent requests
- Consider increasing replicas during peak traffic

### Consumer Scaling
- Scales based on message queue depth
- Each replica processes messages independently
- SQS handles message distribution automatically
- Consider using KEDA for queue-based autoscaling

### SQS Configuration Tips
- Enable Dead Letter Queue (DLQ) for failed messages
- Set appropriate visibility timeout (default: 30s)
- Use FIFO queues if message ordering is critical
- Monitor queue metrics in CloudWatch

## Monitoring and Observability

### Logs
Both services log important events:
- Producer: API requests, SQS send operations, errors
- Consumer: Message polling, processing, deletion, errors

### Metrics to Monitor
- Producer: Request rate, response times, SQS send errors
- Consumer: Messages processed, processing time, errors
- SQS: Queue depth, message age, DLQ messages

### Health Checks
- Producer: `/health` endpoint for liveness/readiness probes
- Consumer: Inherent health through continuous operation

## Production Considerations

1. **AWS Authentication**: Use IAM Roles for Service Accounts (IRSA) instead of static credentials
2. **Secrets Management**: Use AWS Secrets Manager, Azure Key Vault, or HashiCorp Vault
3. **Dead Letter Queue**: Configure DLQ for failed message handling
4. **Message Retention**: Configure appropriate retention period in SQS
5. **Monitoring**: Integrate with Prometheus, CloudWatch, or Dynatrace
6. **Logging**: Configure centralized logging (ELK, Loki, CloudWatch)
7. **TLS**: Configure TLS certificates for Ingress
8. **Network Policies**: Add Kubernetes Network Policies for security
9. **Resource Limits**: Adjust based on load testing results
10. **Idempotency**: Ensure consumer logic is idempotent for message reprocessing

## Testing the Complete Flow

1. Start both services (locally or in Kubernetes)
2. Send a message via the producer API:
   ```bash
   curl -X POST http://localhost:8080/api/message \
     -H "Content-Type: application/json" \
     -d '{"id": "test-message-123"}'
   ```
3. Check producer logs to confirm message was sent
4. Check consumer logs to see message processing:
   ```bash
   # If running in Kubernetes
   kubectl logs -n sqs-demo -l app=sqs-consumer --follow
   
   # If running locally
   # Check the terminal where consumer is running
   ```
5. Verify message was deleted from SQS queue

## Troubleshooting

### Producer Issues
- Check AWS credentials are valid
- Verify SQS queue URL is correct
- Check network connectivity to AWS
- Review API logs for errors

### Consumer Issues
- Check AWS credentials are valid
- Verify queue permissions (receive, delete)
- Check long polling configuration
- Review worker logs for processing errors
- Verify queue is not empty

### Common Errors
- **403 Forbidden**: Check IAM permissions
- **404 Queue Not Found**: Verify queue URL
- **Connection Timeout**: Check network/security groups
- **Messages Not Processing**: Check visibility timeout and DLQ

## Development Tips

### Adding Business Logic to Consumer
Edit `consumer/Worker.cs` and implement your logic in `ProcessBusinessLogicAsync`:

```csharp
private async Task ProcessBusinessLogicAsync(MessageData messageData)
{
    // Your custom logic here
    // Examples:
    // - Save to database
    // - Call external API
    // - Send notification
    // - Transform and forward
}
```

### Customizing Message Format
Update the `MessageData` class in both producer and consumer to match your needs:

```csharp
public class MessageData
{
    public string Id { get; set; }
    public DateTime Timestamp { get; set; }
    // Add your custom properties
}
```

## License

MIT License - Feel free to use this project for learning and production purposes.

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

### Using the Build Script (Recommended)

The repository includes a script that works with both Docker and Podman:

```bash
# Using Podman
./build-multiarch.sh podman

# Using Docker
./build-multiarch.sh docker

# Custom image name and version
./build-multiarch.sh podman myregistry/myapp v2.0
./build-multiarch.sh docker myregistry/myapp v2.0
```

**Note:** Building AMD64 on ARM64 Mac with Podman will be slow due to emulation. Consider using Docker buildx or GitHub Actions for faster multi-arch builds.

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
