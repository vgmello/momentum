import http from "k6/http";
import { group, sleep } from "k6";
import { getOptions } from "../../config/options.js";
import { endpoints, headers } from "../../config/endpoints.js";
import {
    checkResponse,
    parseResponse,
    generateCashierData,
    generateInvoiceData,
    customMetrics,
    logTestSummary,
} from "../../lib/helpers.js";

// Configuration constants
const INVOICE_PAY_PROBABILITY = 0.3; // 30% chance to immediately pay the invoice

// Export test options
export const options = getOptions();

// Test setup
export function setup() {
    console.log("Starting Mixed Realistic Workflow performance test");
    console.log("Environment:", __ENV.ENVIRONMENT || "local");
    console.log("API URL:", endpoints.baseUrl);

    // Verify API is accessible
    const healthCheck = http.get(endpoints.health.ready);
    if (healthCheck.status !== 200) {
        throw new Error(`API is not ready: ${healthCheck.status}`);
    }

    return {
        startTime: new Date().toISOString(),
        testCashiers: [],
    };
}

// Main test scenario - Realistic workflow
export default function (data) {
    const tenantId = `tenant_${__VU}`;

    // Simulate different user behaviors based on VU number
    const userBehavior = __VU % 4;

    switch (userBehavior) {
        case 0:
            // Power user: Creates multiple invoices and manages cashiers
            powerUserWorkflow(tenantId, data);
            break;
        case 1:
            // Regular user: Creates and pays invoices
            regularUserWorkflow(tenantId, data);
            break;
        case 2:
            // Admin user: Manages cashiers
            adminUserWorkflow(tenantId, data);
            break;
        case 3:
            // Read-only user: Views data
            readOnlyUserWorkflow(tenantId, data);
            break;
    }

    // Random think time between actions (1-3 seconds)
    sleep(1 + Math.random() * 2);
}

// Power user workflow
function powerUserWorkflow(tenantId, data) {
    group("Power User Workflow", () => {
        let cashierId;
        let cashierVersion;

        // Create a cashier
        group("Create Cashier", () => {
            const cashierData = generateCashierData();
            const createResponse = http.post(endpoints.cashiers.create, JSON.stringify(cashierData), {
                headers: headers.withTenant(tenantId),
                tags: { operation: "create_cashier", workflow: "power_user" },
            });

            if (checkResponse(createResponse, 201, "Create cashier")) {
                const cashier = parseResponse(createResponse);
                cashierId = cashier.cashierId;
                cashierVersion = cashier.version;
                customMetrics.cashierCreationRate.add(1);
            }
        });

        // Create multiple invoices
        if (cashierId) {
            group("Create Multiple Invoices", () => {
                const numInvoices = 3 + Math.floor(Math.random() * 3); // 3-5 invoices

                for (let i = 0; i < numInvoices; i++) {
                    const invoiceData = generateInvoiceData(cashierId);
                    const createResponse = http.post(endpoints.invoices.create, JSON.stringify(invoiceData), {
                        headers: headers.withTenant(tenantId),
                        tags: { operation: "create_invoice", workflow: "power_user" },
                    });

                    if (checkResponse(createResponse, 201, `Create invoice ${i + 1}`)) {
                        const invoice = parseResponse(createResponse);
                        customMetrics.invoiceCreationRate.add(1);

                        // 30% chance to immediately pay the invoice
                        if (Math.random() < INVOICE_PAY_PROBABILITY) {
                            const paymentData = {
                                version: invoice.version,
                                amountPaid: invoice.amount,
                                paymentDate: new Date().toISOString(),
                            };

                            http.put(endpoints.invoices.markPaid(invoice.invoiceId), JSON.stringify(paymentData), {
                                headers: headers.withTenant(tenantId),
                                tags: { operation: "mark_paid", workflow: "power_user" },
                            });
                        }
                    }

                    sleep(0.2); // Small delay between invoice creations
                }
            });
        }

        // Update cashier information
        if (cashierId && cashierVersion) {
            group("Update Cashier", () => {
                const updateData = {
                    name: `Updated ${generateCashierData().name}`,
                    email: generateCashierData().email,
                    version: cashierVersion,
                };

                http.put(endpoints.cashiers.update(cashierId), JSON.stringify(updateData), {
                    headers: headers.withTenant(tenantId),
                    tags: { operation: "update_cashier", workflow: "power_user" },
                });
            });
        }
    });
}

// Regular user workflow
function regularUserWorkflow(tenantId, data) {
    group("Regular User Workflow", () => {
        // List available cashiers
        const cashiersResponse = http.get(endpoints.cashiers.list, {
            headers: headers.withTenant(tenantId),
            tags: { operation: "list_cashiers", workflow: "regular_user" },
        });

        let selectedCashierId = null;
        if (cashiersResponse.status === 200) {
            const cashiers = parseResponse(cashiersResponse);
            if (cashiers && cashiers.length > 0) {
                selectedCashierId = cashiers[Math.floor(Math.random() * cashiers.length)].cashierId;
            }
        }

        // Create an invoice
        group("Create and Pay Invoice", () => {
            const invoiceData = generateInvoiceData(selectedCashierId);
            const createResponse = http.post(endpoints.invoices.create, JSON.stringify(invoiceData), {
                headers: headers.withTenant(tenantId),
                tags: { operation: "create_invoice", workflow: "regular_user" },
            });

            if (checkResponse(createResponse, 201, "Create invoice")) {
                const invoice = parseResponse(createResponse);
                customMetrics.invoiceCreationRate.add(1);

                // Simulate payment after a delay
                sleep(1);

                const paymentData = {
                    version: invoice.version,
                    amountPaid: invoice.amount,
                    currency: "USD",
                    paymentMethod: "Credit Card",
                    paymentReference: `PAY_${Date.now()}`,
                };

                const paymentResponse = http.post(endpoints.invoices.simulatePayment(invoice.invoiceId), JSON.stringify(paymentData), {
                    headers: headers.withTenant(tenantId),
                    tags: { operation: "simulate_payment", workflow: "regular_user" },
                });

                checkResponse(paymentResponse, 200, "Simulate payment");
            }
        });

        // Check invoice status
        group("Check Invoice Status", () => {
            const invoicesResponse = http.get(`${endpoints.invoices.list}?status=Pending`, {
                headers: headers.withTenant(tenantId),
                tags: { operation: "list_pending_invoices", workflow: "regular_user" },
            });

            checkResponse(invoicesResponse, 200, "List pending invoices");
        });
    });
}

