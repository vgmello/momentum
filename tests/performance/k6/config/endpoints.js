// API endpoints configuration

const BASE_URL = __ENV.API_BASE_URL || 'http://localhost:8101';
const GRPC_ENDPOINT = __ENV.GRPC_ENDPOINT || 'localhost:8102';

export const endpoints = {
  // Base URLs
  baseUrl: BASE_URL,
  grpcEndpoint: GRPC_ENDPOINT,

  // Cashiers endpoints
  cashiers: {
    list: `${BASE_URL}/cashiers`,
    get: (id) => `${BASE_URL}/cashiers/${id}`,
    create: `${BASE_URL}/cashiers`,
    update: (id) => `${BASE_URL}/cashiers/${id}`,
    delete: (id) => `${BASE_URL}/cashiers/${id}`,
  },

  // Invoices endpoints
  invoices: {
    list: `${BASE_URL}/invoices`,
    get: (id) => `${BASE_URL}/invoices/${id}`,
    create: `${BASE_URL}/invoices`,
    cancel: (id) => `${BASE_URL}/invoices/${id}/cancel`,
    markPaid: (id) => `${BASE_URL}/invoices/${id}/mark-paid`,
    simulatePayment: (id) => `${BASE_URL}/invoices/${id}/simulate-payment`,
  },

  // Health check endpoints
  health: {
    ready: `${BASE_URL}/health/internal`,
    live: `${BASE_URL}/health/live`,
  },
};

// Headers configuration
export const headers = {
  json: {
    'Content-Type': 'application/json',
    'Accept': 'application/json',
  },

  // Multi-tenancy support
  withTenant: (tenantId = 'test-tenant') => ({
    ...headers.json,
    'X-Tenant-Id': tenantId,
  }),
};

// Orleans endpoints (if Orleans testing is enabled)
export const orleansEndpoints = {
  baseUrl: __ENV.ORLEANS_URL || 'http://localhost:8104',

  // Orleans dashboard and grain endpoints
  dashboard: `${__ENV.ORLEANS_URL || 'http://localhost:8104'}/`,
  grains: `${__ENV.ORLEANS_URL || 'http://localhost:8104'}/grains`,
};

// Kafka configuration (if event validation is enabled)
export const kafkaConfig = {
  enabled: __ENV.ENABLE_KAFKA_VALIDATION === 'true',
  bootstrapServers: __ENV.KAFKA_BOOTSTRAP_SERVERS || 'localhost:59092',
  topics: {
    cashiers: 'app-domain.cashiers',
    invoices: 'app-domain.invoices',
  },
};
