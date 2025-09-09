import http from "k6/http";
import { group, sleep } from "k6";
import { endpoints, headers } from "../../config/endpoints.js";
import { checkResponse, parseResponse, generateCashierData, customMetrics, logTestSummary } from "../../lib/helpers.js";
import { Trend, Rate } from "k6/metrics";

// Custom metrics for spike testing
const spikeResponseTime = new Trend("spike_response_time");
const recoveryTime = new Trend("recovery_time");
const spikeErrorRate = new Rate("spike_error_rate");

// Spike test configuration - sudden traffic increase
export const options = {
    stages: [
        { duration: "30s", target: 10 }, // Baseline load
        { duration: "1m", target: 10 }, // Maintain baseline
        { duration: "10s", target: 200 }, // SPIKE! Sudden jump to 200 users
        { duration: "2m", target: 200 }, // Maintain spike load
        { duration: "10s", target: 10 }, // Drop back to baseline
        { duration: "1m", target: 10 }, // Recovery period
        { duration: "10s", target: 250 }, // Second SPIKE! Even higher
        { duration: "1m", target: 250 }, // Maintain second spike
        { duration: "30s", target: 0 }, // Ramp down to 0
    ],
    thresholds: {
        http_req_failed: ["rate<0.2"], // Allow 20% error rate during spikes
        http_req_duration: ["p(95)<2000"], // 95% under 2s during spikes
        spike_response_time: ["p(95)<1500"], // Spike-specific response time
        spike_error_rate: ["rate<0.25"], // Spike-specific error tolerance
        cashier_creation_success: ["rate>0.75"], // 75%+ success rate during spikes
    },
    tags: {
        testType: "spike",
        service: "cashiers",
    },
};

// Global variable to track spike phases per VU
let __VU_PHASE;

// Helper functions to reduce cognitive complexity
function determinePhase(vuCount) {
    if (vuCount <= 10) {
        return "baseline";
    } else if (vuCount <= 200) {
        return "spike1";
    } else {
        return "spike2";
    }
}

function handlePhaseTransition(currentPhase, newPhase, phaseStartTime, vuCount, data) {
    const transitionTime = Date.now() - phaseStartTime;
    console.log(`\n📈 Phase transition: ${currentPhase} → ${newPhase}`);
    console.log(`   Transition time: ${transitionTime}ms`);
    console.log(`   Active VUs: ${vuCount}`);

    if (currentPhase.includes("spike")) {
        recoveryTime.add(transitionTime);
    }

    if (newPhase.includes("spike")) {
        data.spikeTimes.push(new Date().toISOString());
    }
}

function executeCashierOperations(tenantId, currentPhase) {
    let cashierId;
    let cashierVersion;

    // Create cashier - measure spike impact
    const cashierData = generateCashierData();
    const createStartTime = new Date().getTime();

    const createResponse = http.post(endpoints.cashiers.create, JSON.stringify(cashierData), {
        headers: headers.withTenant(tenantId),
        tags: {
            operation: "create_cashier_spike",
            phase: currentPhase,
        },
        timeout: "15s", // Longer timeout for spike conditions
    });

    const createDuration = new Date().getTime() - createStartTime;
    spikeResponseTime.add(createDuration);

    // Track spike-specific metrics
    if (currentPhase.includes("spike")) {
        handleSpikeMetrics(createResponse, createDuration, currentPhase);
        if (createResponse.status === 201) {
            const cashier = parseResponse(createResponse);
            cashierId = cashier.cashierId;
            cashierVersion = cashier.version;
        }
    } else {
        handleBaselineMetrics(createResponse);
        if (createResponse.status === 201) {
            const cashier = parseResponse(createResponse);
            cashierId = cashier.cashierId;
            cashierVersion = cashier.version;
        }
    }

    return { cashierId, cashierVersion };
}

function handleSpikeMetrics(createResponse, createDuration, currentPhase) {
    if (createResponse.status === 201) {
        spikeErrorRate.add(0);
        customMetrics.cashierCreationRate.add(1);

        // Log performance during spike
        if (createDuration > 1000) {
            console.log(`⚠️ SPIKE IMPACT: ${createDuration}ms (VU: ${__VU}, Phase: ${currentPhase})`);
        }
    } else {
        spikeErrorRate.add(1);
        customMetrics.cashierCreationRate.add(0);

        if (createResponse.status === 503) {
            console.error(`🔴 SERVICE OVERLOAD during ${currentPhase} (VU: ${__VU})`);
        } else if (createResponse.status === 429) {
            console.error(`🔴 RATE LIMITED during ${currentPhase} (VU: ${__VU})`);
        } else if (createResponse.status === 0) {
            console.error(`🔴 TIMEOUT during ${currentPhase} (VU: ${__VU})`);
        }
    }
}