// Admin user workflow
function adminUserWorkflow(tenantId, data) {
    group("Admin User Workflow", () => {
        // Manage cashiers
        group("Cashier Management", () => {
            // Create new cashier
            const cashierData = generateCashierData();
            const createResponse = http.post(endpoints.cashiers.create, JSON.stringify(cashierData), {
                headers: headers.withTenant(tenantId),
                tags: { operation: "create_cashier", workflow: "admin_user" },
            });

            let cashierId;
            if (checkResponse(createResponse, 201, "Create cashier")) {
                const cashier = parseResponse(createResponse);
                cashierId = cashier.cashierId;
                customMetrics.cashierCreationRate.add(1);
            }

            // List all cashiers
            const listResponse = http.get(endpoints.cashiers.list, {
                headers: headers.withTenant(tenantId),
                tags: { operation: "list_cashiers", workflow: "admin_user" },
            });

            checkResponse(listResponse, 200, "List cashiers");

            // Update cashier if created
            if (cashierId) {
                sleep(0.5);

                const getResponse = http.get(endpoints.cashiers.get(cashierId), {
                    headers: headers.withTenant(tenantId),
                    tags: { operation: "get_cashier", workflow: "admin_user" },
                });

                if (getResponse.status === 200) {
                    const cashier = parseResponse(getResponse);
                    const updateData = {
                        name: `Admin Updated ${cashier.name}`,
                        email: cashier.email,
                        version: cashier.version,
                    };

                    http.put(endpoints.cashiers.update(cashierId), JSON.stringify(updateData), {
                        headers: headers.withTenant(tenantId),
                        tags: { operation: "update_cashier", workflow: "admin_user" },
                    });
                }
            }
        });

        // Review invoices
        group("Invoice Review", () => {
            // Get overdue invoices
            const today = new Date().toISOString().split("T")[0];
            const overdueResponse = http.get(`${endpoints.invoices.list}?status=Pending&dueBefore=${today}`, {
                headers: headers.withTenant(tenantId),
                tags: { operation: "list_overdue_invoices", workflow: "admin_user" },
            });

            checkResponse(overdueResponse, 200, "List overdue invoices");
        });
    });
}

// Read-only user workflow
function readOnlyUserWorkflow(tenantId, data) {
    group("Read-Only User Workflow", () => {
        // View cashiers
        group("View Cashiers", () => {
            const listResponse = http.get(endpoints.cashiers.list, {
                headers: headers.withTenant(tenantId),
                tags: { operation: "list_cashiers", workflow: "read_only" },
            });

            if (checkResponse(listResponse, 200, "List cashiers")) {
                const cashiers = parseResponse(listResponse);
                if (cashiers && cashiers.length > 0) {
                    // View details of a random cashier
                    const randomCashier = cashiers[Math.floor(Math.random() * cashiers.length)];
                    http.get(endpoints.cashiers.get(randomCashier.cashierId), {
                        headers: headers.withTenant(tenantId),
                        tags: { operation: "get_cashier", workflow: "read_only" },
                    });
                }
            }
        });

        // View invoices
        group("View Invoices", () => {
            // List all invoices
            const listResponse = http.get(endpoints.invoices.list, {
                headers: headers.withTenant(tenantId),
                tags: { operation: "list_invoices", workflow: "read_only" },
            });

            if (checkResponse(listResponse, 200, "List invoices")) {
                const invoices = parseResponse(listResponse);
                if (invoices && invoices.length > 0) {
                    // View details of a random invoice
                    const randomInvoice = invoices[Math.floor(Math.random() * invoices.length)];
                    http.get(endpoints.invoices.get(randomInvoice.invoiceId), {
                        headers: headers.withTenant(tenantId),
                        tags: { operation: "get_invoice", workflow: "read_only" },
                    });
                }
            }

            // Check different invoice statuses
            const statuses = ["Pending", "Paid", "Cancelled"];
            const randomStatus = statuses[Math.floor(Math.random() * statuses.length)];

            http.get(`${endpoints.invoices.list}?status=${randomStatus}`, {
                headers: headers.withTenant(tenantId),
                tags: { operation: `list_${randomStatus.toLowerCase()}_invoices`, workflow: "read_only" },
            });
        });
    });
}

// Test teardown
export function teardown(data) {
    logTestSummary("Mixed Realistic Workflow Test", {
        "Start Time": data.startTime,
        "End Time": new Date().toISOString(),
        Environment: __ENV.ENVIRONMENT || "local",
        "API URL": endpoints.baseUrl,
    });
}
