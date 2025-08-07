// Copyright (c) Momentum .NET. All rights reserved.

namespace Momentum.Extensions.Tests;

public class DbCommandAttributeTests
{
    [Theory]
    [InlineData(null, null, null, DbParamsCase.Unset, false, null)]
    [InlineData("test_sp", null, null, DbParamsCase.None, true, "TestDb")]
    [InlineData(null, "SELECT * FROM users", null, DbParamsCase.SnakeCase, false, "ReadOnlyDb")]
    [InlineData("proc_name", "query_text", null, DbParamsCase.Unset, true, null)]
    public void DbCommandAttribute_WithAllParameterCombinations_ShouldSetPropertiesCorrectly(
        string? sp, string? sql, string? fn, DbParamsCase paramsCase, bool nonQuery, string? dataSource)
    {
        // Act
        var attribute = new DbCommandAttribute(sp, sql, fn, paramsCase, nonQuery, dataSource);

        // Assert
        attribute.Sp.ShouldBe(sp);
        attribute.Sql.ShouldBe(sql);
        attribute.Fn.ShouldBe(fn);
        attribute.ParamsCase.ShouldBe(paramsCase);
        attribute.NonQuery.ShouldBe(nonQuery);
        attribute.DataSource.ShouldBe(dataSource);
    }

    [Fact]
    public void DbCommandAttribute_WithSpOnly_ShouldHaveCorrectProperties()
    {
        // Act
        var attribute = new DbCommandAttribute(sp: "create_user");

        // Assert
        attribute.Sp.ShouldBe("create_user");
        attribute.Sql.ShouldBeNull();
        attribute.Fn.ShouldBeNull();
        attribute.ParamsCase.ShouldBe(DbParamsCase.Unset);
        attribute.NonQuery.ShouldBe(false);
        attribute.DataSource.ShouldBeNull();
    }

    [Fact]
    public void DbCommandAttribute_WithSqlOnly_ShouldHaveCorrectProperties()
    {
        // Act
        var attribute = new DbCommandAttribute(sql: "SELECT * FROM users WHERE active = true");

        // Assert
        attribute.Sp.ShouldBeNull();
        attribute.Sql.ShouldBe("SELECT * FROM users WHERE active = true");
        attribute.Fn.ShouldBeNull();
        attribute.ParamsCase.ShouldBe(DbParamsCase.Unset);
        attribute.NonQuery.ShouldBe(false);
        attribute.DataSource.ShouldBeNull();
    }

    [Fact]
    public void DbCommandAttribute_WithFnOnly_ShouldHaveCorrectProperties()
    {
        // Act
        var attribute = new DbCommandAttribute(fn: "select * from AppDomain.invoices_get");

        // Assert
        attribute.Sp.ShouldBeNull();
        attribute.Sql.ShouldBeNull();
        attribute.Fn.ShouldBe("select * from AppDomain.invoices_get");
        attribute.ParamsCase.ShouldBe(DbParamsCase.Unset);
        attribute.NonQuery.ShouldBe(false);
        attribute.DataSource.ShouldBeNull();
    }
}
