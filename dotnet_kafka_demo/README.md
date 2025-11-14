# .NET Kafka Producer-Consumer Demo

A complete .NET 8 microservices application demonstrating Apache Kafka message streaming with separate producer and consumer services.

## Architecture

This project consists of two independent services and a Kafka broker:

- **Producer** (`/producer`): Web API that receives HTTP POST requests and sends messages to a Kafka topic
- **Consumer** (`/consumer`): Background worker service that continuously consumes and processes messages from the Kafka topic
- **Kafka**: Apache Kafka broker running in KRaft mode (no ZooKeeper required)

```
┌──────────────┐      ┌─────────────┐      ┌──────────────┐
│   Producer   │─────▶│    Kafka    │─────▶│   Consumer   │
│   (Web API)  │      │   Broker    │      │   (Worker)   │
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
│   │   ├── IKafkaService.cs
│   │   └── KafkaService.cs
│   ├── Program.cs
│   ├── KafkaDemo.csproj
│   ├── Dockerfile
│   ├── appsettings.json
│   └── appsettings.Development.json
│
├── consumer/                    # Consumer Worker Service
│   ├── Worker.cs
│   ├── Program.cs
│   ├── KafkaConsumer.csproj
│   ├── Dockerfile
│   ├── appsettings.json
│   └── appsettings.Development.json
│
├── k8s-deployment.yaml         # Kubernetes manifests (includes Kafka)
├── k8s-hpa.yaml                # Horizontal Pod Autoscalers
├── build-local.sh              # Local build script
├── build-multiarch.sh          # Multi-arch build script
└── README.md                   # This file
```

## Features

### Producer Service
- REST API with Swagger/OpenAPI documentation
- Health check endpoint for Kubernetes
- Publishes messages to Kafka topics
- Configurable Kafka bootstrap servers and topic

### Consumer Service
- Background worker that continuously polls Kafka
- Automatic offset management with batch commits
- Graceful shutdown handling
- Configurable consumer group and batch size

### Kafka Broker
- KRaft mode (no ZooKeeper dependency)
- Single-node setup for demos/testing
- Persistent storage with StatefulSet
- Ready for Kubernetes deployment

## Prerequisites

- .NET 8 SDK
- Docker or Podman
- Kubernetes cluster (for K8s deployment)
- Apache Kafka (if running locally without K8s)

## Configuration

### Producer (`producer/appsettings.json`)
```json
{
  "Kafka": {
    "BootstrapServers": "kafka:9092",
    "Topic": "demo-topic"
  }
}
```

### Consumer (`consumer/appsettings.json`)
```json
{
  "Kafka": {
    "BootstrapServers": "kafka:9092",
    "Topic": "demo-topic",
    "GroupId": "kafka-demo-consumer-group",
    "CommitBatchSize": 10
  }
}
```

## Quick Start

### Option 1: Kubernetes Deployment (Recommended)

This deploys everything including Kafka:

```bash
# Deploy everything (Kafka + Producer + Consumer)
kubectl apply -f k8s-deployment.yaml

# Optional: Deploy HPA for auto-scaling
kubectl apply -f k8s-hpa.yaml

# Check deployment status
kubectl get all -n kafka-demo

# Check logs
kubectl logs -n kafka-demo -l app=kafka-producer
kubectl logs -n kafka-demo -l app=kafka-consumer
kubectl logs -n kafka-demo kafka-0
```

### Option 2: Local Development with Docker Compose

Create `docker-compose.yml`:

