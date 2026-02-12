// Copyright (c) Momentum .NET. All rights reserved.

using System.Data;
using System.Data.Common;
using Dapper;
using Momentum.Extensions.Abstractions.Dapper;
using Momentum.Extensions.Data;
using NSubstitute;

namespace Momentum.Extensions.Tests.Data;

public class DbDataSourceExtensionsTests
{
    private readonly DbDataSource _dataSource = Substitute.For<DbDataSource>();
    private readonly DbConnection _connection = Substitute.For<DbConnection>();

    public DbDataSourceExtensionsTests()
    {
        _dataSource.OpenConnectionAsync(Arg.Any<CancellationToken>())
            .Returns(_connection);
    }

    [Fact]
    public async Task SpCall_ShouldOpenConnection_AndInvokeDbFunction()
    {
        // Arrange
        var parameters = Substitute.For<IDbParamsProvider>();
        parameters.ToDbParams().Returns(new { Id = 1 });

        CommandDefinition capturedCommand = default;
        Func<DbConnection, Func<CommandDefinition, Task<int>>> dbFunction = _ => cmd =>
        {
            capturedCommand = cmd;
            return Task.FromResult(42);
        };

        // Act
        var result = await _dataSource.SpCall("test_sp", parameters, dbFunction,
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBe(42);
        await _dataSource.Received(1).OpenConnectionAsync(Arg.Any<CancellationToken>());
        capturedCommand.CommandText.ShouldBe("test_sp");
        capturedCommand.CommandType.ShouldBe(CommandType.StoredProcedure);
    }

    [Fact]
    public async Task SpCall_WithTransaction_ShouldPassTransactionToCommandDefinition()
    {
        // Arrange
        var parameters = Substitute.For<IDbParamsProvider>();
        parameters.ToDbParams().Returns(new { });
        var transaction = Substitute.For<DbTransaction>();

        CommandDefinition capturedCommand = default;
        Func<DbConnection, Func<CommandDefinition, Task<int>>> dbFunction = _ => cmd =>
        {
            capturedCommand = cmd;
            return Task.FromResult(1);
        };

        // Act
        await _dataSource.SpCall("sp", parameters, dbFunction, transaction: transaction,
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        capturedCommand.Transaction.ShouldBe(transaction);
    }

    [Fact]
    public async Task SpCall_WithCommandTimeout_ShouldPassTimeoutToCommandDefinition()
    {
        // Arrange
        var parameters = Substitute.For<IDbParamsProvider>();
        parameters.ToDbParams().Returns(new { });

        CommandDefinition capturedCommand = default;
        Func<DbConnection, Func<CommandDefinition, Task<int>>> dbFunction = _ => cmd =>
        {
            capturedCommand = cmd;
            return Task.FromResult(1);
        };

        // Act
        await _dataSource.SpCall("sp", parameters, dbFunction, commandTimeout: 60,
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        capturedCommand.CommandTimeout.ShouldBe(60);
    }

    [Fact]
    public async Task SpCall_WithCancellationToken_ShouldPassTokenToCommandDefinition()
    {
        // Arrange
        var parameters = Substitute.For<IDbParamsProvider>();
        parameters.ToDbParams().Returns(new { });
        using var cts = new CancellationTokenSource();

        CommandDefinition capturedCommand = default;
        Func<DbConnection, Func<CommandDefinition, Task<int>>> dbFunction = _ => cmd =>
        {
            capturedCommand = cmd;
            return Task.FromResult(1);
        };

        // Act
        await _dataSource.SpCall("sp", parameters, dbFunction, cancellationToken: cts.Token);

        // Assert
        capturedCommand.CancellationToken.ShouldBe(cts.Token);
    }

    [Fact]
    public async Task SpCall_WithoutOptionalParams_ShouldHaveNullTransactionAndDefaultTimeout()
    {
        // Arrange
        var parameters = Substitute.For<IDbParamsProvider>();
        parameters.ToDbParams().Returns(new { });

        CommandDefinition capturedCommand = default;
        Func<DbConnection, Func<CommandDefinition, Task<int>>> dbFunction = _ => cmd =>
        {
            capturedCommand = cmd;
            return Task.FromResult(1);
        };

        // Act
        await _dataSource.SpCall("sp", parameters, dbFunction,
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        capturedCommand.Transaction.ShouldBeNull();
        capturedCommand.CommandTimeout.ShouldBeNull();
    }
}
