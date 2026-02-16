// Copyright (c) OrgName. All rights reserved.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Performance", "CA1873", Justification = "Logging argument evaluation cost acceptable for grain operation logging")]
[assembly: SuppressMessage("Orleans", "ORLEANS0010", Justification = "Alias attributes not required for internal grain interfaces and state")]