```yaml
version: '3'
services:
  kafka:
    image: confluentinc/cp-kafka:7.5.0
    ports:
      - "9092:9092"
    environment:
      KAFKA_NODE_ID: 1
      KAFKA_PROCESS_ROLES: broker,controller
      KAFKA_LISTENERS: PLAINTEXT://0.0.0.0:9092,CONTROLLER://0.0.0.0:9093
      KAFKA_ADVERTISED_LISTENERS: PLAINTEXT://localhost:9092
      KAFKA_CONTROLLER_LISTENER_NAMES: CONTROLLER
      KAFKA_LISTENER_SECURITY_PROTOCOL_MAP: CONTROLLER:PLAINTEXT,PLAINTEXT:PLAINTEXT
      KAFKA_CONTROLLER_QUORUM_VOTERS: 1@localhost:9093
      KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR: 1
      KAFKA_TRANSACTION_STATE_LOG_REPLICATION_FACTOR: 1
      KAFKA_TRANSACTION_STATE_LOG_MIN_ISR: 1
      CLUSTER_ID: MkU3OEVBNTcwNTJENDM2Qk

  producer:
    build: ./producer
    ports:
      - "8080:8080"
    depends_on:
      - kafka
    environment:
      Kafka__BootstrapServers: kafka:9092
      Kafka__Topic: demo-topic

  consumer:
    build: ./consumer
    depends_on:
      - kafka
    environment:
      Kafka__BootstrapServers: kafka:9092
      Kafka__Topic: demo-topic
      Kafka__GroupId: kafka-demo-consumer-group
```

Then run:

```bash
docker-compose up
```

### Option 3: Local .NET Development

```bash
# Terminal 1: Start Kafka
docker run -p 9092:9092 \
  -e KAFKA_NODE_ID=1 \
  -e KAFKA_PROCESS_ROLES=broker,controller \
  -e KAFKA_LISTENERS=PLAINTEXT://0.0.0.0:9092,CONTROLLER://0.0.0.0:9093 \
  -e KAFKA_ADVERTISED_LISTENERS=PLAINTEXT://localhost:9092 \
  -e KAFKA_CONTROLLER_LISTENER_NAMES=CONTROLLER \
  -e KAFKA_LISTENER_SECURITY_PROTOCOL_MAP=CONTROLLER:PLAINTEXT,PLAINTEXT:PLAINTEXT \
  -e KAFKA_CONTROLLER_QUORUM_VOTERS=1@localhost:9093 \
  -e KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR=1 \
  -e CLUSTER_ID=MkU3OEVBNTcwNTJENDM2Qk \
  confluentinc/cp-kafka:7.5.0

# Terminal 2: Run Producer
cd producer
dotnet run

# Terminal 3: Run Consumer
cd consumer
dotnet run
```

## Building Container Images

### Local Build

```bash
# Build all services
./build-local.sh

# Build specific service
./build-local.sh producer
./build-local.sh consumer
```

### Multi-Architecture Build

```bash
# Using Docker
./build-multiarch.sh docker all igoroschsimoes/simpledotnetkafka v1

# Using Podman
./build-multiarch.sh podman all myregistry/kafka-demo v1

# Build only producer
./build-multiarch.sh docker producer igoroschsimoes/simpledotnetkafka v1
```

## API Usage

### Send a Message

```bash
curl -X POST http://localhost:8080/api/message \
  -H "Content-Type: application/json" \
  -d '{"id": "test-message-123"}'
```

Response:
```json
{
  "success": true,
  "messageId": "demo-topic [[0]] @42",
  "sentId": "test-message-123"
}
```

### Health Check

```bash
curl http://localhost:8080/health
```

Response:
```json
{
  "status": "healthy",
  "timestamp": "2024-11-14T10:30:00Z"
}
```

## Kubernetes Operations

### Accessing the Producer API

```bash
# Get the service IP
kubectl get svc kafka-producer-service -n kafka-demo

# Port forward to access locally
kubectl port-forward svc/kafka-producer-service 8080:80 -n kafka-demo

# Send test message
curl -X POST http://localhost:8080/api/message \
  -H "Content-Type: application/json" \
  -d '{"id": "k8s-test-message"}'
```

### Monitoring

```bash
# Watch producer logs
kubectl logs -n kafka-demo -l app=kafka-producer -f

# Watch consumer logs
kubectl logs -n kafka-demo -l app=kafka-consumer -f

# Check Kafka logs
kubectl logs -n kafka-demo kafka-0 -f

# Check Kafka topics
kubectl exec -it kafka-0 -n kafka-demo -- \
  kafka-topics --bootstrap-server localhost:9092 --list

# Describe a topic
kubectl exec -it kafka-0 -n kafka-demo -- \
  kafka-topics --bootstrap-server localhost:9092 --describe --topic demo-topic
```

