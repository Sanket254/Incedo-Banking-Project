using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankingBackend.Models;

public class Transaction
{
    public int Id { get; set; }

    // Foreign Key linking to SavingsAccounts
    public int SavingsAccountId { get; set; }
    public SavingsAccount? SavingsAccount { get; set; }

    // e.g., "Deposit", "Withdrawal", "FD_Open", "FD_Payout", "Interest_Credit"
    public string TransactionType { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    // Snapshot of the account balance immediately after this transaction
    [Column(TypeName = "decimal(18,2)")]
    public decimal BalanceAfter { get; set; }

    public string Description { get; set; } = string.Empty;
    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
}