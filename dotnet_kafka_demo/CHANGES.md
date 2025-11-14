# Changes and Updates

## v1.0.0 - Initial Release

### Features
- ✅ Separate Producer and Consumer services
- ✅ Producer: REST API with Swagger documentation
- ✅ Consumer: Background worker service with batch commit
- ✅ Kafka StatefulSet for Kubernetes deployment
- ✅ Health checks and readiness probes
- ✅ Horizontal Pod Autoscaling (HPA) support
- ✅ Multi-architecture Docker builds (amd64, arm64)
- ✅ Comprehensive documentation

### Architecture
- Producer: Web API (.NET 8)
- Consumer: Worker Service (.NET 8)
- Kafka: Confluent Platform 7.5.0 (KRaft mode)

### Configuration
- Configurable bootstrap servers
- Configurable topic names
- Consumer group management
- Batch commit optimization

### Build Scripts
- `build-local.sh`: Local development builds
- `build-multiarch.sh`: Multi-architecture builds for Docker/Podman

### Kubernetes Resources
- Namespace: kafka-demo
- StatefulSet: Kafka broker with persistent storage
- Deployments: Producer (2 replicas), Consumer (2 replicas)
- Services: Producer API, Kafka headless service
- ConfigMap: Application configuration
- Ingress: Producer API exposure
- HPA: Auto-scaling for producer and consumer

### Documentation
- README.md: Comprehensive guide
- QUICKSTART.md: Quick start instructions
- CHANGES.md: This file
