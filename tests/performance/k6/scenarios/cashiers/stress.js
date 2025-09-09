import http from "k6/http";
import { group, sleep } from "k6";
import { endpoints, headers } from "../../config/endpoints.js";
import { parseResponse, generateCashierData, customMetrics, logTestSummary } from "../../lib/helpers.js";

// Stress test configuration - aggressive performance testing
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
        http_req_failed: ["rate<0.15"], // Allow higher error rate under stress
        http_req_duration: ["p(95)<1000", "p(99)<2000"], // 95% under 1s, 99% under 2s
        cashier_creation_success: ["rate>0.85"], // 85%+ success rate under stress
        http_reqs: ["rate>=100"], // At least 100 requests per second
    },
    tags: {
        testType: "stress",
        service: "cashiers",
    },
};

// Test setup
export function setup() {
    console.log("🔥 Starting Cashiers STRESS Test");
    console.log("Environment:", __ENV.ENVIRONMENT || "local");
    console.log("API URL:", endpoints.baseUrl);
    console.log("Peak Load: 300 concurrent users");
    console.log("Duration: ~12 minutes");

    // Verify API is accessible
    const healthCheck = http.get(endpoints.health.ready);
    if (healthCheck.status !== 200) {
        throw new Error(`API is not ready: ${healthCheck.status}`);
    }

    console.log("✅ API Health Check Passed - Starting stress test...\n");

    return { startTime: new Date().toISOString() };
}

// Main stress test scenario
export default function () {
    const tenantId = `stress_tenant_${__VU}`;

    // Aggressive testing - minimal sleep between operations
    group("Cashier Stress Operations", () => {
        let cashierId;
        let cashierVersion;

        // Create cashier with minimal validation
        const cashierData = generateCashierData();
        const createStartTime = new Date().getTime();

        const createResponse = http.post(endpoints.cashiers.create, JSON.stringify(cashierData), {
            headers: headers.withTenant(tenantId),
            tags: { operation: "create_cashier_stress" },
            timeout: "10s", // Longer timeout for stress conditions
        });

        const createDuration = new Date().getTime() - createStartTime;

        // Track performance degradation
        if (createResponse.status === 201) {
            customMetrics.cashierCreationRate.add(1);
            const cashier = parseResponse(createResponse);
            cashierId = cashier.cashierId;
            cashierVersion = cashier.version;

            // Log slow responses
            if (createDuration > 500) {
                console.log(`⚠️ SLOW: ${createDuration}ms for VU ${__VU}`);
            }
        } else if (createResponse.status === 503) {
            console.error(`🔴 SERVICE UNAVAILABLE at VU ${__VU}`);
            customMetrics.cashierCreationRate.add(0);
        } else if (createResponse.status === 429) {
            console.error(`🔴 RATE LIMITED at VU ${__VU}`);
            customMetrics.cashierCreationRate.add(0);
        } else {
            customMetrics.cashierCreationRate.add(0);
        }

        // Rapid-fire read operations if creation succeeded
        if (cashierId) {
            // Burst read operations
            for (let i = 0; i < 3; i++) {
                http.get(endpoints.cashiers.get(cashierId), {
                    headers: headers.withTenant(tenantId),
                    tags: { operation: "get_cashier_burst" },
                    timeout: "5s",
                });
            }

            // Update with version control
            if (cashierVersion) {
                const updateData = {
                    name: `Stressed ${generateCashierData().name}`,
                    email: generateCashierData().email,
                    version: cashierVersion,
                };

                const updateResponse = http.put(endpoints.cashiers.update(cashierId), JSON.stringify(updateData), {
                    headers: headers.withTenant(tenantId),
                    tags: { operation: "update_cashier_stress" },
                    timeout: "10s",
                });

                if (updateResponse.status === 409) {
                    customMetrics.concurrentVersionErrors.add(1);
                }
            }

            // Aggressive delete
            http.del(endpoints.cashiers.delete(cashierId), null, {
                headers: headers.withTenant(tenantId),
                tags: { operation: "delete_cashier_stress" },
                timeout: "5s",
            });
        }

        // List operations to stress query performance
        const listResponse = http.get(`${endpoints.cashiers.list}?pageSize=100`, {
            headers: headers.withTenant(tenantId),
            tags: { operation: "list_cashiers_stress" },
            timeout: "10s",
        });

        if (listResponse.status !== 200 && listResponse.status !== 503) {
            console.error(`List failed: ${listResponse.status}`);
        }
    });

    // Minimal sleep - high throughput
    sleep(0.1 + Math.random() * 0.2);
}

// Test teardown
export function teardown(data) {
    console.log("\n" + "=".repeat(50));
    logTestSummary("🔥 Cashiers Stress Test Completed", {
        "Start Time": data.startTime,
        "End Time": new Date().toISOString(),
        Environment: __ENV.ENVIRONMENT || "local",
        "API URL": endpoints.baseUrl,
        "Peak Concurrent Users": "300",
        "Test Type": "Stress Test",
    });

    console.log("\n📊 Expected Outcomes:");
    console.log("- System should handle up to 200 users gracefully");
    console.log("- Performance degradation expected at 300 users");
    console.log("- Error rate should stay below 15%");
    console.log("- No data corruption under stress");
    console.log("=".repeat(50));
}
