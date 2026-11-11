namespace BankingBackend.DTOs;

public class TransactionRequestDto
{
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
}