// Copyright (c) OrgName. All rights reserved.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Major Code Smell", "S1944",
    Justification = "NSubstitute mocks implement multiple interfaces, casts are valid")]

[assembly: SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly",
    Justification = "NSubstitute When/Do pattern intentionally discards ValueTask from mock setup lambdas")]
