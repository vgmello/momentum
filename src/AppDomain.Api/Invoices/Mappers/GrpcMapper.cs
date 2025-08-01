// Copyright (c) ABCDEG. All rights reserved.

using AppDomain.Invoices.Commands;
using AppDomain.Invoices.Grpc;
using AppDomain.Invoices.Grpc.Models;
using AppDomain.Invoices.Queries;
using Google.Protobuf.WellKnownTypes;

namespace AppDomain.Api.Invoices.Mappers;

[Mapper]
public static partial class GrpcMapper
{
    [MapperIgnoreSource(nameof(AppDomain.Invoices.Contracts.Models.Invoice.AmountPaid))]
    [MapperIgnoreSource(nameof(AppDomain.Invoices.Contracts.Models.Invoice.PaymentDate))]
    public static partial Invoice ToGrpc(this AppDomain.Invoices.Contracts.Models.Invoice source);

    public static GetInvoiceQuery ToQuery(this GetInvoiceRequest request, Guid tenantId) => new(tenantId, Guid.Parse(request.Id));

    public static partial GetInvoicesQuery ToQuery(this GetInvoicesRequest request, Guid tenantId);

    public static partial CreateInvoiceCommand ToCommand(this CreateInvoiceRequest request, Guid tenantId);

    public static CancelInvoiceCommand ToCommand(this CancelInvoiceRequest request, Guid tenantId)
    {
        if (!Guid.TryParse(request.InvoiceId, out var invoiceId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid invoice ID format"));

        return new(tenantId, invoiceId, request.Version);
    }

    public static partial MarkInvoiceAsPaidCommand ToCommand(this MarkInvoiceAsPaidRequest request, Guid tenantId);

    // Custom implementation for SimulatePaymentCommand due to default value logic
    public static SimulatePaymentCommand ToCommand(this SimulatePaymentRequest request, Guid tenantId)
        => new(
            tenantId,
            Guid.Parse(request.InvoiceId),
            request.Version,
            (decimal)request.Amount,
            request.Currency,
            request.PaymentMethod ?? "Credit Card",
            request.PaymentReference ?? $"SIM-{Guid.NewGuid():N}"[..8]
        );

    #region Support Mappers

    private static string ToString(string? value) => value ?? string.Empty;

    private static double ToDouble(decimal value) => Convert.ToDouble(value);

    private static decimal ToDecimal(double value) => Convert.ToDecimal(value);

    private static Guid ToGuid(string value) => Guid.Parse(value);

    private static Guid? ToNullableGuid(string? value) => string.IsNullOrEmpty(value) ? null : Guid.Parse(value);

    private static DateTime? ToNullableDateTime(Timestamp? timestamp) => timestamp?.ToDateTime();

    private static Timestamp? ToTimestamp(DateTime? dateTime) => dateTime?.ToUniversalTime().ToTimestamp();

    private static Timestamp ToTimestamp(DateTime dateTime) => dateTime.ToUniversalTime().ToTimestamp();

    #endregion
}
