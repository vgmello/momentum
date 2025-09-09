# k6 Performance Testing for Momentum .NET

This directory contains k6 performance tests for the Cashiers and Invoices microservices in the Momentum .NET application.

## Overview

The k6 performance testing suite provides comprehensive load testing capabilities for:
- REST API endpoints (Cashiers and Invoices)
- gRPC endpoints (with Protocol Buffers)
- Realistic user workflows
- Mixed service scenarios

## Quick Start

### Using pnpm Scripts (Recommended)

1. Install dependencies:
```bash
pnpm install
```

2. Run test scenarios with built-in web dashboard:
```bash
# Run realistic mixed workflow test
pnpm test:mixed

# Run cashiers baseline test (REST API)
pnpm test:cashiers

# Run cashiers gRPC test
pnpm test:cashiers:grpc

# Run invoices baseline test
pnpm test:invoices

# Run stress and spike tests
pnpm test:cashiers:stress
pnpm test:cashiers:spike
```

3. Access the k6 Web Dashboard during tests:
   - **URL**: http://localhost:5665
   - **Real-time metrics**: CPU, Memory, HTTP requests, response times
   - **Interactive charts**: Customize views and time ranges
   - **Export**: HTML reports automatically saved to `results/` directory

### Using Aspire

1. Start the application with Aspire:
```bash
dotnet run --project src/AppDomain.AppHost
```

2. Access the dashboards:
   - **Aspire Dashboard**: https://localhost:18110 (orchestration and service health)

### Direct k6 Usage

You can also run k6 directly using Docker:
```bash
# Run with web dashboard
docker run --rm --network host -v $(pwd):/scripts -w /scripts \
  -e K6_WEB_DASHBOARD=true \
  grafana/k6 run scenarios/cashiers/baseline.js
```

## Project Structure

```
k6/
├── package.json           # Node.js dependencies and npm scripts
├── pnpm-lock.yaml        # Locked dependency versions
├── config/
│   ├── options.js        # k6 test options and stages
│   └── endpoints.js      # API endpoint configurations
├── lib/
│   └── helpers.js        # Utility functions and custom metrics
├── protos/
│   └── cashiers.proto    # Protocol Buffers definitions for gRPC
├── scenarios/
│   ├── cashiers/
│   │   ├── baseline.js          # Cashiers CRUD operations (REST)
│   │   ├── baseline-grpc.js     # Cashiers CRUD operations (gRPC)
│   │   ├── simple-create.js     # Quick smoke test (REST)
│   │   ├── simple-create-grpc.js # Quick smoke test (gRPC)
│   │   ├── stress.js            # Cashiers stress test (300 users)
│   │   └── spike.js             # Cashiers spike test (sudden load)
│   ├── invoices/
│   │   └── baseline.js          # Invoices lifecycle test
│   └── mixed/
│       └── realistic-workflow.js # Mixed user scenarios
└── results/              # Test output and HTML reports
```

## Test Scenarios

### 1. Baseline Tests
Basic CRUD operations with steady load:
- **Cashiers (REST)**: Create, Read, Update, Delete operations (`scenarios/cashiers/baseline.js`)
- **Cashiers (gRPC)**: Create, Read, Update, Delete operations (`scenarios/cashiers/baseline-grpc.js`)
- **Invoices**: Create, Pay, Cancel operations (`scenarios/invoices/baseline.js`)
- **Simple Create (REST)**: Quick smoke test for cashier creation (`scenarios/cashiers/simple-create.js`)
- **Simple Create (gRPC)**: Quick smoke test for cashier creation (`scenarios/cashiers/simple-create-grpc.js`)

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

The tests use environment variables for configuration:

```bash
# API Configuration (defaults)
API_BASE_URL=http://localhost:8101
GRPC_ENDPOINT=localhost:8102

# Environment Profile (controls test duration and load)
ENVIRONMENT=local              # Default: light load for development
ENVIRONMENT=staging           # Medium load for staging environment
ENVIRONMENT=stress           # Heavy load for stress testing

# k6 Web Dashboard
K6_WEB_DASHBOARD=true        # Enable web dashboard (set by npm scripts)
K6_WEB_DASHBOARD_EXPORT=path # Export HTML report after test completion
```

### Custom Metrics

The tests track custom business metrics:
- `cashier_creation_success`: Success rate for cashier creation
- `invoice_creation_success`: Success rate for invoice creation
- `payment_processing_time`: Time taken to process payments
- `concurrent_version_errors`: Rate of optimistic concurrency conflicts

## Running Tests

### Available npm Scripts

All scripts automatically enable the web dashboard and save HTML reports:

```bash
# Mixed scenarios
pnpm test:mixed              # Realistic workflow test
pnpm test:local              # Local environment test
pnpm test:staging            # Staging environment test

# Cashiers tests
pnpm test:cashiers           # REST API baseline
pnpm test:cashiers:grpc      # gRPC baseline
pnpm test:cashiers:simple    # REST simple create
pnpm test:cashiers:grpc:simple # gRPC simple create
pnpm test:cashiers:stress    # Stress test (300 users)
pnpm test:cashiers:spike     # Spike test

# Invoices tests
pnpm test:invoices           # Invoices baseline
```

### Direct k6 Commands

```bash
# Basic run with web dashboard
docker run --rm --network host -v $(pwd):/scripts -w /scripts \
  -e K6_WEB_DASHBOARD=true \
  grafana/k6 run scenarios/cashiers/baseline.js

# With custom VUs and duration
docker run --rm --network host -v $(pwd):/scripts -w /scripts \
  grafana/k6 run --vus 50 --duration 5m scenarios/invoices/baseline.js

# With specific environment
docker run --rm --network host -v $(pwd):/scripts -w /scripts \
  -e ENVIRONMENT=staging \
  grafana/k6 run scenarios/mixed/realistic-workflow.js

# Output to JSON (in addition to web dashboard)
docker run --rm --network host -v $(pwd):/scripts -w /scripts \
  grafana/k6 run --out json=results/test-results.json scenarios/cashiers/baseline.js
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
