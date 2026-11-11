using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using BankingBackend.Data;
using BankingBackend.DTOs;
using BankingBackend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BankingBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Protects all endpoints in this controller: requires a valid JWT token!
public class SavingsController : ControllerBase
{
    private readonly AppDbContext _context;

    public SavingsController(AppDbContext context)
    {
        _context = context;
    }

    // Helper method to extract the logged-in Customer's ID directly from their JWT token
    private int GetLoggedInCustomerId()
    {
        string? userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.Parse(userIdString!);
    }

    // 1. GET /api/savings/dashboard -> Returns Balance + Recent Transactions
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        int customerId = GetLoggedInCustomerId();

        var account = await _context.SavingsAccounts
            .FirstOrDefaultAsync(a => a.CustomerId == customerId);

        if (account == null)
        {
            return NotFound(new { message = "Savings account not found." });
        }

        // Fetch the 20 most recent transactions for this account, newest first
        var recentTransactions = await _context.Transactions
            .Where(t => t.SavingsAccountId == account.Id)
            .OrderByDescending(t => t.TransactionDate)
            .Take(20)
            .ToListAsync();

        return Ok(new
        {
            accountId = account.Id,
            accountNumber = account.AccountNumber,
            balance = account.Balance,
            recentTransactions = recentTransactions
        });
    }

    // 2. POST /api/savings/deposit -> Adds money & records transaction atomically
    [HttpPost("deposit")]
    public async Task<IActionResult> Deposit([FromBody] TransactionRequestDto dto)
    {
        if (dto.Amount <= 0)
        {
            return BadRequest(new { message = "Deposit amount must be greater than zero." });
        }

        int customerId = GetLoggedInCustomerId();

        // Start a database transaction so balance update + transaction history happen together
        using var dbTransaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var account = await _context.SavingsAccounts
                .FirstOrDefaultAsync(a => a.CustomerId == customerId);

            if (account == null)
            {
                return NotFound(new { message = "Savings account not found." });
            }

            // Update balance
            account.Balance += dto.Amount;

            // Create transaction record
            var transaction = new Transaction
            {
                SavingsAccountId = account.Id,
                TransactionType = "Deposit",
                Amount = dto.Amount,
                BalanceAfter = account.Balance,
                Description = string.IsNullOrWhiteSpace(dto.Description) ? "Cash Deposit" : dto.Description,
                TransactionDate = DateTime.UtcNow
            };

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            // Commit both changes permanently to MySQL
            await dbTransaction.CommitAsync();

            return Ok(new
            {
                message = "Deposit successful!",
                newBalance = account.Balance,
                transaction = transaction
            });
        }
        catch (Exception)
        {
            await dbTransaction.RollbackAsync();
            throw;
        }
    }

    // 3. POST /api/savings/withdraw -> Checks sufficient balance, deducts money & records transaction
    [HttpPost("withdraw")]
    public async Task<IActionResult> Withdraw([FromBody] TransactionRequestDto dto)
    {
        if (dto.Amount <= 0)
        {
            return BadRequest(new { message = "Withdrawal amount must be greater than zero." });
        }

        int customerId = GetLoggedInCustomerId();

        using var dbTransaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var account = await _context.SavingsAccounts
                .FirstOrDefaultAsync(a => a.CustomerId == customerId);

            if (account == null)
            {
                return NotFound(new { message = "Savings account not found." });
            }

            // CRITICAL BUSINESS RULE: Never allow a withdrawal to make the balance negative
            if (account.Balance < dto.Amount)
            {
                return BadRequest(new { message = "Insufficient balance for this withdrawal." });
            }

            // Deduct balance
            account.Balance -= dto.Amount;

            // Create transaction record
            var transaction = new Transaction
            {
                SavingsAccountId = account.Id,
                TransactionType = "Withdrawal",
                Amount = dto.Amount,
                BalanceAfter = account.Balance,
                Description = string.IsNullOrWhiteSpace(dto.Description) ? "Cash Withdrawal" : dto.Description,
                TransactionDate = DateTime.UtcNow
            };

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            await dbTransaction.CommitAsync();

            return Ok(new
            {
                message = "Withdrawal successful!",
                newBalance = account.Balance,
                transaction = transaction
            });
        }
        catch (Exception)
        {
            await dbTransaction.RollbackAsync();
            throw;
        }
    }
}