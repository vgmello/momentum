// Copyright (c) Momentum .NET. All rights reserved.

using LinqToDB.Mapping;
using Momentum.Extensions.Data.LinqToDb;

namespace Momentum.Extensions.Tests.Data.LinqToDb;

// Test entity classes — defined here, not in src
[Table]
public class Customer
{
    public int Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

[Table(Schema = "app")]
public class Invoice
{
    public int Id { get; set; }

    [Column("custom_column")]
    public string InvoiceNumber { get; set; } = "";
}

[Table("custom_table_name", Schema = "billing")]
public class LineItem
{
    public decimal Amount { get; set; }
}

public abstract class BaseEntity
{
    public int Id { get; set; }
}

[Table(Schema = "main")]
public class Order : BaseEntity
{
    public string OrderNumber { get; set; } = "";
}

public class SnakeCaseNamingConventionMetadataReaderTests
{
    private readonly SnakeCaseNamingConventionMetadataReader _reader = new();

    [Fact]
    public void GetAttributes_ForType_ShouldReturnPluralizedSnakeCaseTableName()
    {
        // Act
        var attributes = _reader.GetAttributes(typeof(Customer));

        // Assert
        var table = attributes.OfType<TableAttribute>().ShouldHaveSingleItem();
        table.Name.ShouldBe("customers");
    }

    [Fact]
    public void GetAttributes_ForTypeWithSchema_ShouldPreserveSchema()
    {
        // Act
        var attributes = _reader.GetAttributes(typeof(Invoice));

        // Assert
        var table = attributes.OfType<TableAttribute>().ShouldHaveSingleItem();
        table.Schema.ShouldBe("app");
    }

    [Fact]
    public void GetAttributes_ForTypeWithExplicitTableName_ShouldPreserveExplicitName()
    {
        // Act
        var attributes = _reader.GetAttributes(typeof(LineItem));

        // Assert
        var table = attributes.OfType<TableAttribute>().ShouldHaveSingleItem();
        table.Name.ShouldBe("custom_table_name");
        table.Schema.ShouldBe("billing");
    }

    [Fact]
    public void GetAttributes_ForAbstractType_ShouldReturnAttributesWithoutTableConvention()
    {
        // Act
        var attributes = _reader.GetAttributes(typeof(BaseEntity));

        // Assert
        attributes.OfType<TableAttribute>().ShouldBeEmpty();
    }

    [Fact]
    public void GetAttributes_ForDerivedType_ShouldInheritSchemaFromBase()
    {
        // Act
        var attributes = _reader.GetAttributes(typeof(Order));

        // Assert
        var table = attributes.OfType<TableAttribute>().ShouldHaveSingleItem();
        table.Name.ShouldBe("orders");
        table.Schema.ShouldBe("main");
    }

    [Fact]
    public void GetAttributes_ForMember_ShouldReturnSnakeCaseColumnName()
    {
        // Arrange
        var member = typeof(Customer).GetProperty(nameof(Customer.FirstName))!;

        // Act
        var attributes = _reader.GetAttributes(typeof(Customer), member);

        // Assert
        var column = attributes.OfType<ColumnAttribute>().ShouldHaveSingleItem();
        column.Name.ShouldBe("first_name");
    }

    [Fact]
    public void GetAttributes_ForMemberWithExplicitColumnName_ShouldPreserveExplicitName()
    {
        // Arrange
        var member = typeof(Invoice).GetProperty(nameof(Invoice.InvoiceNumber))!;

        // Act
        var attributes = _reader.GetAttributes(typeof(Invoice), member);

        // Assert
        var column = attributes.OfType<ColumnAttribute>().ShouldHaveSingleItem();
        column.Name.ShouldBe("custom_column");
    }

    [Fact]
    public void GetAttributes_ForSimplePropertyName_ShouldReturnSnakeCaseColumnName()
    {
        // Arrange
        var member = typeof(Customer).GetProperty(nameof(Customer.CreatedAt))!;

        // Act
        var attributes = _reader.GetAttributes(typeof(Customer), member);

        // Assert
        var column = attributes.OfType<ColumnAttribute>().ShouldHaveSingleItem();
        column.Name.ShouldBe("created_at");
    }

    [Fact]
    public void GetDynamicColumns_ShouldReturnEmptyArray()
    {
        // Act
        var result = _reader.GetDynamicColumns(typeof(Customer));

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public void GetObjectID_ShouldReturnClassName()
    {
        // Act
        var id = _reader.GetObjectID();

        // Assert
        id.ShouldBe(nameof(SnakeCaseNamingConventionMetadataReader));
    }
}
