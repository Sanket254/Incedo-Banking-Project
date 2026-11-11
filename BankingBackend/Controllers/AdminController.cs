using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BankingBackend.Data;
using BankingBackend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BankingBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")] // Strictly restricts all endpoints here to users with Role == "Admin"
public class AdminController : ControllerBase
{
    private readonly AppDbContext _context;

    public AdminController(AppDbContext context)
    {
        _context = context;
    }

    // 1. GET /api/Admin/summary -> Bank-wide KPI metrics for the Admin Dashboard
    [HttpGet("summary")]
    public async Task<IActionResult> GetBankSummary()
    {
        int totalCustomers = await _context.Customers.CountAsync(c => c.Role == "Customer");
        decimal totalSavingsBalance = await _context.SavingsAccounts.SumAsync(a => a.Balance);

        int activeFdCount = await _context.FixedDeposits.CountAsync(f => f.Status == "Active");
        decimal activeFdPrincipal = await _context.FixedDeposits
            .Where(f => f.Status == "Active")
            .SumAsync(f => f.PrincipalAmount);

        decimal totalInterestPaid = await _context.InterestAccrualLogs.SumAsync(l => l.InterestCredited);

        return Ok(new
        {
            totalCustomers,
            totalSavingsBalance,
            activeFdCount,
            activeFdPrincipal,
            totalInterestPaid
        });
    }

    // 2. GET /api/Admin/customers -> Lists all customers alongside their Savings Account details
    [HttpGet("customers")]
    public async Task<IActionResult> GetAllCustomers()
    {
        var customers = await (
            from c in _context.Customers
            join s in _context.SavingsAccounts on c.Id equals s.CustomerId into savingsGroup
            from s in savingsGroup.DefaultIfEmpty()
            orderby c.CreatedAt descending
            select new
            {
                customerId = c.Id,
                firstName = c.FirstName,
                lastName = c.LastName,
                email = c.Email,
                role = c.Role,
                createdAt = c.CreatedAt,
                accountNumber = s != null ? s.AccountNumber : "N/A",
                savingsBalance = s != null ? s.Balance : 0m
            }
        ).ToListAsync();

        return Ok(customers);
    }

    // 3. GET /api/Admin/fixed-deposits -> Lists all FDs across the bank with Customer details
    [HttpGet("fixed-deposits")]
    public async Task<IActionResult> GetAllFixedDeposits()
    {
        var fds = await _context.FixedDeposits
            .Include(f => f.Customer) // Performs a SQL JOIN with the Customers table
            .OrderByDescending(f => f.StartDate)
            .Select(f => new
            {
                fdId = f.Id,
                customerId = f.CustomerId,
                customerName = f.Customer != null ? $"{f.Customer.FirstName} {f.Customer.LastName}" : "Unknown",
                customerEmail = f.Customer != null ? f.Customer.Email : "Unknown",
                principalAmount = f.PrincipalAmount,
                tenureMonths = f.TenureMonths,
                interestRate = f.InterestRate,
                startDate = f.StartDate,
                maturityDate = f.MaturityDate,
                maturityAmount = f.MaturityAmount,
                status = f.Status,
                closedDate = f.ClosedDate
            })
            .ToListAsync();

        return Ok(fds);
    }

    // 4. POST /api/Admin/process-interest?periodMonth=2026-11 -> Runs Monthly Interest Accrual
    [HttpPost("process-interest")]
    public async Task<IActionResult> ProcessMonthlyInterest([FromQuery] string? periodMonth)
    {
        // Default to current UTC year-month (e.g., "2026-11") if not provided
        string targetPeriod = string.IsNullOrWhiteSpace(periodMonth)
            ? DateTime.UtcNow.ToString("yyyy-MM")
            : periodMonth.Trim();

        using var dbTransaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var activeFds = await _context.FixedDeposits
                .Where(f => f.Status == "Active")
                .ToListAsync();

            int processedCount = 0;
            int skippedCount = 0;
            int maturedCount = 0;
            decimal totalInterestCreditedNow = 0m;
            var logsCreated = new List<object>();

            foreach (var fd in activeFds)
            {
                // 1. Idempotency Check: Has interest already been credited for this FD in targetPeriod?
                bool alreadyProcessed = await _context.InterestAccrualLogs
                    .AnyAsync(l => l.FixedDepositId == fd.Id && l.PeriodMonth == targetPeriod);

                if (alreadyProcessed)
                {
                    skippedCount++;
                    continue;
                }

                var savingsAccount = await _context.SavingsAccounts
                    .FirstOrDefaultAsync(a => a.CustomerId == fd.CustomerId);

                if (savingsAccount == null)
                {
                    skippedCount++;
                    continue;
                }

                // 2. Calculate 1 month's simple interest: Principal * (Rate / 100) / 12
                decimal monthlyInterest = Math.Round(fd.PrincipalAmount * (fd.InterestRate / 100m) / 12m, 2);

                // 3. Credit monthly interest to the customer's Savings Account
                savingsAccount.Balance += monthlyInterest;

                var interestTx = new Transaction
                {
                    SavingsAccountId = savingsAccount.Id,
                    TransactionType = "Interest_Credit",
                    Amount = monthlyInterest,
                    BalanceAfter = savingsAccount.Balance,
                    Description = $"Monthly FD Interest for FD #{fd.Id} ({targetPeriod})",
                    TransactionDate = DateTime.UtcNow
                };
                _context.Transactions.Add(interestTx);

                // 4. Record in InterestAccrualLogs so this period can never be credited twice
                var accrualLog = new InterestAccrualLog
                {
                    FixedDepositId = fd.Id,
                    PeriodMonth = targetPeriod,
                    InterestCredited = monthlyInterest,
                    ProcessedAt = DateTime.UtcNow
                };
                _context.InterestAccrualLogs.Add(accrualLog);

                // 5. Check if the FD has reached its Maturity Date or completed all tenure months
                int totalMonthsPaid = await _context.InterestAccrualLogs
                    .CountAsync(l => l.FixedDepositId == fd.Id) + 1;

                if (DateTime.UtcNow >= fd.MaturityDate || totalMonthsPaid >= fd.TenureMonths)
                {
                    fd.Status = "Matured";
                    fd.ClosedDate = DateTime.UtcNow;
                    savingsAccount.Balance += fd.PrincipalAmount;

                    var maturityTx = new Transaction
                    {
                        SavingsAccountId = savingsAccount.Id,
                        TransactionType = "FD_Maturity_Payout",
                        Amount = fd.PrincipalAmount,
                        BalanceAfter = savingsAccount.Balance,
                        Description = $"Maturity Principal Payout for FD #{fd.Id}",
                        TransactionDate = DateTime.UtcNow
                    };
                    _context.Transactions.Add(maturityTx);
                    maturedCount++;
                }

                processedCount++;
                totalInterestCreditedNow += monthlyInterest;

                logsCreated.Add(new
                {
                    fdId = fd.Id,
                    customerId = fd.CustomerId,
                    period = targetPeriod,
                    interestCredited = monthlyInterest,
                    newStatus = fd.Status
                });
            }

            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();

            return Ok(new
            {
                message = $"Interest accrual batch completed for period {targetPeriod}.",
                periodMonth = targetPeriod,
                processedCount,
                skippedAlreadyProcessed = skippedCount,
                maturedFdsCount = maturedCount,
                totalInterestCredited = totalInterestCreditedNow,
                details = logsCreated
            });
        }
        catch (Exception)
        {
            await dbTransaction.RollbackAsync();
            throw;
        }
    }
}