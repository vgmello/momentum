// Copyright (c) OrgName. All rights reserved.

// ReSharper disable CheckNamespace

namespace AppDomain.Common.Grpc;

public partial class DecimalValue
{
    private const decimal NanoFactor = 1_000_000_000;

    public DecimalValue(long units, int nanos)
    {
        Units = units;
        Nanos = nanos;
    }

    public static implicit operator decimal(DecimalValue grpcDecimal) => grpcDecimal.Units + grpcDecimal.Nanos / NanoFactor;

    public static implicit operator DecimalValue(decimal value)
    {
        var units = decimal.Truncate(value);
        var nanosDecimal = (value - units) * NanoFactor;
        var nanos = decimal.ToInt32(decimal.Truncate(nanosDecimal));

        return new DecimalValue(decimal.ToInt64(units), nanos);
    }
}
