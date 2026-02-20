import grpc from "k6/net/grpc";
import { check, group, sleep } from "k6";
import crypto from "k6/crypto";

// Simple test configuration for cashier creation via gRPC
export const options = {
    stages: [
        { duration: "5s", target: 2 }, // Ramp up to 2 users
        { duration: "10s", target: 2 }, // Stay at 2 users
        { duration: "5s", target: 0 }, // Ramp down
    ],
    thresholds: {
        grpc_req_duration: ["p(95)<1000"], // 95% of requests should be below 1s
        checks: ["rate>0.9"], // 90% of checks should pass
    },
};

// Configuration
import { endpoints } from "../../config/endpoints.js";
const GRPC_ENDPOINT = __ENV.GRPC_ENDPOINT || endpoints.grpcEndpoint;

// gRPC client
const client = new grpc.Client();
client.load(["../../protos"], "cashiers.proto");

// Helper function to generate random cashier data
function generateCashierData() {
    const timestamp = Date.now();
    const bytes = crypto.randomBytes(4);
    const rand = new DataView(bytes).getUint32(0) % 1000;

    return {
        name: `Test Cashier ${timestamp}_${rand}`,
        email: `cashier_${timestamp}_${rand}@test.example.com`,
    };
}

export default function main() {
    client.connect(GRPC_ENDPOINT, {
        plaintext: true,
        timeout: "30s",
    });

    group("Create Cashier gRPC Test", () => {
        // Generate test data
        const cashierData = generateCashierData();

        console.log(`Creating cashier via gRPC: ${cashierData.name}`);

        // Create cashier using gRPC
        const response = client.invoke("app_domain.cashiers.CashiersService/CreateCashier", {
            name: cashierData.name,
            email: cashierData.email,
        }, {
            metadata: {
                "X-Tenant-Id": "test-tenant",
            },
        });

        // Check response (remove timing check as it's tracked separately by k6)
        check(response, {
            "Create cashier gRPC - status is OK": (r) => r && r.status === grpc.StatusOK,
            "Create cashier gRPC - has cashierId": (r) => r.message && r.message.cashierId !== undefined && r.message.cashierId !== "",
            "Create cashier gRPC - name matches": (r) => r.message && r.message.name === cashierData.name,
            "Create cashier gRPC - email matches": (r) => r.message && r.message.email === cashierData.email,
            "Create cashier gRPC - has tenantId": (r) => r.message && r.message.tenantId !== undefined && r.message.tenantId !== "",
        });

        // Log result based on gRPC status
        if (response.status === grpc.StatusOK) {
            console.log(`✓ Cashier created successfully via gRPC: ${cashierData.name}`);
            // Log performance warning if response time is slow
            if (response.timings && response.timings.duration >= 500) {
                console.warn(`⚠ Slow response time: ${response.timings.duration}ms`);
            }
        } else {
            console.error(`Create cashier gRPC failed:`, {
                status: response.status,
                message: response.message,
                error: response.error,
            });
        }

        // Small pause between iterations
        sleep(1);
    });

    client.close();
}

// Test teardown
export function teardown() {
    console.log("Simple cashier gRPC creation test completed");
}
