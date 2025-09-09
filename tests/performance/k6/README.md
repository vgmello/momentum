# k6 Performance Testing for Momentum .NET

This directory contains k6 performance tests for the Cashiers and Invoices microservices in the Momentum .NET application.

## Overview

The k6 performance testing suite provides comprehensive load testing capabilities for:
- REST API endpoints (Cashiers and Invoices)
- gRPC endpoints
- Realistic user workflows
- Orleans actor grain performance (isolated)
- Kafka event streaming validation (configurable)

## Quick Start

### Using Docker Compose

1. Start the application with performance testing profile:
```bash
# Start all services including k6 with web dashboard
docker compose --profile api up --build

# Or just run the performance tests (assuming services are running)
docker compose run k6-performance
```

2. Access the k6 Web Dashboard:
   - **URL**: http://localhost:5665
   - **Real-time metrics**: CPU, Memory, HTTP requests, response times
   - **Interactive charts**: Customize views and time ranges
   - **Export**: Download HTML reports after test completion

2. Run specific test scenario:
```bash
# Run cashiers baseline test
docker compose run k6-performance run /scripts/scenarios/cashiers/baseline.js

# Run invoices baseline test
docker compose run k6-performance run /scripts/scenarios/invoices/baseline.js
```

### Using Aspire

1. Start the application with Aspire:
```bash
dotnet run --project src/AppDomain.AppHost
```

2. Access the dashboards:
   - **Aspire Dashboard**: https://localhost:18100 (orchestration and service health)

The k6 container will be available in the Aspire dashboard and can be executed from there.

### Local Development

TBD

## Project Structure

```
k6/
├── Dockerfile              # k6 container configuration
├── package.json           # Node.js dependencies
├── run-tests.sh          # Test runner script
├── .env.example          # Environment variables template
├── config/
│   ├── options.js        # k6 test options and stages
│   └── endpoints.js      # API endpoint configurations
├── lib/
│   └── helpers.js        # Utility functions and custom metrics
├── scenarios/
│   ├── cashiers/
│   │   ├── baseline.js          # Cashiers CRUD operations
│   │   ├── simple-create.js     # Quick smoke test
│   │   ├── stress.js            # Cashiers stress test (300 users)
│   │   └── spike.js             # Cashiers spike test (sudden load)
│   ├── invoices/
│   │   └── baseline.js          # Invoices lifecycle test
│   └── mixed/
│       └── realistic-workflow.js # Mixed user scenarios
└── results/              # Test output directory
```

## Test Scenarios

### 1. Baseline Tests
Basic CRUD operations with steady load:
- **Cashiers**: Create, Read, Update, Delete operations (`scenarios/cashiers/baseline.js`)
- **Invoices**: Create, Pay, Cancel operations (`scenarios/invoices/baseline.js`)
- **Simple Create**: Quick smoke test for cashier creation (`scenarios/cashiers/simple-create.js`)

### 2. Realistic Workflow
Simulates different user types (`scenarios/mixed/realistic-workflow.js`):
- **Power Users**: Multiple invoice creation, cashier management
- **Regular Users**: Invoice creation and payment
- **Admin Users**: Cashier management, invoice review
- **Read-Only Users**: Data viewing and reporting

### 3. Stress Tests
Gradually increases load to find breaking points (`scenarios/cashiers/stress.js`):
- Ramps up to 300 virtual users over 12 minutes
- Monitors response times and error rates
- Identifies performance bottlenecks
- Allows 15% error rate under extreme stress
- Tracks 503 (Service Unavailable) and 429 (Rate Limited) errors

### 4. Spike Tests
Sudden traffic increases (`scenarios/cashiers/spike.js`):
- Pattern: 10 → 200 → 10 → 250 → 0 users
- Tests system recovery and elasticity
- Tracks recovery time between spikes
- Phase-aware metrics (baseline vs spike)
- Duration: ~8 minutes

### 5. Soak Tests
Sustained load over extended period:
- 50 users for 30 minutes
- Identifies memory leaks and resource exhaustion
- (Configuration available in `config/options.js`)

## Environment Profiles

