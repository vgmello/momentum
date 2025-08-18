// Copyright (c) ORG_NAME. All rights reserved.

using System.Text.Json.Serialization;

namespace AppDomain.Api.Invoices.Models;

public record CancelInvoiceRequest([property: JsonRequired] int Version);
