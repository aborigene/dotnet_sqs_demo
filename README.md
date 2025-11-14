# .NET Messaging Demos

A collection of .NET 8 microservices demonstrating different messaging patterns and technologies with separate producer and consumer services.

## Overview

This repository contains complete, production-ready examples of message-based communication in .NET applications. Each demo follows the same architectural pattern with separate producer (Web API) and consumer (Worker Service) implementations, making it easy to compare different messaging technologies.

## Available Demos

### 1. AWS SQS Demo (`dotnet_sqs_demo/`)

Demonstrates AWS Simple Queue Service (SQS) integration with .NET 8.

**Key Features:**
- Queue-based messaging (point-to-point)
- Long polling for efficient message retrieval
- Visibility timeout and message deletion
- AWS SDK integration
- Configurable batch processing

**Use Cases:**
- Task queues and job processing
- Decoupling microservices in AWS
- Reliable message delivery with retries
- Managed cloud messaging

[📖 Full Documentation](./dotnet_sqs_demo/README.md)

### 2. Apache Kafka Demo (`dotnet_kafka_demo/`)

Demonstrates Apache Kafka streaming platform integration with .NET 8.

**Key Features:**
- Topic-based pub/sub messaging
- High-throughput event streaming
- Consumer groups and partition management
- Offset management with batch commits
- Self-hosted Kafka broker (KRaft mode)

**Use Cases:**
- Event streaming and processing
- Real-time data pipelines
- Event sourcing architectures
- Log aggregation
- Multi-subscriber scenarios

[📖 Full Documentation](./dotnet_kafka_demo/README.md)

## Architecture

Both demos follow the same microservices pattern:

```
┌──────────────┐      ┌─────────────┐      ┌──────────────┐
│   Producer   │─────▶│   Message   │─────▶│   Consumer   │
│   (Web API)  │      │   Broker    │      │   (Worker)   │
└──────────────┘      └─────────────┘      └──────────────┘
```

### Producer (Web API)
- REST API endpoint: `POST /api/message`
- Accepts JSON payload with message data
- Publishes messages to the broker
- Health check endpoint for monitoring
- Swagger/OpenAPI documentation

### Consumer (Worker Service)
- Background service running continuously
- Polls/subscribes to messages from broker
- Processes messages with business logic
- Graceful shutdown handling
- Configurable batch processing

## Project Structure

```
.
├── dotnet_sqs_demo/              # AWS SQS implementation
│   ├── producer/                 # SQS Producer API
│   ├── consumer/                 # SQS Consumer Worker
│   ├── k8s-deployment.yaml       # Kubernetes manifests
│   ├── build-local.sh            # Local build script
│   └── build-multiarch.sh        # Multi-arch build script
│
├── dotnet_kafka_demo/            # Apache Kafka implementation
│   ├── producer/                 # Kafka Producer API
│   ├── consumer/                 # Kafka Consumer Worker
│   ├── k8s-deployment.yaml       # Kubernetes manifests (includes Kafka)
│   ├── build-local.sh            # Local build script
│   └── build-multiarch.sh        # Multi-arch build script
│
├── COMPARISON.md                 # Detailed comparison of implementations
└── README.md                     # This file
```

## Quick Start

### SQS Demo
```bash
cd dotnet_sqs_demo
# See dotnet_sqs_demo/QUICKSTART.md for detailed instructions
```

### Kafka Demo
```bash
cd dotnet_kafka_demo
# See dotnet_kafka_demo/QUICKSTART.md for detailed instructions
```

## Technology Stack

- **.NET 8**: Latest LTS version
- **Docker**: Container runtime
- **Kubernetes**: Container orchestration
- **AWS SDK**: For SQS integration
- **Confluent.Kafka**: For Kafka integration

## Common Features

All demos include:

✅ **Separate Producer & Consumer services**
- Independent deployment and scaling
- Clear separation of concerns

✅ **Production-Ready**
- Health checks and readiness probes
- Structured logging
- Error handling and retry logic
- Resource limits and requests

✅ **Container Support**
- Multi-stage Dockerfiles
- Multi-architecture builds (amd64/arm64)
- Optimized image sizes

