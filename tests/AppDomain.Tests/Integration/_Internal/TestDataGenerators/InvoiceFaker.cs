// Copyright (c) OrgName. All rights reserved.

using AppDomain.Invoices.Grpc;
using Bogus;

namespace AppDomain.Tests.Integration._Internal.TestDataGenerators;

public sealed class InvoiceFaker : Faker<CreateInvoiceRequest>
{
    public InvoiceFaker(string? cashierId = null)
    {
        RuleFor(i => i.Name, f => f.Commerce.ProductName() + " Invoice");
        RuleFor(i => i.Amount, f => Math.Round(f.Random.Double(10, 10000), 2));
        RuleFor(i => i.Currency, f => f.PickRandom("USD", "EUR", "GBP", "JPY", "CAD", "AUD"));
        RuleFor(i => i.DueDate, f => Timestamp.FromDateTime(f.Date.Future(6, DateTime.UtcNow).ToUniversalTime()));
        RuleFor(i => i.CashierId, f => cashierId ?? (f.Random.Bool(0.3f) ? f.Random.Guid().ToString() : string.Empty));
    }

    public InvoiceFaker WithCashier(string cashierId)
    {
        RuleFor(i => i.CashierId, cashierId);

        return this;
    }

    public InvoiceFaker WithAmount(double amount)
    {
        RuleFor(i => i.Amount, amount);

        return this;
    }

    public InvoiceFaker WithCurrency(string currency)
    {
        RuleFor(i => i.Currency, currency);

        return this;
    }

    public InvoiceFaker WithoutDueDate()
    {
        RuleFor(i => i.DueDate, (Timestamp?)null);

        return this;
    }

    public InvoiceFaker WithDueDate(DateTime dueDate)
    {
        RuleFor(i => i.DueDate, Timestamp.FromDateTime(dueDate.ToUniversalTime()));

        return this;
    }
}

public sealed class SimulatePaymentFaker : Faker<SimulatePaymentRequest>
{
    public SimulatePaymentFaker(string invoiceId)
    {
        RuleFor(p => p.InvoiceId, invoiceId);
        RuleFor(p => p.PaymentMethod, f => f.PickRandom("credit_card", "debit_card", "bank_transfer", "cash", "paypal"));
        RuleFor(p => p.PaymentReference, f => f.Random.AlphaNumeric(12).ToUpper());
        RuleFor(p => p.Amount, f => Math.Round(f.Random.Double(10, 10000), 2));
    }

    public SimulatePaymentFaker WithAmount(double amount)
    {
        RuleFor(p => p.Amount, amount);

        return this;
    }

    public SimulatePaymentFaker WithPaymentMethod(string method)
    {
        RuleFor(p => p.PaymentMethod, method);

        return this;
    }
}

public sealed class MarkInvoiceAsPaidFaker : Faker<MarkInvoiceAsPaidRequest>
{
    public MarkInvoiceAsPaidFaker(string invoiceId)
    {
        RuleFor(p => p.InvoiceId, invoiceId);
        RuleFor(p => p.AmountPaid, f => Math.Round(f.Random.Double(10, 10000), 2));
        RuleFor(p => p.PaymentDate, f => Timestamp.FromDateTime(f.Date.Recent(7).ToUniversalTime()));
    }
}

public sealed class CancelInvoiceFaker : Faker<CancelInvoiceRequest>
{
    public CancelInvoiceFaker(string invoiceId)
    {
        RuleFor(c => c.InvoiceId, invoiceId);
        // CancelInvoiceRequest doesn't have a Reason field in the proto definition
        // Only InvoiceId and Version are required
    }
}
