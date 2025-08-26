// Copyright (c) ORG_NAME. All rights reserved.

using AppDomain.Cashiers.Grpc;
using Bogus;

namespace AppDomain.Tests.Integration._Internal.TestDataGenerators;

public sealed class CashierFaker : Faker<CreateCashierRequest>
{
    public CashierFaker()
    {
        RuleFor(c => c.Name, f => f.Person.FullName);
        RuleFor(c => c.Email, f => f.Person.Email);
    }
}

public sealed class UpdateCashierFaker : Faker<UpdateCashierRequest>
{
    public UpdateCashierFaker(string? cashierId = null)
    {
        RuleFor(c => c.CashierId, f => cashierId ?? f.Random.Guid().ToString());
        RuleFor(c => c.Name, f => f.Person.FullName);
        RuleFor(c => c.Email, f => f.Person.Email);
    }
}
