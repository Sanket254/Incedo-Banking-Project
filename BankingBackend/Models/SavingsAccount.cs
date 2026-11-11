using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankingBackend.Models;

public class SavingsAccount
{
    public int Id { get; set; }
    
    // Foreign Key linking to the Customers table
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public string AccountNumber { get; set; } = string.Empty;

    // Stores exact currency in MySQL as DECIMAL(18,2)
    [Column(TypeName = "decimal(18,2)")]
    public decimal Balance { get; set; } = 0m;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}