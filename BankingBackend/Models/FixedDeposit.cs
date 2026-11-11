using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankingBackend.Models;

public class FixedDeposit
{
    public int Id { get; set; }

    // Foreign Key linking to Customers
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal PrincipalAmount { get; set; }

    // Duration in months (e.g., 6, 12, 24, 36)
    public int TenureMonths { get; set; }

    // Annual interest rate percentage (e.g., 6.50, 7.25)
    [Column(TypeName = "decimal(5,2)")]
    public decimal InterestRate { get; set; }

    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime MaturityDate { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal MaturityAmount { get; set; }

    // Status: "Active", "Matured", or "Closed" (for premature withdrawal)
    public string Status { get; set; } = "Active";

    public DateTime? ClosedDate { get; set; }
}