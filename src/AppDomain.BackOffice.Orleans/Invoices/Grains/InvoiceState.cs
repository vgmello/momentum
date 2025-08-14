// Copyright (c) ORG_NAME. All rights reserved.

namespace AppDomain.BackOffice.Orleans.Invoices.Grains;

[GenerateSerializer]
public sealed class InvoiceState
{
    [Id(0)]
    public decimal Amount { get; set; }

    [Id(1)]
    public bool Paid { get; set; }
}