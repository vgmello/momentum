// Copyright (c) ABCDEG. All rights reserved.

using Momentum.Extensions.Abstractions.Dapper;

namespace Momentum.Extensions.SourceGenerators.DbCommand;

public record DbCommandSourceGenSettings(DbParamsCase DbCommandDefaultParamCase, string DbCommandParamPrefix);
