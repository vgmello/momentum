import http from "k6/http";
import { check, group, sleep } from "k6";
import { getOptions } from "../../config/options.js";
import { endpoints, headers } from "../../config/endpoints.js";
import { checkResponse, parseResponse, generateCashierData, customMetrics, logTestSummary } from "../../lib/helpers.js";

// Export test options
export const options = getOptions();

// Test setup
export function setup() {
    console.log("Starting Cashiers baseline performance test");
    console.log("Environment:", __ENV.ENVIRONMENT || "local");
    console.log("API URL:", endpoints.baseUrl);

    // Verify API is accessible
    const healthCheck = http.get(endpoints.health.ready);
    if (healthCheck.status !== 200) {
        throw new Error(`API is not ready: ${healthCheck.status}`);
    }

    return { startTime: new Date().toISOString() };
}

// Main test scenario
export default function main() {
    const tenantId = `tenant_${__VU}_${__ITER}`;

    group("Cashiers CRUD Operations", () => {
        let cashierId;
        let cashierVersion;

        // Create cashier
        group("Create Cashier", () => {
            const cashierData = generateCashierData();
            const createResponse = http.post(endpoints.cashiers.create, JSON.stringify(cashierData), {
                headers: headers.withTenant(tenantId),
                tags: { operation: "create_cashier" },
            });

            const success = checkResponse(createResponse, 201, "Create cashier");
            customMetrics.cashierCreationRate.add(success ? 1 : 0);

            if (success) {
                const cashier = parseResponse(createResponse);
                cashierId = cashier.cashierId;
                cashierVersion = cashier.version;
            }
        });

        // Get cashier by ID
        if (cashierId) {
            group("Get Cashier by ID", () => {
                const getResponse = http.get(endpoints.cashiers.get(cashierId), {
                    headers: headers.withTenant(tenantId),
                    tags: { operation: "get_cashier" },
                });

                checkResponse(getResponse, 200, "Get cashier");
            });
        }

        // List cashiers
        group("List Cashiers", () => {
            const listResponse = http.get(endpoints.cashiers.list, {
                headers: headers.withTenant(tenantId),
                tags: { operation: "list_cashiers" },
            });

            const success = checkResponse(listResponse, 200, "List cashiers");
            if (success) {
                const cashiers = parseResponse(listResponse);
                check(cashiers, {
                    "List contains cashiers": (list) => Array.isArray(list) && list.length > 0,
                });
            }
        });

        // Update cashier
        if (cashierId && cashierVersion) {
            group("Update Cashier", () => {
                const updateData = {
                    name: `Updated ${generateCashierData().name}`,
                    email: generateCashierData().email,
                    version: cashierVersion,
                };

                const updateResponse = http.put(endpoints.cashiers.update(cashierId), JSON.stringify(updateData), {
                    headers: headers.withTenant(tenantId),
                    tags: { operation: "update_cashier" },
                });

                const success = checkResponse(updateResponse, 200, "Update cashier");
                if (success) {
                    const updatedCashier = parseResponse(updateResponse);
                    cashierVersion = updatedCashier.version;
                }
            });
        }

        // Delete cashier
        if (cashierId) {
            group("Delete Cashier", () => {
                const deleteResponse = http.del(endpoints.cashiers.delete(cashierId), null, {
                    headers: headers.withTenant(tenantId),
                    tags: { operation: "delete_cashier" },
                });

                checkResponse(deleteResponse, 204, "Delete cashier");
            });
        }

        // Small pause between iterations
        sleep(0.5);
    });

    // Test search and filtering
    group("Cashiers Search and Filter", () => {
        const queryString = "page=1&pageSize=10&sortBy=name";

        const searchResponse = http.get(`${endpoints.cashiers.list}?${queryString}`, {
            headers: headers.withTenant(tenantId),
            tags: { operation: "search_cashiers" },
        });

        checkResponse(searchResponse, 200, "Search cashiers");
    });
}

// Test teardown
export function teardown(data) {
    logTestSummary("Cashiers Baseline Test", {
        "Start Time": data.startTime,
        "End Time": new Date().toISOString(),
        Environment: __ENV.ENVIRONMENT || "local",
        "API URL": endpoints.baseUrl,
    });
}
