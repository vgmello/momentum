// Copyright (c) OrgName. All rights reserved.

using AppDomain.Cashiers.Queries;
using Momentum.Extensions.Messaging;
using NSubstitute.ExceptionExtensions;
using Wolverine;

namespace AppDomain.Tests.Unit.Cashier;

public class GetCashiersQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnCashiers_WhenQueryIsValid()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var query = new GetCashiersQuery(tenantId, 0, 10);

        var cashierEntities = new List<AppDomain.Cashiers.Data.Entities.Cashier>
        {
            new() { TenantId = tenantId, CashierId = Guid.NewGuid(), Name = "Cashier 1", Email = "cashier1@test.com" },
            new() { TenantId = tenantId, CashierId = Guid.NewGuid(), Name = "Cashier 2", Email = "cashier2@test.com" }
        };

        var expectedResults = cashierEntities.Select(c => new GetCashiersQuery.Result(c.TenantId, c.CashierId, c.Name, c.Email ?? string.Empty, c.CreatedDateUtc, c.UpdatedDateUtc, c.Version))
            .ToList();

        var messagingMock = Substitute.For<IMessageBus>();
        messagingMock.InvokeQueryAsync(
                Arg.Any<GetCashiersQueryHandler.DbQuery>(),
                Arg.Any<CancellationToken>())
            .Returns(cashierEntities);

        // Act
        var result = await GetCashiersQueryHandler.Handle(query, messagingMock, CancellationToken.None);

        // Assert
        result.ShouldBe(expectedResults);
        await messagingMock.Received(1).InvokeQueryAsync(
            Arg.Is<GetCashiersQueryHandler.DbQuery>(dbQuery =>
                dbQuery.TenantId == tenantId &&
                dbQuery.Limit == 10 &&
                dbQuery.Offset == 0),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldUseDefaultLimit_WhenNotSpecified()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var query = new GetCashiersQuery(tenantId); // Use constructor with default limit

        var messagingMock = Substitute.For<IMessageBus>();
        messagingMock.InvokeQueryAsync(
                Arg.Any<GetCashiersQueryHandler.DbQuery>(),
                Arg.Any<CancellationToken>())
            .Returns(new List<AppDomain.Cashiers.Data.Entities.Cashier>());

        // Act
        await GetCashiersQueryHandler.Handle(query, messagingMock, CancellationToken.None);

        // Assert
        await messagingMock.Received(1).InvokeQueryAsync(
            Arg.Is<GetCashiersQueryHandler.DbQuery>(dbQuery =>
                dbQuery.TenantId == tenantId &&
                dbQuery.Limit == 100 &&
                dbQuery.Offset == 0),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyList_WhenNoCashiersFound()
    {
        // Arrange
        var query = new GetCashiersQuery(Guid.NewGuid(), 0, 10);

        var messagingMock = Substitute.For<IMessageBus>();
        messagingMock.InvokeQueryAsync(
                Arg.Any<GetCashiersQueryHandler.DbQuery>(),
                Arg.Any<CancellationToken>())
            .Returns(new List<AppDomain.Cashiers.Data.Entities.Cashier>());

        // Act
        var result = await GetCashiersQueryHandler.Handle(query, messagingMock, CancellationToken.None);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldRespectPagination_WhenOffsetProvided()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var query = new GetCashiersQuery(tenantId, 10, 5);

        var messagingMock = Substitute.For<IMessageBus>();
        messagingMock.InvokeQueryAsync(
                Arg.Any<GetCashiersQueryHandler.DbQuery>(),
                Arg.Any<CancellationToken>())
            .Returns(new List<AppDomain.Cashiers.Data.Entities.Cashier>());

        // Act
        await GetCashiersQueryHandler.Handle(query, messagingMock, CancellationToken.None);

        // Assert
        await messagingMock.Received(1).InvokeQueryAsync(
            Arg.Is<GetCashiersQueryHandler.DbQuery>(dbQuery =>
                dbQuery.TenantId == tenantId &&
                dbQuery.Limit == 5 &&
                dbQuery.Offset == 10),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldPropagateException_WhenDatabaseFails()
    {
        // Arrange
        var query = new GetCashiersQuery(Guid.NewGuid(), 0, 10);

        var messagingMock = Substitute.For<IMessageBus>();
        messagingMock.InvokeQueryAsync(
                Arg.Any<GetCashiersQueryHandler.DbQuery>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Database connection failed"));

        // Act & Assert
        await Should.ThrowAsync<Exception>(async () =>
            await GetCashiersQueryHandler.Handle(query, messagingMock, CancellationToken.None));
    }
}
