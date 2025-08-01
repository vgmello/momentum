// Copyright (c) ABCDEG. All rights reserved.

using Orleans.Concurrency;

namespace AppDomain.BackOffice.Orleans.Invoices.Grains;

public interface IInvoiceGrain : IGrainWithGuidKey
{
    Task<InvoiceState> GetState();

    Task Pay(decimal amount);

    [OneWay]
    Task Notify(bool important);
}
