// Copyright (c) ABCDEG. All rights reserved.

using System.Text.Json.Serialization;

namespace AppDomain.Api.Invoices.Models;

public record CreateInvoiceRequest(
    string Name,
    [property: JsonRequired] decimal Amount,
    string? Currency = "",
    DateTime? DueDate = null,
    Guid? CashierId = null
);
