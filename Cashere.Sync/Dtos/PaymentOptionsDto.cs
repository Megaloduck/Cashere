using System;

namespace Cashere.Sync.Dtos;

public record PaymentOptionsDto(
    bool CashEnabled,
    bool QrisEnabled,
    bool EdcEnabled,
    string? QrisAccountInfo,
    string? EdcAccountInfo);

public record PaymentNotificationDto(string SaleNumber, decimal Amount, DateTime PaidAt);
