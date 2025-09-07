// Copyright (c) OrgName. All rights reserved.

namespace AppDomain.Common.Grpc;

public partial class DecimalValue
{
    private const decimal NanoFactor = 1_000_000_000m;

    public static implicit operator decimal(DecimalValue? grpcDecimal)
        => grpcDecimal is null ? 0m : grpcDecimal.Units + grpcDecimal.Nanos / NanoFactor;

    public static implicit operator DecimalValue(decimal value)
    {
        var units = decimal.ToInt64(decimal.Truncate(value));
        var nanos = decimal.ToInt32((value - units) * NanoFactor);

        return new DecimalValue { Units = units, Nanos = nanos };
    }
}
