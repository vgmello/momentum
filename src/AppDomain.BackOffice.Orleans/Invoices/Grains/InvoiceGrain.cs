// Copyright (c) ABCDEG. All rights reserved.

namespace AppDomain.BackOffice.Orleans.Invoices.Grains;

public sealed class InvoiceGrain([PersistentState("invoice")] IPersistentState<InvoiceState> state) : Grain, IInvoiceGrain
{
    public Task<InvoiceState> GetState() => Task.FromResult(state.State);

    public async Task Pay(decimal amount)
    {
        state.State.Amount += amount;
        state.State.Paid = true;
        await state.WriteStateAsync();
    }

    public Task Notify(bool important)
    {
        return Task.CompletedTask;
    }
}
