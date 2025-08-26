// Copyright (c) ORG_NAME. All rights reserved.

using AppDomain.Invoices.Commands;
using AppDomain.Invoices.Grpc;
using AppDomain.Invoices.Grpc.Models;
using AppDomain.Invoices.Queries;
using Google.Protobuf.WellKnownTypes;

namespace AppDomain.Api.Invoices.Mappers;

/// <summary>
///     Provides mapping functionality between gRPC request/response models and domain objects for invoice operations.
///     Handles bidirectional transformations between Protocol Buffer messages and strongly-typed domain models,
///     including type conversions, validation, and default value handling.
/// </summary>
[Mapper]
public static partial class GrpcMapper
{
    /// <summary>
    ///     Converts a domain invoice model to a gRPC response message.
    ///     Excludes AmountPaid and PaymentDate fields as they are not exposed in the gRPC contract.
    /// </summary>
    /// <param name="source">The domain invoice model to convert</param>
    /// <returns>A gRPC Invoice message suitable for API responses</returns>
    [MapperIgnoreSource(nameof(AppDomain.Invoices.Contracts.Models.Invoice.AmountPaid))]
    [MapperIgnoreSource(nameof(AppDomain.Invoices.Contracts.Models.Invoice.PaymentDate))]
    public static partial Invoice ToGrpc(this AppDomain.Invoices.Contracts.Models.Invoice source);

    /// <summary>
    ///     Converts a gRPC request for a single invoice into a domain query.
    ///     Parses the invoice ID from string format and combines with tenant context.
    /// </summary>
    /// <param name="request">The gRPC request containing the invoice ID</param>
    /// <param name="tenantId">The tenant identifier to scope the query</param>
    /// <returns>A query object for retrieving a specific invoice</returns>
    public static GetInvoiceQuery ToQuery(this GetInvoiceRequest request, Guid tenantId) => new(tenantId, Guid.Parse(request.Id));

    /// <summary>
    ///     Converts a gRPC request for multiple invoices into a domain query.
    ///     Maps pagination and filtering parameters from the gRPC request.
    /// </summary>
    /// <param name="request">The gRPC request containing query parameters</param>
    /// <param name="tenantId">The tenant identifier to scope the query results</param>
    /// <returns>A query object for retrieving multiple invoices</returns>
    public static partial GetInvoicesQuery ToQuery(this GetInvoicesRequest request, Guid tenantId);

    /// <summary>
    ///     Converts a gRPC invoice creation request into a domain command.
    ///     Maps all invoice fields from Protocol Buffer format to strongly-typed command object.
    /// </summary>
    /// <param name="request">The gRPC request containing invoice creation data</param>
    /// <param name="tenantId">The tenant identifier for the new invoice</param>
    /// <returns>A command object for creating an invoice</returns>
    public static partial CreateInvoiceCommand ToCommand(this CreateInvoiceRequest request, Guid tenantId);

