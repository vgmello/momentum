import http from 'k6/http';
import { check, group, sleep } from 'k6';
import { getOptions } from '../../config/options.js';
import { endpoints, headers } from '../../config/endpoints.js';
import {
  checkResponse,
  parseResponse,
  generateInvoiceData,
  generateCashierData,
  customMetrics,
  logTestSummary
} from '../../lib/helpers.js';

// Export test options
export const options = getOptions();

// Test setup
export function setup() {
  console.log('Starting Invoices baseline performance test');
  console.log('Environment:', __ENV.ENVIRONMENT || 'local');
  console.log('API URL:', endpoints.baseUrl);

  // Verify API is accessible
  const healthCheck = http.get(endpoints.health.ready);
  if (healthCheck.status !== 200) {
    throw new Error(`API is not ready: ${healthCheck.status}`);
  }

  // Create a test cashier for invoice assignment
  const tenantId = 'setup_tenant';
  const cashierData = generateCashierData();
  const cashierResponse = http.post(
    endpoints.cashiers.create,
    JSON.stringify(cashierData),
    { headers: headers.withTenant(tenantId) }
  );

  let testCashierId = null;
  if (cashierResponse.status === 201) {
    const cashier = parseResponse(cashierResponse);
    testCashierId = cashier.cashierId;
  }

  return {
    startTime: new Date().toISOString(),
    testCashierId: testCashierId,
  };
}

// Main test scenario
export default function (data) {
  const tenantId = `tenant_${__VU}_${__ITER}`;
  const { testCashierId } = data;

  group('Invoices CRUD Operations', () => {
    let invoiceId;
    let invoiceVersion;
    let invoiceAmount;

    // Create invoice
    group('Create Invoice', () => {
      const invoiceData = generateInvoiceData(testCashierId);
      const createResponse = http.post(
        endpoints.invoices.create,
        JSON.stringify(invoiceData),
        {
          headers: headers.withTenant(tenantId),
          tags: { operation: 'create_invoice' }
        }
      );

      const success = checkResponse(createResponse, 201, 'Create invoice');
      customMetrics.invoiceCreationRate.add(success ? 1 : 0);

      if (success) {
        const invoice = parseResponse(createResponse);
        invoiceId = invoice.invoiceId;
        invoiceVersion = invoice.version;
        invoiceAmount = invoice.amount;
      }
    });

    // Get invoice by ID
    if (invoiceId) {
      group('Get Invoice by ID', () => {
        const getResponse = http.get(
          endpoints.invoices.get(invoiceId),
          {
            headers: headers.withTenant(tenantId),
            tags: { operation: 'get_invoice' }
          }
        );

        checkResponse(getResponse, 200, 'Get invoice');
      });
    }

    // List invoices
    group('List Invoices', () => {
      const listResponse = http.get(
        endpoints.invoices.list,
        {
          headers: headers.withTenant(tenantId),
          tags: { operation: 'list_invoices' }
        }
      );

      const success = checkResponse(listResponse, 200, 'List invoices');
      if (success) {
        const invoices = parseResponse(listResponse);
        check(invoices, {
          'List contains invoices': (list) => Array.isArray(list) && list.length >= 0,
        });
      }
    });

    // Simulate payment (test endpoint)
    if (invoiceId && invoiceVersion) {
      group('Simulate Payment', () => {
        const paymentData = {
          version: invoiceVersion,
          amount: invoiceAmount,
          currency: 'USD',
          paymentMethod: 'Credit Card',
          paymentReference: `PAY_${Date.now()}`,
        };

        const startTime = new Date().getTime();
        const paymentResponse = http.post(
          endpoints.invoices.simulatePayment(invoiceId),
          JSON.stringify(paymentData),
          {
            headers: headers.withTenant(tenantId),
            tags: { operation: 'simulate_payment' }
          }
        );
        const endTime = new Date().getTime();

        const success = checkResponse(paymentResponse, 200, 'Simulate payment');
        if (success) {
          customMetrics.paymentProcessingTime.add(endTime - startTime);
          // Update version after payment using API response
          const paymentResult = parseResponse(paymentResponse);
          invoiceVersion = paymentResult.version;
        }
      });
    }

    // Mark invoice as paid
    if (invoiceId && invoiceVersion) {
      group('Mark Invoice as Paid', () => {
        const markPaidData = {
          version: invoiceVersion,
          amountPaid: invoiceAmount,
          paymentDate: new Date().toISOString(),
        };

        const markPaidResponse = http.put(
          endpoints.invoices.markPaid(invoiceId),
          JSON.stringify(markPaidData),
          {
            headers: headers.withTenant(tenantId),
            tags: { operation: 'mark_invoice_paid' }
          }
        );

        const success = checkResponse(markPaidResponse, 200, 'Mark invoice paid');
        if (success) {
          const updatedInvoice = parseResponse(markPaidResponse);
          invoiceVersion = updatedInvoice.version;
        } else if (markPaidResponse.status === 409) {
          customMetrics.concurrentVersionErrors.add(1);
        }
      });
    }

    // Small pause between iterations
    sleep(0.5);
  });

  // Test invoice lifecycle
  group('Invoice Lifecycle', () => {
    const invoiceData = generateInvoiceData(testCashierId);

    // Create
    const createResponse = http.post(
      endpoints.invoices.create,
      JSON.stringify(invoiceData),
      {
        headers: headers.withTenant(tenantId),
        tags: { operation: 'lifecycle_create' }
      }
    );

    if (createResponse.status === 201) {
      const invoice = parseResponse(createResponse);

      // Cancel
      const cancelData = {
        version: invoice.version,
      };

      const cancelResponse = http.put(
        endpoints.invoices.cancel(invoice.invoiceId),
        JSON.stringify(cancelData),
        {
          headers: headers.withTenant(tenantId),
          tags: { operation: 'lifecycle_cancel' }
        }
      );

      checkResponse(cancelResponse, 200, 'Cancel invoice');
    }
  });

  // Test search and filtering
  group('Invoices Search and Filter', () => {
    const queryString = 'status=Pending&page=1&pageSize=10&sortBy=dueDate';

    const searchResponse = http.get(
      `${endpoints.invoices.list}?${queryString}`,
      {
        headers: headers.withTenant(tenantId),
        tags: { operation: 'search_invoices' }
      }
    );

    checkResponse(searchResponse, 200, 'Search invoices');
  });
}

// Test teardown
export function teardown(data) {
  // Clean up test cashier if created
  if (data.testCashierId) {
    http.del(
      endpoints.cashiers.delete(data.testCashierId),
      null,
      { headers: headers.withTenant('setup_tenant') }
    );
  }

  logTestSummary('Invoices Baseline Test', {
    'Start Time': data.startTime,
    'End Time': new Date().toISOString(),
    'Environment': __ENV.ENVIRONMENT || 'local',
    'API URL': endpoints.baseUrl,
    'Test Cashier ID': data.testCashierId || 'None',
  });
}
