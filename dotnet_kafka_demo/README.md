# .NET Kafka Demo

A simple demonstration application showcasing Apache Kafka messaging with .NET 8, including both producer and consumer functionality.

## Features

- **Producer**: REST API endpoint to send messages to Kafka topics
- **Consumer**: Background service that continuously consumes messages from Kafka topics
- **Health Checks**: Kubernetes-ready health check endpoint
- **Containerization**: Multi-stage Docker build for optimized images
- **Kubernetes**: Complete K8s deployment with Kafka StatefulSet

## Architecture

This demo includes:
- **MessageController**: HTTP API for sending messages to Kafka
- **KafkaProducerService**: Service for producing messages to Kafka topics
- **KafkaConsumerService**: Background service for consuming messages from Kafka topics
- **Kafka StatefulSet**: A single-node Kafka cluster running in Kubernetes

## Prerequisites

- .NET 8 SDK
- Docker
- Kubernetes cluster (for K8s deployment)
- Apache Kafka (if running locally without K8s)

## Configuration

Update `appsettings.json` with your Kafka settings:

```json
{
  "Kafka": {
    "BootstrapServers": "localhost:9092",
    "Topic": "demo-topic",
    "GroupId": "kafka-demo-consumer-group"
  }
}
```

## Running Locally

### Using Docker Compose (Recommended for Local Development)

Create a `docker-compose.yml` file to run Kafka:

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
```

Then run:

```bash
# Start Kafka
docker-compose up -d

# Run the application
dotnet run
```

### Direct Execution

```bash
dotnet restore
dotnet build
dotnet run
```

The API will be available at: `http://localhost:5000` (or `https://localhost:5001`)

## API Usage

### Send a Message

```bash
curl -X POST http://localhost:5000/api/message \
  -H "Content-Type: application/json" \
  -d '{"id": "test-message-123"}'
```

Response:
```json
{
  "success": true,
  "messageId": "demo-topic-0-42",
  "sentId": "test-message-123"
}
```

### Health Check

```bash
curl http://localhost:5000/health
```

Response:
```json
{
  "status": "healthy",
  "timestamp": "2024-11-14T10:30:00Z"
}
```

## Docker

### Build the Image

```bash
docker build -t kafka-demo:latest .
```

### Run with Docker

```bash
docker run -p 8080:8080 \
  -e Kafka__BootstrapServers=kafka:9092 \
  -e Kafka__Topic=demo-topic \
  -e Kafka__GroupId=kafka-demo-consumer-group \
  kafka-demo:latest
```

### Build Multi-Architecture Image

```bash
chmod +x build-multiarch.sh
./build-multiarch.sh
```

## Kubernetes Deployment

### Deploy Everything (Kafka + Application)

```bash
kubectl apply -f k8s-deployment.yaml
```

This will create:
- Namespace: `kafka-demo`
- Kafka StatefulSet with persistent storage
- Kafka Service (headless)
- Application Deployment (2 replicas)
- Application Service (LoadBalancer)
- ConfigMap with Kafka configuration

### Deploy HPA (Optional)

```bash
kubectl apply -f k8s-hpa.yaml
```

### Verify Deployment

```bash
# Check all resources
kubectl get all -n kafka-demo

# Check Kafka pod
kubectl logs -n kafka-demo kafka-0

# Check application pods
kubectl logs -n kafka-demo -l app=kafka-demo

# Check consumed messages (in consumer logs)
kubectl logs -n kafka-demo -l app=kafka-demo --tail=50
```

### Access the Application

```bash
# Get the LoadBalancer IP
kubectl get svc kafka-demo-service -n kafka-demo

# Send a test message
curl -X POST http://<EXTERNAL-IP>/api/message \
  -H "Content-Type: application/json" \
  -d '{"id": "k8s-test-message"}'
```

### Scale the Application

```bash
# Manual scaling
kubectl scale deployment kafka-demo -n kafka-demo --replicas=5

# With HPA enabled, it will auto-scale based on CPU/memory
```

## Message Flow

1. **Send Message**: POST request to `/api/message` endpoint
2. **Producer**: Message is sent to Kafka topic with the ID as the key
3. **Kafka**: Message is stored in the topic partition
4. **Consumer**: Background service continuously polls and processes messages
5. **Logging**: Both producer and consumer log all activities

## Monitoring

Check the logs to see message flow:

```bash
# Local
dotnet run

# Docker
docker logs <container-id>

# Kubernetes
kubectl logs -n kafka-demo -l app=kafka-demo -f
```

You'll see logs like:
```
info: KafkaDemo.Services.KafkaProducerService[0]
      Sending message to Kafka topic: demo-topic
info: KafkaDemo.Services.KafkaProducerService[0]
      Message delivered to topic demo-topic, partition 0, offset 42
info: KafkaDemo.Services.KafkaConsumerService[0]
      Message received from topic demo-topic, partition 0, offset 42: Key=test-123, Value={"Id":"test-123","Timestamp":"2024-11-14T10:30:00Z"}
```

## Cleanup

### Local
```bash
docker-compose down -v
```

### Kubernetes
```bash
kubectl delete namespace kafka-demo
```

## Notes

- The Kafka setup uses KRaft mode (no ZooKeeper required)
- Single-node Kafka is suitable for demos/testing only
- For production, use a proper Kafka cluster with multiple brokers
- The consumer automatically creates the topic if it doesn't exist
- Messages are consumed by the background service and logged to console

## Troubleshooting

### Kafka Connection Issues

If you see connection errors:
1. Verify Kafka is running: `kubectl get pods -n kafka-demo`
2. Check Kafka logs: `kubectl logs -n kafka-demo kafka-0`
3. Verify network connectivity between app and Kafka

### Consumer Not Receiving Messages

1. Check consumer logs for errors
2. Verify topic exists and has messages
3. Ensure consumer group ID is correct
4. Check Kafka broker is accessible

## License

MIT
