# Incedo Digital Banking Portal

A full-stack digital banking solution built with ASP.NET Core 8, React TypeScript, and MySQL. This application simulates core banking operations including secure user authentication, transactional savings accounts, and fixed deposit interest accrual engines.

## 🏗️ Cloud Architecture & Tech Stack

This project is deployed entirely on the AWS Free Tier cloud infrastructure:
* **Frontend UI:** React + Vite + TypeScript (Hosted on **Amazon S3 Static Website**)
* **Backend API:** C# .NET 8 Web API (Hosted on **AWS Elastic Beanstalk** - Linux)
* **Database:** MySQL 8.4 LTS (Hosted on **Amazon RDS**)
* **ORM:** Entity Framework (EF) Core with automated cloud migrations

## 🗄️ Database Schema

The system utilizes a relational database (`IncedoBankingDb`) consisting of 5 interconnected tables:
1. **Customers:** Stores user identities, hashed passwords (BCrypt), and role-based access claims (Customer/Admin).
2. **SavingsAccounts:** Auto-generated `INC...` account numbers tied to customers via foreign keys, maintaining real-time transactional liquidity.
3. **Transactions:** An immutable ledger tracking deposits, withdrawals, FD payouts, and interest credits.
4. **FixedDeposits:** Tracks locked principal amounts, tiered interest rates, and maturity timelines.
5. **InterestAccrualLogs:** An idempotency table that ensures the batch monthly interest engine never double-credits an active Fixed Deposit.

## 🚀 Key API Endpoints & Features

The API relies on JWT (JSON Web Tokens) for stateless authentication. 

**Public Routes:**
* `POST /api/Auth/register`: Hashes the user's password, creates a Customer record, and instantly provisions an empty Savings Account.
* `POST /api/Auth/login`: Validates credentials and issues a signed JWT.

**Protected Customer Routes (Requires `Customer` JWT):**
* `GET /api/Savings/dashboard`: Retrieves live balance and transaction history.
* `POST /api/Savings/deposit` & `/withdraw`: Executes atomic database transactions to update liquidity.
* `POST /api/FixedDeposit/open`: Locks funds from Savings into a new FD term.

**Protected Admin Routes (Requires `Admin` JWT):**
* `GET /api/Admin/summary`: Aggregates bank-wide KPIs (total liquidity, active FD volume).
* `POST /api/Admin/process-interest`: A batch processing engine that loops through all active FDs, calculates one month's pro-rated interest, credits the linked savings accounts, and safely updates the accrual log.

## 🧪 Testing
The backend business logic (specifically the complex Fixed Deposit maturity and penalty math) is verified by an **xUnit** test suite featuring 8 passing `[Fact]` and `[Theory]` tests.
