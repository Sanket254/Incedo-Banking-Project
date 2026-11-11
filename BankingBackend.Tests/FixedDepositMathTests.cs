using BankingBackend.Controllers;
using Xunit;

namespace BankingBackend.Tests;

public class FixedDepositMathTests
{
    // 1. Parameterized test verifying all 3 interest rate slabs
    [Theory]
    [InlineData(6, 6.00)]    // 6-11 months -> 6.00%
    [InlineData(11, 6.00)]   // Boundary check at 11 months -> 6.00%
    [InlineData(12, 6.75)]   // 12-23 months -> 6.75%
    [InlineData(18, 6.75)]   // Mid-tier check -> 6.75%
    [InlineData(24, 7.25)]   // 24+ months -> 7.25%
    [InlineData(36, 7.25)]   // Long-term check -> 7.25%
    public void GetApplicableInterestRate_ReturnsCorrectSlabRate(int tenureMonths, decimal expectedRate)
    {
        // Act
        decimal actualRate = FixedDepositController.GetApplicableInterestRate(tenureMonths);

        // Assert
        Assert.Equal(expectedRate, actualRate);
    }

    // 2. Single test verifying exact Maturity Amount calculation for a 1-year FD
    [Fact]
    public void CalculateMaturityAmount_OneYearFd_ReturnsExactMaturityValue()
    {
        // Arrange: 2000 Principal at 6.75% for 12 months -> Interest = 135.00 -> Total = 2135.00
        decimal principal = 2000m;
        decimal annualRate = 6.75m;
        int tenureMonths = 12;

        // Act
        decimal maturityAmount = FixedDepositController.CalculateMaturityAmount(principal, annualRate, tenureMonths);

        // Assert
        Assert.Equal(2135.00m, maturityAmount);
    }

    // 3. Single test verifying 2 decimal rounding on fractional months
    [Fact]
    public void CalculateMaturityAmount_FractionalInterest_RoundsToTwoDecimals()
    {
        // Arrange: 10000 Principal at 6.00% for 6 months -> Interest = 300.00 -> Total = 10300.00
        decimal principal = 10000m;
        decimal annualRate = 6.00m;
        int tenureMonths = 6;

        // Act
        decimal maturityAmount = FixedDepositController.CalculateMaturityAmount(principal, annualRate, tenureMonths);

        // Assert
        Assert.Equal(10300.00m, maturityAmount);
    }
}