import http from "k6/http";
import { check, group, sleep } from "k6";

// Simple test configuration for cashier creation
export const options = {
    stages: [
        { duration: "5s", target: 2 }, // Ramp up to 2 users
        { duration: "10s", target: 2 }, // Stay at 2 users
        { duration: "5s", target: 0 }, // Ramp down
    ],
    thresholds: {
        http_req_failed: ["rate<0.1"], // http errors should be less than 10%
        http_req_duration: ["p(95)<1000"], // 95% of requests should be below 1s
    },
};

// Configuration
import { endpoints } from "../../config/endpoints.js";
const API_BASE_URL = __ENV.API_BASE_URL || endpoints.baseUrl;

// Headers
const headers = {
    "Content-Type": "application/json",
    Accept: "application/json",
    "X-Tenant-Id": "test-tenant",
};

// Helper function to generate random cashier data
function generateCashierData() {
    const timestamp = Date.now();
    const random = Math.floor(Math.random() * 1000);

    return {
        name: `Test Cashier ${timestamp}_${random}`,
        email: `cashier_${timestamp}_${random}@test.example.com`,
    };
}

export default function main() {
    group("Create Cashier Test", () => {
        // Generate test data
        const cashierData = generateCashierData();

        console.log(`Creating cashier: ${cashierData.name}`);

        // Create cashier
        const createResponse = http.post(`${API_BASE_URL}/cashiers`, JSON.stringify(cashierData), { headers });

        // Check response
        const success = check(createResponse, {
            "Create cashier - status is 201": (r) => r.status === 201,
            "Create cashier - response time < 500ms": (r) => r.timings.duration < 500,
            "Create cashier - has cashierId": (r) => {
                try {
                    const body = JSON.parse(r.body);
                    return body.cashierId !== undefined;
                } catch (e) {
                    console.error(`Failed to parse response: ${e}`, r.body);
                    return false;
                }
            },
            "Create cashier - name matches": (r) => {
                try {
                    const body = JSON.parse(r.body);
                    return body.name === cashierData.name;
                } catch (e) {
                    console.warn("Failed to parse response for name check:", e);
                    return false;
                }
            },
            "Create cashier - email matches": (r) => {
                try {
                    const body = JSON.parse(r.body);
                    return body.email === cashierData.email;
                } catch (e) {
                    console.warn("Failed to parse response for email check:", e);
                    return false;
                }
            },
        });

        if (success) {
            console.log(`✓ Cashier created successfully: ${cashierData.name}`);
        } else {
            console.error(`Create cashier failed:`, {
                status: createResponse.status,
                body: createResponse.body,
                error: createResponse.error,
            });
        }

        // Small pause between iterations
        sleep(1);
    });
}

// Test teardown
export function teardown() {
    console.log("Simple cashier creation test completed");
}
