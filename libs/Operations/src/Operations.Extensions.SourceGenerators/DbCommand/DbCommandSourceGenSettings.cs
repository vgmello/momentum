// Copyright (c) ABCDEG. All rights reserved.

using Operations.Extensions.Abstractions.Dapper;

namespace Operations.Extensions.SourceGenerators.DbCommand;

public record DbCommandSourceGenSettings(DbParamsCase DbCommandDefaultParamCase, string DbCommandParamPrefix);
