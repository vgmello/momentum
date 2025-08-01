// Copyright (c) ABCDEG. All rights reserved.

namespace AppDomain.Cashiers.Contracts.Models;

public record CashierPayment
{
    public int CashierId { get; set; }

    public int PaymentId { get; set; }

    public DateTime PaymentDate { get; set; }
}
