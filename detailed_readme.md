# 🏦 Incedo Digital Banking Portal

An enterprise-grade, full-stack digital banking and financial services web application built with **ASP.NET Core 8 Web API**, **React 18 with TypeScript**, and **MySQL 8.4**. 

The system simulates core retail banking functions including secure customer onboarding, automated savings account generation, real-time deposit/withdrawal transactions, fixed deposit management, and an idempotent batch interest processing engine for bank administrators.

---

## 📋 Table of Contents
1. [System Architecture](#-system-architecture)
2. [Tech Stack Breakdown](#-tech-stack-breakdown)
3. [Key Features & Business Capabilities](#-key-features--business-capabilities)
4. [Database Schema & Entity Models](#-database-schema--entity-models)
5. [Security & Authentication Architecture](#-security--authentication-architecture)
6. [Business Logic & Mathematical Calculations](#-business-logic--mathematical-calculations)
7. [API Endpoint Specification](#-api-endpoint-specification)
8. [Automated Testing Suite](#-automated-testing-suite)
9. [Local Development & Setup Guide](#-local-development--setup-guide)
10. [AWS Cloud Deployment Guide](#-aws-cloud-deployment-guide)
11. [Project Structure](#-project-structure)

---

## 🏗️ System Architecture

The application adopts a classic 3-Tier Cloud Architecture deployed completely on AWS infrastructure:

```
+-----------------------------------------------------------------------+
|                            PRESENTATION LAYER                         |
|  React 18 + TypeScript + Vite + Tailwind CSS (Hosted on Amazon S3)   |
+-----------------------------------------------------------------------+
                                   |
                                   | HTTPS / HTTP REST API
                                   v
+-----------------------------------------------------------------------+
|                             BUSINESS LAYER                            |
|     ASP.NET Core 8 Web API (Hosted on AWS Elastic Beanstalk / Linux)  |
|  - JWT Authentication & BCrypt Security                              |
|  - Savings & FD Business Domain Services                              |
|  - Admin Batch Interest Calculation Engine                             |
+-----------------------------------------------------------------------+
                                   |
                                   | EF Core 8 (Pomelo MySQL Provider)
                                   v
+-----------------------------------------------------------------------+
|                              DATA LAYER                               |
|                Amazon RDS (MySQL 8.4 Community Edition)              |
|  - Customers, SavingsAccounts, Transactions, FixedDeposits, Logs      |
+-----------------------------------------------------------------------+
```

---

## 🛠️ Tech Stack Breakdown

### **Frontend**
* **Framework:** React 18 (Vite build tool)
* **Language:** TypeScript
* **Styling:** Tailwind CSS / Lucide React Icons
* **HTTP Client:** Axios (with custom request/response interceptors)
* **Hosting:** Amazon S3 (Static Website Hosting)

### **Backend**
* **Framework:** ASP.NET Core 8.0 (Web API)
* **Language:** C# (.NET 8 SDK)
* **ORM:** Entity Framework Core 8.0
* **MySQL Provider:** Pomelo.EntityFrameworkCore.MySql (v8.0)
* **Security:** `System.IdentityModel.Tokens.Jwt`, `BCrypt.Net-Next`
* **API Documentation:** Swagger / OpenAPI UI
* **Hosting:** AWS Elastic Beanstalk (AL2023 Linux Environment running .NET 8)

### **Database & Cloud Infrastructure**
* **Database Engine:** Amazon RDS MySQL 8.4
* **Database Driver:** MySqlConnector
* **Cloud Platform:** Amazon Web Services (AWS Free Tier Infrastructure)

---

## 💡 Key Features & Business Capabilities

### **1. Customer Portal**
* **Instant Digital Account Provisioning:** Upon user registration, the system automatically generates a unique 10-digit savings account number formatted as `INCXXXXXXXX`.
* **Real-time Balance Dashboard:** Displays current available liquidity, total active fixed deposits, and recent account activity.
* **Atomic Liquidity Operations:** Allows customers to deposit or withdraw funds with immediate ledger updates and atomic transaction validation (e.g., overdraft prevention).
* **Fixed Deposit Booking:** Enables customers to transfer savings funds into high-yield Fixed Deposits with customizable duration terms (3 to 36 months).

### **2. Bank Administrator Portal**
* **Bank-Wide KPI Analytics:** Provides aggregated metrics on overall liquidity, total customer count, total active FD volume, and monthly payout commitments.
* **Batch Monthly Interest Engine:** Executes idempotent batch jobs to calculate and credit monthly pro-rated interest across all active Fixed Deposits.
* **System Accrual Audit:** Maintains an unalterable log of all processed batch jobs to prevent double-crediting.

---

## 🗄️ Database Schema & Entity Models

The database consists of **5 interconnected tables** with strong relational integrity rules enforced by Entity Framework Core migrations.

```
       +-------------------+
       |     Customers     |
       +-------------------+
                 | 1
                 |
                 | 1
       +-------------------+          1 +-------------------+
       |  SavingsAccounts  |------------|   Transactions    |
       +-------------------+            +-------------------+
                 | 1                              *
                 |
                 | *
       +-------------------+          1 +-----------------------+
       |   FixedDeposits   |------------|  InterestAccrualLogs  |
       +-------------------+            +-----------------------+
```

### **Table Definitions**

#### 1. `Customers`
| Field | Type | Constraints | Description |
| :--- | :--- | :--- | :--- |
| `CustomerId` | `int` | Primary Key, Auto-increment | Unique customer identifier |
| `FirstName` | `varchar(50)` | Not Null | Customer first name |
| `LastName` | `varchar(50)` | Not Null | Customer last name |
| `Email` | `varchar(100)` | Unique, Not Null | User login identifier |
| `PasswordHash` | `varchar(255)` | Not Null | BCrypt salted hash |
| `Role` | `varchar(20)` | Not Null | `Customer` or `Admin` |
| `CreatedAt` | `datetime` | Default: UTC Now | Registration timestamp |

#### 2. `SavingsAccounts`
| Field | Type | Constraints | Description |
| :--- | :--- | :--- | :--- |
| `AccountId` | `int` | Primary Key, Auto-increment | Internal account ID |
| `AccountNumber` | `varchar(20)` | Unique, Not Null | Public account number (`INC...`) |
| `CustomerId` | `int` | Foreign Key (`Customers.CustomerId`) | Account owner link |
| `Balance` | `decimal(18,2)` | Not Null, Min: 0.00 | Real-time available liquidity |
| `CreatedAt` | `datetime` | Default: UTC Now | Opening date |

#### 3. `Transactions`
| Field | Type | Constraints | Description |
| :--- | :--- | :--- | :--- |
| `TransactionId` | `int` | Primary Key, Auto-increment | Unique transaction ID |
| `AccountId` | `int` | Foreign Key (`SavingsAccounts.AccountId`) | Target account |
| `Amount` | `decimal(18,2)` | Not Null | Transaction value |
| `Type` | `varchar(20)` | Not Null | `Deposit`, `Withdrawal`, `FD_Debit`, `Interest_Credit` |
| `Description` | `varchar(255)` | Not Null | Transaction summary |
| `Timestamp` | `datetime` | Default: UTC Now | Audit timestamp |

#### 4. `FixedDeposits`
| Field | Type | Constraints | Description |
| :--- | :--- | :--- | :--- |
| `FdId` | `int` | Primary Key, Auto-increment | Unique FD record ID |
| `CustomerId` | `int` | Foreign Key (`Customers.CustomerId`) | Owner link |
| `PrincipalAmount` | `decimal(18,2)` | Not Null | Initial locked balance |
| `InterestRate` | `decimal(5,2)` | Not Null | Annualized interest rate (%) |
| `DurationMonths` | `int` | Not Null | Term length (3 - 36 months) |
| `StartDate` | `datetime` | Not Null | Term start timestamp |
| `MaturityDate` | `datetime` | Not Null | Calculation end date |
| `Status` | `varchar(20)` | Not Null | `Active`, `Matured`, `Closed` |

#### 5. `InterestAccrualLogs`
| Field | Type | Constraints | Description |
| :--- | :--- | :--- | :--- |
| `LogId` | `int` | Primary Key, Auto-increment | Audit log entry ID |
| `FdId` | `int` | Foreign Key (`FixedDeposits.FdId`) | Processed FD link |
| `ProcessedYear` | `int` | Not Null | Audit year (e.g., 2026) |
| `ProcessedMonth` | `int` | Not Null | Audit month (1-12) |
| `InterestAmount` | `decimal(18,2)` | Not Null | Calculated interest payout |
| `ProcessedAt` | `datetime` | Default: UTC Now | Execution timestamp |

---

## 🔒 Security & Authentication Architecture

* **Stateless Token Authentication:** Utilizes **JSON Web Tokens (JWT)** signed with a 256-bit secret key (`HMAC-SHA256`).
* **Password Hashing:** Passwords are never stored in plain text. All credentials are salted and hashed using **BCrypt** (`workFactor = 11`).
* **Role-Based Access Control (RBAC):** Endpoints are protected via standard ASP.NET Core `[Authorize(Roles = "Admin")]` and `[Authorize(Roles = "Customer")]` attributes.
* **CORS Policy:** Configured at the framework level to allow secure cross-origin resource sharing between the S3 client domain and the API backend.

---

## 🧮 Business Logic & Mathematical Calculations

### **1. Tiered Interest Rate Structure**
The backend applies dynamic interest rates for Fixed Deposits based on the term duration:
$$\text{Interest Rate} = \begin{cases} 
5.5\% & \text{if } 3 \le \text{Duration} < 6 \text{ months} \\
6.5\% & \text{if } 6 \le \text{Duration} < 12 \text{ months} \\
7.5\% & \text{if } \text{Duration} \ge 12 \text{ months}
\end{cases}$$

### **2. Monthly Interest Calculation Formula**
The monthly interest credited to a customer's linked savings account during batch processing is derived as:
$$\text{Monthly Interest} = \frac{\text{PrincipalAmount} \times \left(\frac{\text{InterestRate}}{100}\right)}{12}$$

*Example: A principal of ₹100,000 booked for 12 months at 7.5% annual interest yields:*
$$\text{Monthly Payout} = \frac{100,000 \times 0.075}{12} = ₹625.00$$

### **3. Idempotency Check Algorithm**
To prevent accidental double-payouts during batch job executions, the `ProcessInterest` service checks:
$$\text{Can Process} = \neg \text{Exists}\Big(\text{InterestAccrualLogs} \mid \text{FdId} = x \land \text{Year} = Y \land \text{Month} = M\Big)$$

---

## 📡 API Endpoint Specification

### **Authentication Controller (`/api/Auth`)**
| Method | Endpoint | Authorization | Description |
| :--- | :--- | :--- | :--- |
| `POST` | `/api/Auth/register` | Public | Registers a new user & creates a default savings account |
| `POST` | `/api/Auth/login` | Public | Authenticates credentials and returns a JWT |

### **Savings Controller (`/api/Savings`)**
| Method | Endpoint | Authorization | Description |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/Savings/dashboard` | `Customer` | Returns account balance, account number, & history |
| `POST` | `/api/Savings/deposit` | `Customer` | Credits funds to the logged-in customer's account |
| `POST` | `/api/Savings/withdraw` | `Customer` | Debits funds if sufficient balance is present |

### **Fixed Deposit Controller (`/api/FixedDeposit`)**
| Method | Endpoint | Authorization | Description |
| :--- | :--- | :--- | :--- |
| `POST` | `/api/FixedDeposit/open` | `Customer` | Books an FD, deducting principal from savings balance |
| `GET` | `/api/FixedDeposit/my-fds` | `Customer` | Retrieves all active and matured FDs for the user |

### **Admin Controller (`/api/Admin`)**
| Method | Endpoint | Authorization | Description |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/Admin/summary` | `Admin` | Aggregates system liquidity & global metrics |
| `POST` | `/api/Admin/process-interest` | `Admin` | Triggers the monthly batch interest accrual job |

---

## 🧪 Automated Testing Suite

The application includes an **xUnit** unit testing suite located in the `BankingBackend.Tests` project. 

### **Tested Scenarios**
1. `CalculateTieredInterestRate_ReturnsCorrectRatesForTermLengths` (Verifies rate assignment for 3, 6, 12+ month terms).
2. `OpenFixedDeposit_InsufficientSavingsBalance_ThrowsException` (Validates transactional safeguards).
3. `ProcessMonthlyInterest_IdempotencyCheck_PreventsDoublePayout` (Ensures log table enforcement).
4. `WithdrawSavings_ValidAmount_UpdatesBalanceAndLogsTransaction` (Verifies atomic state changes).

### **Executing Tests**
```bash
cd BankingBackend.Tests
dotnet test
```

---

## 💻 Local Development & Setup Guide

### **Prerequisites**
* [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
* [Node.js v18+](https://nodejs.org/)
* [MySQL Community Server 8.0+](https://dev.mysql.com/downloads/mysql/)

### **1. Database Configuration**
Create a new MySQL schema locally:
```sql
CREATE DATABASE IncedoBankingDb;
```

### **2. Backend Configuration**
Navigate to `BankingBackend/appsettings.json` and adjust the connection string:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=IncedoBankingDb;User=root;Password=your_password;"
  },
  "JwtSettings": {
    "Secret": "SUPER_SECRET_KEY_WITH_AT_LEAST_256_BITS_OF_ENTROPY",
    "Issuer": "IncedoBankingApi",
    "Audience": "IncedoBankingApp"
  }
}
```

Apply database migrations:
```bash
cd BankingBackend
dotnet ef database update
dotnet run
```
The API will start at `http://localhost:5000` (Swagger UI: `http://localhost:5000/swagger`).

### **3. Frontend Configuration**
Create a `.env.local` file inside `banking-frontend/`:
```env
VITE_API_URL=http://localhost:5000/api
```

Install dependencies and start the development server:
```bash
cd banking-frontend
npm install
npm run dev
```
The client will start at `http://localhost:5173`.

---

## ☁️ AWS Cloud Deployment Guide

### **1. Database (Amazon RDS)**
* Engine: MySQL 8.4 LTS Free Tier.
* Connectivity: Publicly Accessible (with inbound port 3306 open to EC2 Security Group).

### **2. Backend (AWS Elastic Beanstalk)**
* Platform: `.NET 8 on AL2023 Linux`.
* Environment Variables configured in Beanstalk Console:
  * `ConnectionStrings__DefaultConnection`: `Server=your-rds-endpoint.rds.amazonaws.com;Port=3306;Database=IncedoBankingDb;User=admin;Password=your_password;`
  * `JwtSettings__Secret`: `YourProductionJwtSecretKey...`

### **3. Frontend (Amazon S3 Static Website)**
* Build artifact generated via `npm run build` (`dist/` directory).
* Uploaded to an S3 bucket configured for **Static Website Hosting** with a public `s3:GetObject` policy.

---

## 📁 Project Structure

```
Incedo Mini project/
├── BankingBackend/              # ASP.NET Core 8 Web API
│   ├── Controllers/             # API Endpoints (Auth, Savings, FD, Admin)
│   ├── Data/                    # AppDbContext & EF Core Configurations
│   ├── Models/                  # Entity Models & DTOs
│   ├── Services/                # Core Business Logic & Interest Engines
│   ├── Program.cs               # Application Entry Point & Dependency Injection
│   └── appsettings.json         # Environment Configuration
│
├── BankingBackend.Tests/        # xUnit Test Suite
│   └── Services/                # Unit Tests for Banking Logic
│
└── banking-frontend/            # React 18 + TypeScript Frontend
    ├── src/
    │   ├── components/          # Reusable UI Components
    │   ├── pages/               # Auth, Dashboard, FD, and Admin Views
    │   ├── services/            # Axios API Integration Services
    │   └── App.tsx              # Application Routing & State Provider
    ├── index.html
    └── package.json
```