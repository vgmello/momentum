import grpc from "k6/net/grpc";
import { check, group, sleep } from "k6";
import { getOptions } from "../../config/options.js";
import { endpoints } from "../../config/endpoints.js";
import { generateCashierData, customMetrics, logTestSummary } from "../../lib/helpers.js";
import http from "k6/http";

// Export test options
export const options = getOptions();

// gRPC client
const client = new grpc.Client();
client.load(["../../protos"], "cashiers.proto");

// Test setup
export function setup() {
    console.log("Starting Cashiers gRPC baseline performance test");
    console.log("Environment:", __ENV.ENVIRONMENT || "local");
    console.log("gRPC Endpoint:", endpoints.grpcEndpoint);

    // Verify API is accessible via HTTP health check
    const healthCheck = http.get(endpoints.health.ready);
    if (healthCheck.status !== 200) {
        throw new Error(`API is not ready: ${healthCheck.status}`);
    }

    return { startTime: new Date().toISOString() };
}

// Helper function to check gRPC response
function checkGrpcResponse(response, operation) {
    const success = response.status === grpc.StatusOK;
    
    check(response, {
        [`${operation} - gRPC status OK`]: (r) => r.status === grpc.StatusOK,
        [`${operation} - has valid response`]: (r) => r.message !== null,
        [`${operation} - response time < 500ms`]: (r) => r && r.timings && r.timings.duration < 500,
    });

    if (!success) {
        console.error(`${operation} gRPC failed:`, JSON.stringify({
            status: response.status,
            message: response.message,
            error: response.error
        }));
    }

    return success;
}

// Main test scenario
export default function () {
    const tenantId = `tenant_${__VU}_${__ITER}`;

    // Connect to gRPC service
    client.connect(endpoints.grpcEndpoint, {
        plaintext: true,
        timeout: "30s",
    });

    group("Cashiers gRPC CRUD Operations", () => {
        let cashierId;
        let cashierVersion;

        // Create cashier
        group("Create Cashier (gRPC)", () => {
            const cashierData = generateCashierData();
            
            const request = {
                name: cashierData.name,
                email: cashierData.email,
            };

            const response = client.invoke("app_domain.cashiers.CashiersService/CreateCashier", request, {
                metadata: {
                    "X-Tenant-Id": tenantId,
                },
                tags: { operation: "create_cashier_grpc" },
            });

            const success = checkGrpcResponse(response, "Create cashier");
            customMetrics.cashierCreationRate.add(success ? 1 : 0);

            if (success && response.message) {
                cashierId = response.message.cashier_id;
                // Note: gRPC version might be handled differently than REST
            }
        });

        // Get cashier by ID
        if (cashierId) {
            group("Get Cashier by ID (gRPC)", () => {
                const request = {
                    id: cashierId,
                };

                const response = client.invoke("app_domain.cashiers.CashiersService/GetCashier", request, {
                    metadata: {
                        "X-Tenant-Id": tenantId,
                    },
                    tags: { operation: "get_cashier_grpc" },
                });

                const success = checkGrpcResponse(response, "Get cashier");
                
                if (success && response.message) {
                    check(response.message, {
                        "Get cashier - cashier_id matches": (cashier) => cashier.cashier_id === cashierId,
                        "Get cashier - has name": (cashier) => cashier.name && cashier.name.length > 0,
                        "Get cashier - has email": (cashier) => cashier.email && cashier.email.includes("@"),
                        "Get cashier - has tenant_id": (cashier) => cashier.tenant_id && cashier.tenant_id.length > 0,
                    });
                }
            });
        }

        // List cashiers
        group("List Cashiers (gRPC)", () => {
            const request = {
                limit: 10,
                offset: 0,
            };

            const response = client.invoke("app_domain.cashiers.CashiersService/GetCashiers", request, {
                metadata: {
                    "X-Tenant-Id": tenantId,
                },
                tags: { operation: "list_cashiers_grpc" },
            });

            const success = checkGrpcResponse(response, "List cashiers");
            
            if (success && response.message) {
                check(response.message, {
                    "List cashiers - has cashiers array": (resp) => resp.cashiers && Array.isArray(resp.cashiers),
                    "List cashiers - contains cashiers": (resp) => resp.cashiers && resp.cashiers.length > 0,
                });
            }
        });

        // Update cashier
        if (cashierId) {
            group("Update Cashier (gRPC)", () => {
                const updateData = generateCashierData();
                
                const request = {
                    cashier_id: cashierId,
                    name: `Updated ${updateData.name}`,
                    email: updateData.email,
                    version: 1, // Default version for gRPC
                };

                const response = client.invoke("app_domain.cashiers.CashiersService/UpdateCashier", request, {
                    metadata: {
                        "X-Tenant-Id": tenantId,
                    },
                    tags: { operation: "update_cashier_grpc" },
                });

                const success = checkGrpcResponse(response, "Update cashier");
                
                if (success && response.message) {
                    check(response.message, {
                        "Update cashier - name updated": (cashier) => cashier.name.startsWith("Updated"),
                        "Update cashier - email updated": (cashier) => cashier.email === updateData.email,
                    });
                }
            });
        }

        // Delete cashier
        if (cashierId) {
            group("Delete Cashier (gRPC)", () => {
                const request = {
                    cashier_id: cashierId,
                };

                const response = client.invoke("app_domain.cashiers.CashiersService/DeleteCashier", request, {
                    metadata: {
                        "X-Tenant-Id": tenantId,
                    },
                    tags: { operation: "delete_cashier_grpc" },
                });

                checkGrpcResponse(response, "Delete cashier");
            });
        }

        // Small pause between iterations
        sleep(0.5);
    });

    // Test search and filtering (using list with different parameters)
    group("Cashiers Search and Filter (gRPC)", () => {
        const request = {
            limit: 5,
            offset: 0,
        };

        const response = client.invoke("app_domain.cashiers.CashiersService/GetCashiers", request, {
            metadata: {
                "X-Tenant-Id": tenantId,
            },
            tags: { operation: "search_cashiers_grpc" },
        });

        checkGrpcResponse(response, "Search cashiers");
    });

    // Close gRPC connection
    client.close();
}

// Test teardown
export function teardown(data) {
    logTestSummary("Cashiers gRPC Baseline Test", {
        "Start Time": data.startTime,
        "End Time": new Date().toISOString(),
        Environment: __ENV.ENVIRONMENT || "local",
        "gRPC Endpoint": endpoints.grpcEndpoint,
    });
}