    /// <summary>
    ///     Converts a gRPC invoice cancellation request into a domain command.
    ///     Validates the invoice ID format and throws an RpcException if invalid.
    /// </summary>
    /// <param name="request">The gRPC request containing invoice ID and version</param>
    /// <param name="tenantId">The tenant identifier for authorization</param>
    /// <returns>A command object for cancelling an invoice</returns>
    /// <exception cref="RpcException">Thrown when the invoice ID format is invalid</exception>
    public static CancelInvoiceCommand ToCommand(this CancelInvoiceRequest request, Guid tenantId)
    {
        if (!Guid.TryParse(request.InvoiceId, out var invoiceId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid invoice ID format"));

        return new(tenantId, invoiceId, request.Version);
    }

    /// <summary>
    ///     Converts a gRPC request to mark an invoice as paid into a domain command.
    ///     Maps payment details and invoice reference information.
    /// </summary>
    /// <param name="request">The gRPC request containing payment information</param>
    /// <param name="tenantId">The tenant identifier for authorization</param>
    /// <returns>A command object for marking an invoice as paid</returns>
    public static partial MarkInvoiceAsPaidCommand ToCommand(this MarkInvoiceAsPaidRequest request, Guid tenantId);

    /// <summary>
    ///     Converts a gRPC payment simulation request into a domain command.
    ///     Applies default values for optional fields: payment method defaults to "Credit Card",
    ///     and payment reference defaults to a generated "SIM-" prefixed identifier.
    /// </summary>
    /// <param name="request">The gRPC request containing simulation parameters</param>
    /// <param name="tenantId">The tenant identifier for authorization</param>
    /// <returns>A command object for simulating a payment</returns>
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

    /// <summary>
    ///     Converts a nullable string to a non-null string, replacing null with empty string.
    ///     Used for gRPC string field mappings where null values are not allowed.
    /// </summary>
    /// <param name="value">The nullable string value to convert</param>
    /// <returns>The original string or empty string if null</returns>
    private static string ToString(string? value) => value ?? string.Empty;

    /// <summary>
    ///     Converts a decimal value to double precision for gRPC Protocol Buffer compatibility.
    ///     Used for monetary amounts in gRPC message fields.
    /// </summary>
    /// <param name="value">The decimal value to convert</param>
    /// <returns>The equivalent double value</returns>
    private static double ToDouble(decimal value) => Convert.ToDouble(value);

    /// <summary>
    ///     Converts a double value to decimal precision for domain model compatibility.
    ///     Used when receiving monetary amounts from gRPC Protocol Buffer messages.
    /// </summary>
    /// <param name="value">The double value to convert</param>
    /// <returns>The equivalent decimal value</returns>
    private static decimal ToDecimal(double value) => Convert.ToDecimal(value);

    /// <summary>
    ///     Parses a string representation of a GUID into a Guid object.
    ///     Used for converting string-based identifiers from gRPC to strongly-typed domain identifiers.
    /// </summary>
    /// <param name="value">The string representation of the GUID</param>
    /// <returns>The parsed Guid object</returns>
    /// <exception cref="FormatException">Thrown when the string is not a valid GUID format</exception>
    private static Guid ToGuid(string value) => Guid.Parse(value);

    /// <summary>
    ///     Parses a nullable string representation of a GUID into a nullable Guid object.
    ///     Returns null for null or empty strings, otherwise parses the GUID.
    /// </summary>
    /// <param name="value">The nullable string representation of the GUID</param>
    /// <returns>The parsed Guid object or null if the input is null/empty</returns>
    /// <exception cref="FormatException">Thrown when the string is not a valid GUID format</exception>
    private static Guid? ToNullableGuid(string? value) => string.IsNullOrEmpty(value) ? null : Guid.Parse(value);

    /// <summary>
    ///     Converts a Protocol Buffer Timestamp to a nullable DateTime.
    ///     Handles null timestamps by returning null, otherwise converts to DateTime.
    /// </summary>
    /// <param name="timestamp">The Protocol Buffer timestamp to convert</param>
    /// <returns>The equivalent DateTime or null if the timestamp is null</returns>
    private static DateTime? ToNullableDateTime(Timestamp? timestamp) => timestamp?.ToDateTime();

    /// <summary>
    ///     Converts a nullable DateTime to a Protocol Buffer Timestamp.
    ///     Handles null DateTime by returning null, otherwise converts to UTC and creates a Timestamp.
    /// </summary>
    /// <param name="dateTime">The nullable DateTime to convert</param>
    /// <returns>The equivalent Protocol Buffer Timestamp or null if the DateTime is null</returns>
    private static Timestamp? ToTimestamp(DateTime? dateTime) => dateTime?.ToUniversalTime().ToTimestamp();

    /// <summary>
    ///     Converts a DateTime to a Protocol Buffer Timestamp.
    ///     Always converts the DateTime to UTC before creating the Timestamp.
    /// </summary>
    /// <param name="dateTime">The DateTime to convert</param>
    /// <returns>The equivalent Protocol Buffer Timestamp in UTC</returns>
    private static Timestamp ToTimestamp(DateTime dateTime) => dateTime.ToUniversalTime().ToTimestamp();

    #endregion
}
