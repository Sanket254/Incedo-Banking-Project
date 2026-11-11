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
[Authorize]
public class FixedDepositController : ControllerBase
{
    private readonly AppDbContext _context;

    public FixedDepositController(AppDbContext context)
    {
        _context = context;
    }

    private int GetLoggedInCustomerId()
    {
        string? userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.Parse(userIdString!);
    }

    // Helper: Determines the annual interest rate slab based on tenure in months
    public static decimal GetApplicableInterestRate(int tenureMonths)
    {
        if (tenureMonths < 12) return 6.00m;
        if (tenureMonths < 24) return 6.75m;
        return 7.25m;
    }

    // Helper: Calculates total Maturity Value rounded to 2 decimal places
    public static decimal CalculateMaturityAmount(decimal principal, decimal annualRate, int tenureMonths)
    {
        decimal interest = principal * (annualRate / 100m) * (tenureMonths / 12m);
        return Math.Round(principal + interest, 2);
    }

    // 1. POST /api/FixedDeposit/calculate -> Preview FD rate & maturity before opening
    [HttpPost("calculate")]
    public IActionResult CalculatePreview([FromBody] CreateFixedDepositDto dto)
    {
        if (dto.PrincipalAmount < 1000)
        {
            return BadRequest(new { message = "Minimum FD amount is 1000." });
        }
        if (dto.TenureMonths < 6 || dto.TenureMonths > 120)
        {
            return BadRequest(new { message = "Tenure must be between 6 and 120 months." });
        }

        decimal rate = GetApplicableInterestRate(dto.TenureMonths);
        decimal maturityAmount = CalculateMaturityAmount(dto.PrincipalAmount, rate, dto.TenureMonths);
        DateTime maturityDate = DateTime.UtcNow.AddMonths(dto.TenureMonths);

        return Ok(new
        {
            principalAmount = dto.PrincipalAmount,
            tenureMonths = dto.TenureMonths,
            interestRate = rate,
            interestEarned = maturityAmount - dto.PrincipalAmount,
            maturityAmount = maturityAmount,
            maturityDate = maturityDate
        });
    }

