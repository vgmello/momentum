// Copyright (c) Momentum .NET. All rights reserved.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Major Code Smell", "S3011",
    Justification = "Reflection access to non-public members required for Wolverine routing setup")]

[assembly: SuppressMessage("Performance", "CA1873",
    Justification = "Logging argument evaluation cost acceptable for startup-time configuration logging")]

[assembly: SuppressMessage("Major Code Smell", "S8969",
    Justification = "CloudEventMapper uses a null-forgiving operator on the Kafka message to bridge a " +
                    "benign key-annotation variance (string vs string?) between Wolverine's IIncomingMapper " +
                    "and CloudNative's IsCloudEvent; without it the compiler reports CS8620.")]
