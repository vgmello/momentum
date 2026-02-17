import { check, sleep } from "k6";
import { Rate, Trend } from "k6/metrics";
import crypto from "k6/crypto";

// Custom metrics
export const customMetrics = {
    cashierCreationRate: new Rate("cashier_creation_success"),
    invoiceCreationRate: new Rate("invoice_creation_success"),
    paymentProcessingTime: new Trend("payment_processing_time"),
    concurrentVersionErrors: new Rate("concurrent_version_errors"),
};

// Cryptographically secure random number [0, 1) using k6/crypto
export function random() {
    const bytes = crypto.randomBytes(4);
    const view = new DataView(bytes);
    return view.getUint32(0) / (0xffffffff + 1);
}

// Cryptographically secure random integer [0, max)
export function randomInt(max) {
    return Math.floor(random() * max);
}

// Helper function to generate random data
export function generateRandomString(length = 10) {
    const characters = "abcdefghijklmnopqrstuvwxyz0123456789";
    let result = "";
    for (let i = 0; i < length; i++) {
        result += characters.charAt(randomInt(characters.length));
    }
    return result;
}

// Generate random email
export function generateEmail() {
    return `user_${generateRandomString(8)}@test.example.com`;
}

// Generate random name
export function generateName() {
    const firstNames = ["John", "Jane", "Bob", "Alice", "Charlie", "Diana", "Eve", "Frank"];
    const lastNames = ["Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller"];
    return `${firstNames[randomInt(firstNames.length)]} ${lastNames[randomInt(lastNames.length)]}`;
}

// Generate random amount
export function generateAmount(min = 10, max = 1000) {
    return Math.floor(random() * (max - min + 1)) + min;
}

// Generate future date
export function generateFutureDate(daysAhead = 30) {
    const date = new Date();
    date.setDate(date.getDate() + randomInt(daysAhead) + 1);
    return date.toISOString();
}

// Check response and log errors
export function checkResponse(response, expectedStatus = 200, testName = "API call", responseTimeThreshold = 500) {
    const success = check(response, {
        [`${testName} - status is ${expectedStatus}`]: (r) => r.status === expectedStatus,
        [`${testName} - response time < ${responseTimeThreshold}ms`]: (r) => r.timings.duration < responseTimeThreshold,
    });

    if (!success) {
        console.error(`${testName} failed:`, {
            status: response.status,
            body: response.body,
            error: response.error,
        });
    }

    return success;
}

// Parse JSON response safely
export function parseResponse(response) {
    try {
        return JSON.parse(response.body);
    } catch (e) {
        console.error(`Failed to parse response: ${response.body}`, e);
        return null;
    }
}

// Retry logic for transient failures
export function retryRequest(fn, maxRetries = 3, delay = 1000) {
    let lastError;

    for (let i = 0; i < maxRetries; i++) {
        try {
            const result = fn();
            if (result.status < 500) {
                return result;
            }
            lastError = result;
        } catch (e) {
            lastError = e;
        }

        if (i < maxRetries - 1) {
            sleep(delay / 1000);
            delay *= 2; // Exponential backoff
        }
    }

    console.error("Request failed after retries:", lastError);
    return lastError;
}

// Generate test data for cashier
export function generateCashierData() {
    return {
        name: generateName(),
        email: generateEmail(),
    };
}

// Generate test data for invoice
export function generateInvoiceData(cashierId = null) {
    const data = {
        name: `Invoice ${generateRandomString(6)}`,
        amount: generateAmount(50, 5000),
        currency: "USD",
        dueDate: generateFutureDate(30),
    };

    if (cashierId) {
        data.cashierId = cashierId;
    }

    return data;
}

// Log test summary
export function logTestSummary(testName, data) {
    console.log(`\n=== ${testName} Summary ===`);
    Object.entries(data).forEach(([key, value]) => {
        console.log(`${key}: ${value}`);
    });
    console.log("========================\n");
}

// Validate UUID format
export function isValidUUID(uuid) {
    const uuidRegex = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
    return uuidRegex.test(uuid);
}

// Calculate percentile from array of numbers
export function percentile(arr, p) {
    const sorted = arr.slice().sort((a, b) => a - b);
    const index = Math.ceil((sorted.length * p) / 100) - 1;
    return sorted[index];
}

// Format bytes to human readable
export function formatBytes(bytes, decimals = 2) {
    if (bytes === 0) return "0 Bytes";

    const k = 1024;
    const dm = Math.max(0, decimals);
    const sizes = ["Bytes", "KB", "MB", "GB"];

    const i = Math.floor(Math.log(bytes) / Math.log(k));

    return Number.parseFloat((bytes / Math.pow(k, i)).toFixed(dm)) + " " + sizes[i];
}