    // 2. POST /api/FixedDeposit/open -> Deducts from Savings & creates active FD
    [HttpPost("open")]
    public async Task<IActionResult> OpenFixedDeposit([FromBody] CreateFixedDepositDto dto)
    {
        if (dto.PrincipalAmount < 1000)
        {
            return BadRequest(new { message = "Minimum FD amount is 1000." });
        }
        if (dto.TenureMonths < 6 || dto.TenureMonths > 120)
        {
            return BadRequest(new { message = "Tenure must be between 6 and 120 months." });
        }

        int customerId = GetLoggedInCustomerId();

        using var dbTransaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // 1. Fetch customer's savings account and check balance
            var savingsAccount = await _context.SavingsAccounts
                .FirstOrDefaultAsync(a => a.CustomerId == customerId);

            if (savingsAccount == null)
            {
                return NotFound(new { message = "Savings account not found." });
            }

            if (savingsAccount.Balance < dto.PrincipalAmount)
            {
                return BadRequest(new { message = "Insufficient savings balance to open this Fixed Deposit." });
            }

            // 2. Deduct FD Principal from Savings Account
            savingsAccount.Balance -= dto.PrincipalAmount;

            // 3. Calculate FD parameters
            decimal rate = GetApplicableInterestRate(dto.TenureMonths);
            DateTime startDate = DateTime.UtcNow;
            DateTime maturityDate = startDate.AddMonths(dto.TenureMonths);
            decimal maturityAmount = CalculateMaturityAmount(dto.PrincipalAmount, rate, dto.TenureMonths);

            // 4. Create the Fixed Deposit record
            var fd = new FixedDeposit
            {
                CustomerId = customerId,
                PrincipalAmount = dto.PrincipalAmount,
                TenureMonths = dto.TenureMonths,
                InterestRate = rate,
                StartDate = startDate,
                MaturityDate = maturityDate,
                MaturityAmount = maturityAmount,
                Status = "Active"
            };

            _context.FixedDeposits.Add(fd);
            await _context.SaveChangesAsync();

            // 5. Record the deduction in the Savings Account Transactions history
            var transaction = new Transaction
            {
                SavingsAccountId = savingsAccount.Id,
                TransactionType = "FD_Open",
                Amount = dto.PrincipalAmount,
                BalanceAfter = savingsAccount.Balance,
                Description = $"Opened FD #{fd.Id} for {dto.TenureMonths} months @ {rate}%",
                TransactionDate = DateTime.UtcNow
            };

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            await dbTransaction.CommitAsync();

            return Ok(new
            {
                message = "Fixed Deposit opened successfully!",
                newSavingsBalance = savingsAccount.Balance,
                fixedDeposit = fd
            });
        }
        catch (Exception)
        {
            await dbTransaction.RollbackAsync();
            throw;
        }
    }

    // 3. GET /api/FixedDeposit/my-fds -> Returns all FDs belonging to the logged-in customer
    [HttpGet("my-fds")]
    public async Task<IActionResult> GetMyFixedDeposits()
    {
        int customerId = GetLoggedInCustomerId();

        var fds = await _context.FixedDeposits
            .Where(f => f.CustomerId == customerId)
            .OrderByDescending(f => f.StartDate)
            .ToListAsync();

        return Ok(fds);
    }

    // 4. POST /api/FixedDeposit/{id}/premature-withdraw -> Closes FD early with 1% penalty & credits Savings
    [HttpPost("{id}/premature-withdraw")]
    public async Task<IActionResult> PrematureWithdraw(int id)
    {
        int customerId = GetLoggedInCustomerId();

        using var dbTransaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // 1. Find the FD and ensure it belongs to the logged-in customer
            var fd = await _context.FixedDeposits
                .FirstOrDefaultAsync(f => f.Id == id && f.CustomerId == customerId);

            if (fd == null)
            {
                return NotFound(new { message = "Fixed Deposit not found." });
            }

            if (fd.Status != "Active")
            {
                return BadRequest(new { message = $"Cannot withdraw an FD with status '{fd.Status}'." });
            }

            var savingsAccount = await _context.SavingsAccounts
                .FirstOrDefaultAsync(a => a.CustomerId == customerId);

            if (savingsAccount == null)
            {
                return NotFound(new { message = "Savings account not found." });
            }

            // 2. Calculate days elapsed and apply 1.00% penalty to interest rate
            DateTime now = DateTime.UtcNow;
            int daysElapsed = Math.Max(0, (now - fd.StartDate).Days);

            decimal penaltyRate = 1.00m;
            decimal effectiveRate = Math.Max(0m, fd.InterestRate - penaltyRate);

            decimal interestEarned = Math.Round(
                fd.PrincipalAmount * (effectiveRate / 100m) * (daysElapsed / 365m), 2);

            decimal payoutAmount = fd.PrincipalAmount + interestEarned;

            // 3. Update FD status to "Closed"
            fd.Status = "Closed";
            fd.ClosedDate = now;

            // 4. Credit the payout amount back into the customer's Savings Account
            savingsAccount.Balance += payoutAmount;

            // 5. Log the credit transaction
            var transaction = new Transaction
            {
                SavingsAccountId = savingsAccount.Id,
                TransactionType = "FD_Premature_Payout",
                Amount = payoutAmount,
                BalanceAfter = savingsAccount.Balance,
                Description = $"Premature closure of FD #{fd.Id} (Principal: {fd.PrincipalAmount}, Interest: {interestEarned} after 1% penalty)",
                TransactionDate = now
            };

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            await dbTransaction.CommitAsync();

            return Ok(new
            {
                message = "Fixed Deposit closed prematurely and funds credited to Savings Account.",
                fdId = fd.Id,
                daysHeld = daysElapsed,
                originalRate = fd.InterestRate,
                effectiveRateAfterPenalty = effectiveRate,
                interestEarned = interestEarned,
                totalPayout = payoutAmount,
                newSavingsBalance = savingsAccount.Balance
            });
        }
        catch (Exception)
        {
            await dbTransaction.RollbackAsync();
            throw;
        }
    }
}