### Scaling

```bash
# Manual scaling
kubectl scale deployment kafka-producer -n kafka-demo --replicas=5
kubectl scale deployment kafka-consumer -n kafka-demo --replicas=3

# With HPA enabled, auto-scaling happens automatically based on CPU/memory
kubectl get hpa -n kafka-demo
```

### Cleanup

```bash
kubectl delete namespace kafka-demo
```

## Message Flow

1. **Client Request**: HTTP POST to `/api/message` with JSON body
2. **Producer**: Validates and sends message to Kafka topic
3. **Kafka**: Stores message in topic partition
4. **Consumer**: Polls and retrieves messages from topic
5. **Processing**: Consumer processes message with business logic
6. **Commit**: Consumer commits offset after successful processing

## Monitoring and Logging

All services provide structured logging:

**Producer logs:**
```
info: KafkaDemo.Services.KafkaService[0]
      Sending message to Kafka topic: demo-topic
```

**Consumer logs:**
```
info: KafkaConsumer.Worker[0]
      Processing message: Topic=demo-topic, Partition=0, Offset=42
info: KafkaConsumer.Worker[0]
      Parsed Message - ID: test-123, Timestamp: 2024-11-14T10:30:00Z
info: KafkaConsumer.Worker[0]
      Business logic processed for ID: test-123
```

## Configuration Options

### Producer Environment Variables
- `ASPNETCORE_URLS`: HTTP binding address (default: `http://+:8080`)
- `Kafka__BootstrapServers`: Kafka broker address
- `Kafka__Topic`: Target topic name

### Consumer Environment Variables
- `Kafka__BootstrapServers`: Kafka broker address
- `Kafka__Topic`: Topic to consume from
- `Kafka__GroupId`: Consumer group ID
- `Kafka__CommitBatchSize`: Number of messages before committing offset (default: 10)

## Production Considerations

### Kafka
- Use multiple brokers for high availability
- Configure appropriate replication factors
- Set up proper retention policies
- Enable monitoring (JMX, Prometheus)
- Consider using managed Kafka services (MSK, Confluent Cloud)

### Producer
- Implement retry logic with exponential backoff
- Add circuit breakers for fault tolerance
- Configure appropriate timeouts
- Use compression for large messages

### Consumer
- Tune batch size and commit frequency
- Implement dead letter queue for failed messages
- Add metrics and monitoring
- Configure appropriate concurrency

## Troubleshooting

### Kafka Connection Issues
```bash
# Check Kafka pod
kubectl get pods -n kafka-demo kafka-0

# Check Kafka logs
kubectl logs -n kafka-demo kafka-0

# Verify network connectivity
kubectl exec -it kafka-producer-<pod-id> -n kafka-demo -- \
  nc -zv kafka-0.kafka.kafka-demo.svc.cluster.local 9092
```

### Consumer Not Receiving Messages
```bash
# Check consumer group
kubectl exec -it kafka-0 -n kafka-demo -- \
  kafka-consumer-groups --bootstrap-server localhost:9092 --describe --group kafka-demo-consumer-group

# Check topic messages
kubectl exec -it kafka-0 -n kafka-demo -- \
  kafka-console-consumer --bootstrap-server localhost:9092 --topic demo-topic --from-beginning --max-messages 10
```

### Producer Can't Send Messages
- Verify Kafka is running and accessible
- Check bootstrap servers configuration
- Ensure topic exists (auto-created by default)
- Review producer logs for errors

## License

MIT

## Additional Resources

- [Apache Kafka Documentation](https://kafka.apache.org/documentation/)
- [Confluent.Kafka .NET Client](https://github.com/confluentinc/confluent-kafka-dotnet)
- [.NET Worker Services](https://learn.microsoft.com/en-us/dotnet/core/extensions/workers)
- [Kubernetes StatefulSets](https://kubernetes.io/docs/concepts/workloads/controllers/statefulset/)
