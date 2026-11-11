using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using BankingBackend.Data;
using BankingBackend.DTOs;
using BankingBackend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace BankingBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _config;

    // Dependency Injection: .NET automatically passes AppDbContext and IConfiguration here
    public AuthController(AppDbContext context, IConfiguration config)  //In FastAPI, you put db: Session = Depends(get_db) inside each endpoint function. In C#, you put dependencies in the Class Constructor once, assign them to private fields (_context and _config), and every endpoint in the file can use them!
    {
        _context = context;
        _config = config;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)  //async - It keeps the web server non-blocking while waiting for MySQL
    {                                           //[FromBody]: Tells .NET to parse the incoming JSON body into your RegisterDto object (just like FastAPI does automatically with Pydantic).
    
        // 1. Check if email already exists in MySQL
        bool emailExists = await _context.Customers.AnyAsync(c => c.Email == dto.Email);
        if (emailExists)                                    //^(LINQ Lambda Expression): This is C#'s LINQ (Language Integrated Query)
        {
            return BadRequest(new { message = "Email is already registered." });
        }

        // 2. Hash the plain-text password using BCrypt
        string passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

        // 3. Create the Customer object
        var customer = new Customer
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email,
            PasswordHash = passwordHash,
            Role = dto.Role == "Admin" ? "Admin" : "Customer",
            CreatedAt = DateTime.UtcNow
        };

        // 4. Stage Customer in EF Core and save to MySQL so it generates customer.Id
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();  //Exact equivalent of SQLAlchemy's db.add(customer) and db.commit()

        // 5. Automatically create a Savings Account for this new Customer
        var savingsAccount = new SavingsAccount
        {
            CustomerId = customer.Id,
            AccountNumber = "INC" + DateTime.UtcNow.Ticks.ToString().Substring(10),
            Balance = 0m,
            CreatedAt = DateTime.UtcNow
        };

        _context.SavingsAccounts.Add(savingsAccount);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Registration successful!",
            customerId = customer.Id,
            accountNumber = savingsAccount.AccountNumber,
            role = customer.Role
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        // 1. Find the customer by Email
        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Email == dto.Email);
        if (customer == null)
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        // 2. Verify the password against the stored BCrypt hash
        bool isPasswordValid = BCrypt.Net.BCrypt.Verify(dto.Password, customer.PasswordHash);
        if (!isPasswordValid)
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        // 3. Generate the JWT Token
        string token = GenerateJwtToken(customer);

        return Ok(new
        {
            token = token,
            customer = new
            {
                id = customer.Id,
                firstName = customer.FirstName,
                lastName = customer.LastName,
                email = customer.Email,
                role = customer.Role
            }
        });
    }

    private string GenerateJwtToken(Customer customer)
    {
        var jwtKey = _config["Jwt:Key"]!;
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        // Claims are the payload data stored inside the JWT (like sub, email, role)
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, customer.Id.ToString()),
            new Claim(ClaimTypes.Email, customer.Email),
            new Claim(ClaimTypes.Role, customer.Role)
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}