### Local (Default)
```javascript
stages: [
  { duration: '30s', target: 10 },  // Ramp up
  { duration: '2m', target: 10 },   // Steady state
  { duration: '30s', target: 0 },   // Ramp down
]
```

### Staging
```javascript
stages: [
  { duration: '1m', target: 50 },   // Ramp up
  { duration: '3m', target: 100 },  // Increase load
  { duration: '5m', target: 100 },  // Steady state
  { duration: '2m', target: 0 },    // Ramp down
]
```

### Stress
```javascript
stages: [
  { duration: '2m', target: 100 },  // Initial load
  { duration: '5m', target: 200 },  // Increase stress
  { duration: '5m', target: 300 },  // Maximum stress
  { duration: '10m', target: 0 },   // Recovery
]
```

## Configuration

### Environment Variables

Create a `.env` file based on `.env.example`:

```bash
# API Configuration
API_BASE_URL=http://localhost:8101
GRPC_ENDPOINT=localhost:8102

# Environment Profile
ENVIRONMENT=local
```

### Custom Metrics

The tests track custom business metrics:
- `cashier_creation_success`: Success rate for cashier creation
- `invoice_creation_success`: Success rate for invoice creation
- `payment_processing_time`: Time taken to process payments
- `concurrent_version_errors`: Rate of optimistic concurrency conflicts

## Running Tests

### Command Line Options

```bash
# Basic run
k6 run scenarios/cashiers/baseline.js

# With custom VUs and duration
k6 run --vus 50 --duration 5m scenarios/invoices/baseline.js

# With specific environment
ENVIRONMENT=staging k6 run scenarios/mixed/realistic-workflow.js

# Run realistic workflow scenario
k6 run scenarios/mixed/realistic-workflow.js

# Output to JSON
k6 run --out json=results/test-results.json scenarios/cashiers/baseline.js

# With HTML report
k6 run --out html=results/report.html scenarios/invoices/baseline.js
```

### Docker Compose Commands

```bash
# Run default scenario (cashiers baseline) with web dashboard
docker compose run k6-performance
# Access dashboard at http://localhost:5665

# Run specific scenarios
docker compose run k6-performance run /scripts/scenarios/cashiers/stress.js
docker compose run k6-performance run /scripts/scenarios/cashiers/spike.js
docker compose run k6-performance run /scripts/scenarios/cashiers/simple-create.js

# Run with web dashboard enabled
docker compose run k6-performance run --web-dashboard /scripts/scenarios/cashiers/stress.js

# Run with environment override
docker compose run -e ENVIRONMENT=staging k6-performance

# Run specific scenario
docker compose run k6-performance run /scripts/scenarios/cashiers/baseline.js

# Run with volume for results
docker compose run -v $(pwd)/results:/results k6-performance
```

## Analyzing Results

### k6 Web Dashboard (Real-time)
Access **http://localhost:5665** during test execution for:
- **Live metrics**: Request rate, response times, active VUs
- **Interactive charts**: Zoom, pan, and filter metrics
- **System metrics**: CPU and memory usage
- **Custom metrics**: Business-specific KPIs
- **HTML export**: Download complete test report after execution

### Console Output
k6 provides real-time metrics during test execution:
- Active VUs
- Request rate
- Response times (avg, p95, p99)
- Error rate

## Best Practices

1. **Start Small**: Begin with baseline tests before stress testing
2. **Monitor Resources**: Watch CPU, memory, and network during tests
3. **Incremental Load**: Gradually increase load to identify limits
4. **Realistic Scenarios**: Use workflows that match actual user behavior
5. **Regular Testing**: Run performance tests as part of CI/CD pipeline
6. **Baseline Metrics**: Establish performance baselines for comparison
7. **Clean Data**: Ensure test data doesn't pollute production systems

## Contributing

When adding new test scenarios:

1. Follow the existing structure in `scenarios/`
2. Use helper functions from `lib/helpers.js`
3. Add custom metrics for business-critical operations
4. Document the scenario purpose and expected outcomes
5. Include both positive and negative test cases
6. Consider different user personas and workflows

## Resources

- [k6 Documentation](https://k6.io/docs/)
- [k6 Examples](https://github.com/grafana/k6-examples)
- [Performance Testing Best Practices](https://k6.io/docs/testing-guides/)