✅ **Kubernetes Ready**
- Complete K8s manifests
- ConfigMaps and Secrets
- Services and Ingress
- Horizontal Pod Autoscaling (HPA)

✅ **Build Automation**
- Local development build scripts
- Multi-arch build support
- Docker and Podman compatibility

✅ **Comprehensive Documentation**
- README with full setup instructions
- Quick start guides
- Configuration examples
- Troubleshooting tips

## Comparison

| Feature | SQS | Kafka |
|---------|-----|-------|
| **Type** | Queue | Stream |
| **Hosting** | AWS Managed | Self-hosted |
| **Throughput** | Moderate | Very High |
| **Latency** | ~10-20ms | ~1-5ms |
| **Message Order** | FIFO queues | Partition-level |
| **Retention** | Up to 14 days | Configurable |
| **Replay** | Limited | Full replay |
| **Scaling** | Automatic | Manual/HPA |
| **Cost Model** | Pay per request | Infrastructure |

See [COMPARISON.md](./COMPARISON.md) for detailed comparison.

## When to Use What?

### Choose SQS when:
- 🔹 Running in AWS ecosystem
- 🔹 Need managed service (zero ops)
- 🔹 Queue-based messaging sufficient
- 🔹 Moderate throughput requirements
- 🔹 Want built-in AWS integration

### Choose Kafka when:
- 🔹 Need high throughput streaming
- 🔹 Require message replay capability
- 🔹 Event sourcing architecture
- 🔹 Real-time data pipelines
- 🔹 Multi-subscriber scenarios
- 🔹 Self-hosted or multi-cloud

## Prerequisites

- .NET 8 SDK
- Docker or Podman
- Kubernetes cluster (optional, for K8s deployment)
- AWS Account (for SQS demo)
- kubectl (for K8s deployments)

## Development

### Local Development
```bash
# Navigate to specific demo
cd dotnet_sqs_demo  # or dotnet_kafka_demo

# Run producer
cd producer
dotnet run

# Run consumer (in another terminal)
cd consumer
dotnet run
```

### Building Container Images
```bash
# Local build
./build-local.sh all

# Multi-architecture build and push
./build-multiarch.sh docker all your-registry/app-name v1
```

### Kubernetes Deployment
```bash
# Deploy
kubectl apply -f k8s-deployment.yaml
kubectl apply -f k8s-hpa.yaml

# Monitor
kubectl get pods -n <namespace> -w
kubectl logs -f -l app=<app-name>
```

## API Usage

All producers expose the same API interface:

### Send Message
```bash
curl -X POST http://localhost:8080/api/message \
  -H "Content-Type: application/json" \
  -d '{"id": "test-message-123"}'
```

**Response:**
```json
{
  "success": true,
  "messageId": "...",
  "sentId": "test-message-123"
}
```

### Health Check
```bash
curl http://localhost:8080/health
```

**Response:**
```json
{
  "status": "healthy",
  "timestamp": "2024-11-14T10:30:00Z"
}
```

## Message Format

All demos use the same message structure:

```json
{
  "Id": "user-provided-id",
  "Timestamp": "2024-11-14T10:30:00Z"
}
```

## Contributing

Feel free to add more messaging technology examples following the same pattern:
1. Create a new folder: `dotnet_<technology>_demo/`
2. Implement producer and consumer services
3. Add Dockerfiles and K8s manifests
4. Include build scripts
5. Write comprehensive documentation

## Future Demos (Planned)

- 🔜 RabbitMQ
- 🔜 Azure Service Bus
- 🔜 Redis Pub/Sub
- 🔜 NATS
- 🔜 Google Cloud Pub/Sub

## Resources

- [.NET Documentation](https://learn.microsoft.com/en-us/dotnet/)
- [AWS SQS Documentation](https://docs.aws.amazon.com/sqs/)
- [Apache Kafka Documentation](https://kafka.apache.org/documentation/)
- [Kubernetes Documentation](https://kubernetes.io/docs/)

## License

MIT

## Author

Igor Simoes
- GitHub: [@aborigene](https://github.com/aborigene)

---

**Note:** These demos are designed for learning and development purposes. For production use, ensure proper security configurations, monitoring, and error handling are in place.
