using BankingBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace BankingBackend.Data;
//The DbContext is the heart of Entity Framework. It is exactly like your SQLAlchemy Session class. It manages the connection and coordinates saving/fetching objects
public class AppDbContext : DbContext
{
    // The constructor accepts connection options and passes them to the base class
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // This property represents the actual 'Customers' table in SQL Server
    public DbSet<Customer> Customers { get; set; }
    public DbSet<SavingsAccount> SavingsAccounts { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<FixedDeposit> FixedDeposits { get; set; }
    public DbSet<InterestAccrualLog> InterestAccrualLogs { get; set; }

}