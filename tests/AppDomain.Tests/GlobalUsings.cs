// Copyright (c) ORG_NAME. All rights reserved.

global using Dapper;
#if INCLUDE_API
global using Grpc.Core;
global using Timestamp = Google.Protobuf.WellKnownTypes.Timestamp;
#endif
global using NSubstitute;
global using Shouldly;
global using Xunit;
