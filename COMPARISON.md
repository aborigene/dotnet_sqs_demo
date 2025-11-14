# SQS vs Kafka Implementation Comparison

This document highlights the key differences between the AWS SQS and Apache Kafka implementations.

## Architecture Similarities

Both implementations follow the same microservices pattern:
- **Producer**: Web API that accepts HTTP POST requests and publishes messages
- **Consumer**: Background worker service that processes messages
- Same project structure with separate producer/consumer folders
- Identical build scripts and Kubernetes deployment patterns

## Key Differences

### 1. Message Broker

**SQS:**
- Managed AWS service (cloud-based)
- No broker deployment needed
- Queue-based messaging (point-to-point)

**Kafka:**
- Self-hosted broker (StatefulSet in K8s)
- Persistent storage required
- Topic-based streaming (pub/sub)

### 2. Configuration

**SQS (`appsettings.json`):**
```json
{
  "AWS": {
    "SQS": {
      "QueueUrl": "https://sqs.us-east-1.amazonaws.com/...",
      "MaxNumberOfMessages": 10,
      "WaitTimeSeconds": 20
    }
  }
}
```

**Kafka (`appsettings.json`):**
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

### 3. NuGet Packages

**SQS:**
```xml
<PackageReference Include="AWSSDK.SQS" Version="3.7.400.27" />
```

**Kafka:**
```xml
<PackageReference Include="Confluent.Kafka" Version="2.3.0" />
```

### 4. Producer Service

**SQS:**
- Uses `IAmazonSQS` client
- Sends to queue URL
- Returns MessageId from SQS

**Kafka:**
- Uses `IProducer<string, string>` client
- Produces to topic with key/value
- Returns TopicPartitionOffset

### 5. Consumer Service

**SQS:**
- Polls queue with long polling (20 seconds)
- Receives batch of messages
- Must explicitly delete messages after processing
- Visibility timeout mechanism

**Kafka:**
- Subscribes to topic
- Consumes messages continuously
- Manual offset commit in batches
- Consumer group coordination

### 6. Message Handling

**SQS:**
```csharp
var response = await _sqsClient.ReceiveMessageAsync(receiveMessageRequest);
foreach (var message in response.Messages)
{
    await ProcessMessageAsync(message);
    await DeleteMessageAsync(message.ReceiptHandle);
}
```

**Kafka:**
```csharp
_consumer.Subscribe(_topic);
var consumeResult = _consumer.Consume(stoppingToken);
await ProcessMessageAsync(consumeResult);
_consumer.Commit(consumeResult);
```

### 7. Kubernetes Deployment

**SQS:**
- No broker pods needed
- Requires AWS credentials (Secret)
- ConfigMap with queue URL

**Kafka:**
- StatefulSet for Kafka broker
- PersistentVolumeClaim for data storage
- Headless service for Kafka
- No external credentials needed

### 8. Authentication

**SQS:**
- AWS credentials (Access Key + Secret Key)
- IAM roles in production
- Region configuration

**Kafka:**
- No authentication in basic setup
- Can add SASL/SSL for production
- Bootstrap servers connection

### 9. Ports

**SQS Producer:**
- Port 5000 (HTTP)

**Kafka Producer:**
- Port 8080 (HTTP)

**Kafka Broker:**
- Port 9092 (client connections)
- Port 9093 (controller)

### 10. Health Checks

Both implement the same health check endpoint:
```csharp
app.MapGet("/health", () => Results.Ok(new { 
    status = "healthy", 
    timestamp = DateTime.UtcNow 
}));
```

## Code Structure Comparison

### Producer Controller (Identical Logic)

Both implement the same endpoint structure:
- `POST /api/message`
- Accepts `MessageRequest` with Id
- Returns success response with messageId

### Consumer Worker (Similar Pattern)

Both implement:
- `BackgroundService` base class
- Continuous message processing loop
- Error handling with delays
- Graceful shutdown

### Message Format (Identical)

Both use the same JSON message structure:
```json
{
  "Id": "user-provided-id",
  "Timestamp": "2024-11-14T10:30:00Z"
}
```

## Performance Characteristics

| Feature | SQS | Kafka |
|---------|-----|-------|
| Throughput | Moderate (thousands/sec) | High (millions/sec) |
| Latency | ~10-20ms | ~1-5ms |
| Message Order | FIFO queues only | Partition-level ordering |
| Message Replay | Limited (DLQ) | Full replay capability |
| Retention | 14 days max | Configurable (days/TB) |
| Scaling | Automatic | Manual/Auto (K8s HPA) |

## Use Case Recommendations

**Choose SQS when:**
- Running in AWS ecosystem
- Need managed service (zero ops)
- Queue-based messaging sufficient
- Moderate throughput requirements
- Want built-in AWS integration

**Choose Kafka when:**
- Need high throughput streaming
- Require message replay capability
- Event sourcing architecture
- Real-time data pipelines
- Multi-subscriber scenarios
- Self-hosted or multi-cloud

## Build Scripts

Both use identical build script structure:
- `build-local.sh` - Local development builds
- `build-multiarch.sh` - Multi-arch container builds
- Support for Docker and Podman
- Same command-line interfaces

## Deployment Patterns

Both support:
- Local development with Docker
- Kubernetes deployment with manifests
- Horizontal Pod Autoscaling (HPA)
- Multi-replica deployments
- Health checks and readiness probes

## Migration Path

Converting from SQS to Kafka (or vice versa) requires:

1. **Configuration Changes**: Update appsettings.json
2. **NuGet Packages**: Swap AWSSDK.SQS ↔ Confluent.Kafka
3. **Service Implementation**: Reimplement producer/consumer logic
4. **Kubernetes Manifests**: Add/remove Kafka broker, update ConfigMap
5. **Credentials**: Remove AWS credentials (for Kafka) or add them (for SQS)

The high-level application logic (controller, models, business logic) remains largely unchanged.

## Summary

Both implementations provide production-ready microservices architectures with proper separation of concerns, health monitoring, and scalability. The choice between SQS and Kafka depends primarily on your infrastructure, throughput requirements, and message consumption patterns.
