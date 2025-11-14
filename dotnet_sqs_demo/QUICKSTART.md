# Quick Start Guide

## Local Development

### 1. Run Producer Locally

```bash
cd producer
dotnet restore
dotnet run
```

Access at: `https://localhost:5001`

### 2. Run Consumer Locally

```bash
cd consumer
dotnet restore
dotnet run
```

### 3. Test the Flow

Send a message:
```bash
curl -X POST https://localhost:5001/api/message \
  -H "Content-Type: application/json" \
  -d '{"id": "test-123"}'
```

Watch consumer logs to see it being processed.

---

## Docker Build & Run

### Build Locally

```bash
# Build both services
./build-local.sh all

# Or build individually
./build-local.sh producer
./build-local.sh consumer
```

### Run with Docker

```bash
# Producer
docker run -d -p 8080:8080 \
  -e AWS_ACCESS_KEY_ID=your-key \
  -e AWS_SECRET_ACCESS_KEY=your-secret \
  -e AWS_REGION=us-east-1 \
  -e AWS__SQS__QueueUrl=https://sqs.us-east-1.amazonaws.com/ACCOUNT/QUEUE \
  --name sqs-producer simpledotnetsqs-producer:latest

# Consumer
docker run -d \
  -e AWS_ACCESS_KEY_ID=your-key \
  -e AWS_SECRET_ACCESS_KEY=your-secret \
  -e AWS_REGION=us-east-1 \
  -e AWS__SQS__QueueUrl=https://sqs.us-east-1.amazonaws.com/ACCOUNT/QUEUE \
  --name sqs-consumer simpledotnetsqs-consumer:latest
```

---

## Kubernetes Deployment

### Build and Push Multi-Arch Images

```bash
# Build and push all services
./build-multiarch.sh docker all

# Or individually
./build-multiarch.sh docker producer
./build-multiarch.sh docker consumer
```

### Deploy to K8s

```bash
# Update k8s-deployment.yaml with your AWS credentials and queue URL

# Deploy
kubectl apply -f k8s-deployment.yaml

# Optional: Enable autoscaling
kubectl apply -f k8s-hpa.yaml

# Verify
kubectl get pods -n sqs-demo
kubectl logs -n sqs-demo -l app=sqs-producer --tail=20
kubectl logs -n sqs-demo -l app=sqs-consumer --tail=20
```

### Test in K8s

```bash
# Port forward
kubectl port-forward -n sqs-demo svc/sqs-producer-service 8080:80

# Send test message
curl -X POST http://localhost:8080/api/message \
  -H "Content-Type: application/json" \
  -d '{"id": "k8s-test-123"}'

# Watch consumer logs
kubectl logs -n sqs-demo -l app=sqs-consumer --follow
```

---

## Configuration

### Update Queue URL

**Producer**: Edit `producer/appsettings.json`
```json
{
  "AWS": {
    "SQS": {
      "QueueUrl": "https://sqs.REGION.amazonaws.com/ACCOUNT/QUEUE"
    }
  }
}
```

**Consumer**: Edit `consumer/appsettings.json`
```json
{
  "AWS": {
    "SQS": {
      "QueueUrl": "https://sqs.REGION.amazonaws.com/ACCOUNT/QUEUE",
      "MaxNumberOfMessages": 10,
      "WaitTimeSeconds": 20
    }
  }
}
```

---

## Troubleshooting

### View Logs

```bash
# Kubernetes
kubectl logs -n sqs-demo -l app=sqs-producer --tail=50
kubectl logs -n sqs-demo -l app=sqs-consumer --tail=50 --follow

# Docker
docker logs sqs-producer
docker logs sqs-consumer --follow
```

### Check SQS Queue

```bash
aws sqs get-queue-attributes \
  --queue-url https://sqs.REGION.amazonaws.com/ACCOUNT/QUEUE \
  --attribute-names ApproximateNumberOfMessages
```

### Common Issues

1. **403 Forbidden**: Check AWS credentials and IAM permissions
2. **Connection issues**: Verify network connectivity to AWS
3. **Messages not processing**: Check consumer logs and queue visibility timeout
4. **Producer API not responding**: Check health endpoint `/health`

---

## Next Steps

1. Customize consumer business logic in `consumer/Worker.cs`
2. Add authentication to producer API
3. Configure Dead Letter Queue in AWS
4. Set up monitoring and alerting
5. Implement KEDA for queue-based autoscaling

For more details, see the main [README.md](README.md).
