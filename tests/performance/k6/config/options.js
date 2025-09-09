// Base k6 configuration options

export const baseOptions = {
    // Default thresholds for all tests
    thresholds: {
        http_req_failed: ["rate<0.1"], // http errors should be less than 10%
        http_req_duration: ["p(95)<500", "p(99)<1000"], // 95% below 500ms, 99% below 1s
        http_reqs: ["rate>=10"], // at least 10 requests per second
        grpc_req_duration: ["p(95)<1000", "p(99)<2000"], // gRPC allows higher latency due to connection overhead
    },

    // Default tags
    tags: {
        environment: __ENV.ENVIRONMENT || "local",
        testType: "performance",
    },

    // Network configuration
    noConnectionReuse: false,
    userAgent: "k6-performance-test/1.0",

    // Batch configuration for better performance
    batch: 20,
    batchPerHost: 6,

    // HTTP settings
    httpDebug: __ENV.DEBUG === "true" ? "full" : "",

    // Summary trend stats to collect
    summaryTrendStats: ["avg", "min", "med", "max", "p(90)", "p(95)", "p(99)"],
};

// Environment-specific configurations
export const environments = {
    local: {
        stages: [
            { duration: "30s", target: 10 }, // Ramp up to 10 users
            { duration: "2m", target: 10 }, // Stay at 10 users
            { duration: "30s", target: 0 }, // Ramp down to 0 users
        ],
        ...baseOptions,
    },

    staging: {
        stages: [
            { duration: "1m", target: 50 }, // Ramp up to 50 users
            { duration: "3m", target: 100 }, // Ramp up to 100 users
            { duration: "5m", target: 100 }, // Stay at 100 users
            { duration: "2m", target: 0 }, // Ramp down to 0 users
        ],
        ...baseOptions,
        thresholds: {
            ...baseOptions.thresholds,
            http_req_duration: ["p(95)<300"], // Tighter SLA for staging
        },
    },

    stress: {
        stages: [
            { duration: "2m", target: 100 }, // Ramp up to 100 users
            { duration: "5m", target: 100 }, // Stay at 100 users
            { duration: "2m", target: 200 }, // Ramp up to 200 users
            { duration: "5m", target: 200 }, // Stay at 200 users
            { duration: "2m", target: 300 }, // Ramp up to 300 users
            { duration: "5m", target: 300 }, // Stay at 300 users
            { duration: "10m", target: 0 }, // Ramp down to 0 users
        ],
        ...baseOptions,
        thresholds: {
            ...baseOptions.thresholds,
            http_req_failed: ["rate<0.2"], // Allow higher error rate for stress test
        },
    },

    spike: {
        stages: [
            { duration: "10s", target: 10 }, // Baseline load
            { duration: "2m", target: 10 }, // Stay at baseline
            { duration: "10s", target: 200 }, // Spike to 200 users
            { duration: "3m", target: 200 }, // Stay at spike
            { duration: "10s", target: 10 }, // Back to baseline
            { duration: "2m", target: 10 }, // Stay at baseline
            { duration: "10s", target: 0 }, // Ramp down
        ],
        ...baseOptions,
    },

    soak: {
        stages: [
            { duration: "2m", target: 50 }, // Ramp up to 50 users
            { duration: "30m", target: 50 }, // Stay at 50 users for 30 minutes
            { duration: "2m", target: 0 }, // Ramp down to 0 users
        ],
        ...baseOptions,
    },
};

// Get options for current environment
export function getOptions() {
    const env = __ENV.ENVIRONMENT || "local";
    return environments[env] || environments.local;
}
