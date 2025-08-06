// Copyright (c) ABCDEG. All rights reserved.

using LinqToDB;
using LinqToDB.Mapping;

namespace Momentum.Extensions.Data.LinqToDb;

public static class LinqToDbExtensions
{
    public static DataOptions UseMappingSchema(this DataOptions options, Action<MappingSchema> config)
    {
        var mappingSchema = new MappingSchema();

        config.Invoke(mappingSchema);

        return options.UseMappingSchema(mappingSchema);
    }
}
