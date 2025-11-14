# Quick Start Guide

## Local Development (Fastest Way to Test)

### 1. Start Kafka

```bash
docker run -d --name kafka -p 9092:9092 \
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
```

### 2. Update Configuration

Update `producer/appsettings.json` and `consumer/appsettings.json`:
```json
{
  "Kafka": {
    "BootstrapServers": "localhost:9092",
    "Topic": "demo-topic"
  }
}
```

### 3. Run Producer

```bash
cd producer
dotnet run
```

Producer API will be available at: http://localhost:5000

### 4. Run Consumer (in new terminal)

```bash
cd consumer
dotnet run
```

### 5. Send Test Message

```bash
curl -X POST http://localhost:5000/api/message \
  -H "Content-Type: application/json" \
  -d '{"id": "test-123"}'
```

You'll see the message in the consumer logs!

## Kubernetes Deployment

### 1. Deploy Everything

```bash
kubectl apply -f k8s-deployment.yaml
```

### 2. Wait for Pods

```bash
kubectl get pods -n kafka-demo -w
```

### 3. Port Forward Producer

```bash
kubectl port-forward svc/kafka-producer-service 8080:80 -n kafka-demo
```

### 4. Send Test Message

```bash
curl -X POST http://localhost:8080/api/message \
  -H "Content-Type: application/json" \
  -d '{"id": "k8s-test-123"}'
```

### 5. Check Consumer Logs

```bash
kubectl logs -n kafka-demo -l app=kafka-consumer -f
```

## Cleanup

### Local
```bash
docker stop kafka
docker rm kafka
```

### Kubernetes
```bash
kubectl delete namespace kafka-demo
```
