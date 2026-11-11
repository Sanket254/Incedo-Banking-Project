using System;

namespace BankingBackend.Models;

//This is your SQLAlchemy equivalent—a C# class that represents a database table.
public class Customer
{
    public int Id { get; set; }  // EF Core automatically makes 'Id' the Primary Key
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;

    // Either "Customer" or "Admin"
    public string Role { get; set; } = "Customer";
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

//(The { get; set; } syntax creates "Properties". It means these fields can be read from and written to)