function handleBaselineMetrics(createResponse) {
    const success = checkResponse(createResponse, 201, "Create cashier (baseline)");
    customMetrics.cashierCreationRate.add(success ? 1 : 0);
}

function performAdditionalOperations(testResult, tenantId, currentPhase) {
    const { cashierId, cashierVersion } = testResult;

    // Read operations to test cache/connection pool behavior
    const getResponse = http.get(endpoints.cashiers.get(cashierId), {
        headers: headers.withTenant(tenantId),
        tags: {
            operation: "get_cashier_spike",
            phase: currentPhase,
        },
        timeout: "10s",
    });

    if (currentPhase.includes("spike") && getResponse.status !== 200) {
        console.error(`Read failed during spike: ${getResponse.status}`);
    }

    // Update operation during spike
    if (cashierVersion && Math.random() < 0.5) {
        const updateData = {
            name: `Spike Test ${generateCashierData().name}`,
            email: generateCashierData().email,
            version: cashierVersion,
        };

        http.put(endpoints.cashiers.update(cashierId), JSON.stringify(updateData), {
            headers: headers.withTenant(tenantId),
            tags: {
                operation: "update_cashier_spike",
                phase: currentPhase,
            },
            timeout: "10s",
        });
    }

    // Clean up
    if (Math.random() < 0.3) {
        http.del(endpoints.cashiers.delete(cashierId), null, {
            headers: headers.withTenant(tenantId),
            tags: {
                operation: "delete_cashier_spike",
                phase: currentPhase,
            },
            timeout: "5s",
        });
    }
}

function performListOperations(tenantId, currentPhase) {
    if (Math.random() < 0.2) {
        http.get(`${endpoints.cashiers.list}?pageSize=50`, {
            headers: headers.withTenant(tenantId),
            tags: {
                operation: "list_cashiers_spike",
                phase: currentPhase,
            },
            timeout: "10s",
        });
    }
}

// Test setup
export function setup() {
    console.log("⚡ Starting Cashiers SPIKE Test");
    console.log("Environment:", __ENV.ENVIRONMENT || "local");
    console.log("API URL:", endpoints.baseUrl);
    console.log("Spike Pattern: 10 → 200 → 10 → 250 → 0 users");
    console.log("Duration: ~8 minutes");

    // Verify API is accessible
    const healthCheck = http.get(endpoints.health.ready);
    if (healthCheck.status !== 200) {
        throw new Error(`API is not ready: ${healthCheck.status}`);
    }

    console.log("✅ API Health Check Passed - Starting spike test...\n");

    return {
        startTime: new Date().toISOString(),
        spikeTimes: [],
        recoveryMetrics: [],
    };
}

// Main spike test scenario
export default function (data) {
    const tenantId = `spike_tenant_${__VU}`;

    // VU-scoped phase tracking using function-scoped variable
    if (!__VU_PHASE) {
        __VU_PHASE = { currentPhase: "baseline", phaseStartTime: Date.now() };
    }
    let { currentPhase, phaseStartTime } = __VU_PHASE;

    // Determine current phase based on VU count
    const vuCount = __VU;
    const newPhase = determinePhase(vuCount);

    // Log phase transitions
    if (newPhase !== currentPhase) {
        handlePhaseTransition(currentPhase, newPhase, phaseStartTime, vuCount, data);

        // Update VU-scoped phase state
        __VU_PHASE.currentPhase = newPhase;
        __VU_PHASE.phaseStartTime = Date.now();
        currentPhase = newPhase;
    }

    group("Cashier Spike Operations", () => {
        const testResult = executeCashierOperations(tenantId, currentPhase);

        // Additional operations based on test result
        if (testResult.cashierId) {
            performAdditionalOperations(testResult, tenantId, currentPhase);
        }

        // List operations to test query performance under spike
        performListOperations(tenantId, currentPhase);
    });

    // Variable sleep based on phase
    const sleepTime = currentPhase.includes("spike") ? 0.05 : 0.5;
    sleep(sleepTime + Math.random() * sleepTime);
}

// Test teardown
export function teardown(data) {
    console.log("\n" + "=".repeat(50));
    logTestSummary("⚡ Cashiers Spike Test Completed", {
        "Start Time": data.startTime,
        "End Time": new Date().toISOString(),
        Environment: __ENV.ENVIRONMENT || "local",
        "API URL": endpoints.baseUrl,
        "Spike Pattern": "10 → 200 → 10 → 250 → 0 users",
        "Number of Spikes": data.spikeTimes.length.toString(),
        "Test Type": "Spike Test",
    });

    console.log("\n📊 Expected Outcomes:");
    console.log("- System should handle sudden traffic spikes");
    console.log("- Quick recovery to baseline performance");
    console.log("- Connection pools should scale appropriately");
    console.log("- Rate limiting should protect the system");
    console.log("- No memory leaks after spike recovery");
    console.log("=".repeat(50));
}
