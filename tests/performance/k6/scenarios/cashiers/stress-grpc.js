import grpc from "k6/net/grpc";
import { check, group, sleep } from "k6";
import { endpoints } from "../../config/endpoints.js";
import { generateCashierData, customMetrics, logTestSummary, random } from "../../lib/helpers.js";
import http from "k6/http";

// Stress test configuration - aggressive gRPC performance testing
export const options = {
    stages: [
        { duration: "30s", target: 50 }, // Ramp up to 50 users
        { duration: "1m", target: 100 }, // Ramp up to 100 users
        { duration: "2m", target: 200 }, // Ramp up to 200 users (stress level)
        { duration: "3m", target: 200 }, // Stay at 200 users
        { duration: "1m", target: 300 }, // Push to 300 users (breaking point)
        { duration: "2m", target: 300 }, // Maintain peak stress
        { duration: "2m", target: 0 }, // Ramp down to 0 users
    ],
    thresholds: {
        grpc_req_duration: ["p(95)<1500", "p(99)<3000"], // 95% under 1.5s, 99% under 3s
        checks: ["rate>0.85"], // 85%+ success rate under stress
    },
    tags: {
        testType: "stress",
        service: "cashiers-grpc",
    },
};

// gRPC client
const client = new grpc.Client();
client.load(["../../protos"], "cashiers.proto");

// Test setup
export function setup() {
    console.log("🔥 Starting Cashiers gRPC STRESS Test");
    console.log("Environment:", __ENV.ENVIRONMENT || "local");
    console.log("gRPC Endpoint:", endpoints.grpcEndpoint);
    console.log("Peak Load: 300 concurrent users");
    console.log("Duration: ~12 minutes");

    // Verify API is accessible via HTTP health check
    const healthCheck = http.get(endpoints.health.ready);
    if (healthCheck.status !== 200) {
        throw new Error(`API is not ready: ${healthCheck.status}`);
    }

    console.log("✅ API Health Check Passed - Starting gRPC stress test...\n");

    return { startTime: new Date().toISOString() };
}

// Main stress test scenario
export default function main() {
    const tenantId = `stress_tenant_${__VU}`;
    
    // Connect to gRPC service with aggressive timeout
    client.connect(endpoints.grpcEndpoint, {
        plaintext: true,
        timeout: "10s", // Longer timeout for stress conditions
    });

    // Aggressive testing - minimal sleep between operations
    group("Cashier gRPC Stress Operations", () => {
        let cashierId;
        let cashierVersion = 1; // Default version for gRPC

        // Create cashier with minimal validation
        const cashierData = generateCashierData();
        const createStartTime = Date.now();

        const createRequest = {
            name: cashierData.name,
            email: cashierData.email,
        };

        const createResponse = client.invoke("app_domain.cashiers.CashiersService/CreateCashier", createRequest, {
            metadata: {
                "X-Tenant-Id": tenantId,
            },
            tags: { operation: "create_cashier_grpc_stress" },
        });

        const createDuration = Date.now() - createStartTime;

        // Track performance degradation
        const createSuccess = check(createResponse, {
            "Create cashier - gRPC status OK": (r) => r.status === grpc.StatusOK,
        });

        if (createSuccess && createResponse.message) {
            customMetrics.cashierCreationRate.add(1);
            cashierId = createResponse.message.cashier_id;

            // Log slow responses
            if (createDuration > 500) {
                console.log(`⚠️ SLOW gRPC: ${createDuration}ms for VU ${__VU}`);
            }
        } else if (createResponse.status === grpc.StatusUnavailable) {
            console.error(`🔴 gRPC SERVICE UNAVAILABLE at VU ${__VU}`);
            customMetrics.cashierCreationRate.add(0);
        } else if (createResponse.status === grpc.StatusResourceExhausted) {
            console.error(`🔴 gRPC RESOURCE EXHAUSTED at VU ${__VU}`);
            customMetrics.cashierCreationRate.add(0);
        } else {
            customMetrics.cashierCreationRate.add(0);
        }

        // Rapid-fire read operations if creation succeeded
        if (cashierId) {
            // Burst read operations
            for (let i = 0; i < 3; i++) {
                const getRequest = {
                    id: cashierId,
                };

                const getResponse = client.invoke("app_domain.cashiers.CashiersService/GetCashier", getRequest, {
                    metadata: {
                        "X-Tenant-Id": tenantId,
                    },
                    tags: { operation: "get_cashier_grpc_burst" },
                });

                check(getResponse, {
                    "Get cashier burst - gRPC status OK": (r) => r.status === grpc.StatusOK,
                });
            }

            // Update with version control
            const updateData = generateCashierData();
            const updateRequest = {
                cashier_id: cashierId,
                name: `Stressed ${updateData.name}`,
                email: updateData.email,
                version: cashierVersion,
            };

            const updateResponse = client.invoke("app_domain.cashiers.CashiersService/UpdateCashier", updateRequest, {
                metadata: {
                    "X-Tenant-Id": tenantId,
                },
                tags: { operation: "update_cashier_grpc_stress" },
            });

            check(updateResponse, {
                "Update cashier - gRPC status OK": (r) => r.status === grpc.StatusOK,
            });

            if (updateResponse.status === grpc.StatusAborted) {
                customMetrics.concurrentVersionErrors.add(1);
            }

            // Aggressive delete
            const deleteRequest = {
                cashier_id: cashierId,
            };

            const deleteResponse = client.invoke("app_domain.cashiers.CashiersService/DeleteCashier", deleteRequest, {
                metadata: {
                    "X-Tenant-Id": tenantId,
                },
                tags: { operation: "delete_cashier_grpc_stress" },
            });

            check(deleteResponse, {
                "Delete cashier - gRPC status OK": (r) => r.status === grpc.StatusOK,
            });
        }

        // List operations to stress query performance
        const listRequest = {
            limit: 100,
            offset: 0,
        };

        const listResponse = client.invoke("app_domain.cashiers.CashiersService/GetCashiers", listRequest, {
            metadata: {
                "X-Tenant-Id": tenantId,
            },
            tags: { operation: "list_cashiers_grpc_stress" },
        });

        const listSuccess = check(listResponse, {
            "List cashiers - gRPC status OK": (r) => r.status === grpc.StatusOK,
        });

        if (!listSuccess && listResponse.status !== grpc.StatusUnavailable) {
            console.error(`gRPC List failed: ${listResponse.status}`);
        }
    });

    // Close gRPC connection
    client.close();

    // Minimal sleep - high throughput
    sleep(0.1 + random() * 0.2);
}

// Test teardown
export function teardown(data) {
    console.log("\n" + "=".repeat(50));
    logTestSummary("🔥 Cashiers gRPC Stress Test Completed", {
        "Start Time": data.startTime,
        "End Time": new Date().toISOString(),
        Environment: __ENV.ENVIRONMENT || "local",
        "gRPC Endpoint": endpoints.grpcEndpoint,
        "Peak Concurrent Users": "300",
        "Test Type": "gRPC Stress Test",
    });

    console.log("\n📊 Expected Outcomes:");
    console.log("- System should handle up to 200 users gracefully");
    console.log("- Performance degradation expected at 300 users");
    console.log("- Error rate should stay below 15%");
    console.log("- No data corruption under stress");
    console.log("- gRPC connection pooling should handle load");
    console.log("=".repeat(50));
}