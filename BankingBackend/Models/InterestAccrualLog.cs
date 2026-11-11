using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankingBackend.Models;

public class InterestAccrualLog
{
    public int Id { get; set; }

    public int FixedDepositId { get; set; }
    public FixedDeposit? FixedDeposit { get; set; }

    // Format: "YYYY-MM" (e.g., "2026-11") so we can check if this month was already processed
    public string PeriodMonth { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal InterestCredited { get; set; }

    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}