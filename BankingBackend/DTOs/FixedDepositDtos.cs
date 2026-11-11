namespace BankingBackend.DTOs;

public class CreateFixedDepositDto
{
    public decimal PrincipalAmount { get; set; }
    public int TenureMonths { get; set; }
}