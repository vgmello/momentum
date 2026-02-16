// Copyright (c) OrgName. All rights reserved.

using AppDomain.Cashiers.Commands;
using AppDomain.Cashiers.Contracts.Models;
using AppDomain.Cashiers.Grpc;
using AppDomain.Cashiers.Queries;
using Google.Protobuf.WellKnownTypes;
using GrpcCashier = AppDomain.Cashiers.Grpc.Models.Cashier;

#pragma warning disable RMG089 // Mapping nullable source to non-nullable target handled by helper methods

namespace AppDomain.Api.Cashiers.Mappers;

/// <summary>
///     Provides mapping methods to transform between gRPC request/response models and domain commands, queries, and entities for cashier
///     operations.
///     Uses source generation to automatically implement mapping logic for partial methods.
/// </summary>
[Mapper]
public static partial class GrpcMapper
{
    /// <summary>
    ///     Transforms a domain cashier entity into a gRPC response model.
    ///     Excludes cashier payments and version information from the mapping.
    /// </summary>
    /// <param name="source">The domain cashier entity to transform.</param>
    /// <returns>A gRPC cashier model for client consumption.</returns>
    [MapperIgnoreSource(nameof(Cashier.CashierPayments))]
    public static partial GrpcCashier ToGrpc(this Cashier source);

    /// <summary>
    ///     Transforms a query result containing cashier data into a gRPC response model.
    /// </summary>
    /// <param name="source">The query result containing cashier information.</param>
    /// <returns>A gRPC cashier model for client consumption.</returns>
    public static partial GrpcCashier ToGrpc(this GetCashiersQuery.Result source);

    /// <summary>
    ///     Transforms a gRPC request to create a cashier into a domain command.
    /// </summary>
    /// <param name="request">The create cashier request from the gRPC client.</param>
    /// <param name="tenantId">The tenant identifier for multi-tenancy support.</param>
    /// <returns>A <see cref="CreateCashierCommand" /> for domain processing.</returns>
    public static partial CreateCashierCommand ToCommand(this CreateCashierRequest request, Guid tenantId);

    /// <summary>
    ///     Transforms a gRPC request to update a cashier into a domain command.
    ///     Excludes the cashier ID from the source mapping as it is provided separately.
    /// </summary>
    /// <param name="request">The update cashier request from the gRPC client.</param>
    /// <param name="tenantId">The tenant identifier for multi-tenancy support.</param>
    /// <param name="cashierId">The unique identifier of the cashier to update.</param>
    /// <returns>An <see cref="UpdateCashierCommand" /> for domain processing.</returns>
    [MapperIgnoreSource(nameof(UpdateCashierRequest.CashierId))]
    public static partial UpdateCashierCommand ToCommand(this UpdateCashierRequest request, Guid tenantId, Guid cashierId);

    /// <summary>
    ///     Transforms a gRPC request to delete a cashier into a domain command.
    /// </summary>
    /// <param name="request">The delete cashier request from the gRPC client.</param>
    /// <param name="tenantId">The tenant identifier for multi-tenancy support.</param>
    /// <returns>A <see cref="DeleteCashierCommand" /> for domain processing.</returns>
    public static DeleteCashierCommand ToCommand(this DeleteCashierRequest request, Guid tenantId)
        => new(tenantId, request.CashierId.ToGuidSafe("Invalid cashier ID format"));

    /// <summary>
    ///     Transforms a gRPC request to retrieve cashiers into a domain query.
    /// </summary>
    /// <param name="request">The get cashiers request from the gRPC client containing filtering and pagination parameters.</param>
    /// <param name="tenantId">The tenant identifier for multi-tenancy support.</param>
    /// <returns>A <see cref="GetCashiersQuery" /> for domain processing.</returns>
    public static partial GetCashiersQuery ToQuery(this GetCashiersRequest request, Guid tenantId);

    /// <summary>
    ///     Transforms a gRPC request to retrieve a single cashier into a domain query.
    /// </summary>
    /// <param name="request">The get cashier request from the gRPC client.</param>
    /// <param name="tenantId">The tenant identifier for multi-tenancy support.</param>
    /// <returns>A <see cref="GetCashierQuery" /> for domain processing.</returns>
    public static GetCashierQuery ToQuery(this GetCashierRequest request, Guid tenantId)
        => new(tenantId, request.Id.ToGuidSafe("Invalid cashier ID format"));

    /// <summary>
    ///     Converts a GUID to its string representation for gRPC serialization.
    /// </summary>
    /// <param name="guid">The GUID to convert.</param>
    /// <returns>The string representation of the GUID.</returns>
    private static string ToString(Guid guid) => guid.ToString();

    /// <summary>
    ///     Converts a DateTime to a Protocol Buffer Timestamp.
    /// </summary>
    private static Timestamp ToTimestamp(DateTime dateTime) => dateTime.ToUniversalTime().ToTimestamp();
}
