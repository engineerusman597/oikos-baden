# Oikos Baden - Operations Management Platform

> A comprehensive operations dashboard for Oikos Baden, built with .NET 9 Blazor Server and Clean Architecture principles.

**Live Application**: [my.online-mahnantraege.de](https://my.online-mahnantraege.de)

---

## 📋 Table of Contents

- [Overview](#overview)
- [Architecture](#architecture)
- [Key Features](#key-features)
- [Technology Stack](#technology-stack)
- [Getting Started](#getting-started)
- [Project Structure](#project-structure)
- [Configuration](#configuration)
- [Production Deployment](#production-deployment)
- [Development](#development)
- [API Integration](#api-integration)
- [License](#license)

---

## 🎯 Overview

**Oikos Baden** is a modern SaaS platform designed for managing legal claims, invoices, and company verification processes. The platform streamlines the workflow of debt collection, invoice management, and company credit checks through an intuitive web interface.

The platform serves **two distinct business domains** under one codebase:
- **Rechtfix** ([my.online-mahnantraege.de](https://my.online-mahnantraege.de)): Full-featured debt collection and invoice management platform
- **Bonix** ([bonix-auskunft.de](https://bonix-auskunft.de)): Standalone company credit check portal with its own branding and user flow

### What It Does

- **Invoice Management**: Multi-step invoice submission wizard with OCR-based data extraction
- **Claims Processing**: Track claims through various legal stages (submission → court → enforcement)
- **Company Credit Checks**: Dual-access model:
  - **For Rechtfix users**: Included in Standard/Pro subscription plans
  - **For Bonix users**: One-time purchase via dedicated portal
- **Subscription Management**: Tiered subscription plans with Stripe payment integration (Rechtfix)
- **Multi-tenant Support**: Role-based access control (Admin/User/User_Bonix) with team collaboration features
- **Partner Network**: Manage insurance and banking partners

---

## 🏗️ Architecture

The project follows **Clean Architecture** principles with clear separation of concerns:

```
┌─────────────────────────────────────────────────┐
│           Oikos.Web (Presentation)              │
│   Blazor Server Components, Controllers, UI    │
└───────────────────┬─────────────────────────────┘
                    │
┌───────────────────▼─────────────────────────────┐
│      Oikos.Application (Business Logic)         │
│      Services, DTOs, Validation                 │
└───────────────────┬─────────────────────────────┘
                    │
          ┌─────────┴─────────┐
          ▼                   ▼
┌──────────────────┐   ┌──────────────────┐
│ Oikos.Infrastructure │   │   Oikos.Domain   │
│    Data Access   │   │     Entities     │
└─────────┬────────┘   └─────────┬────────┘
          │                      │
          └─────────┬────────────┘
                    ▼
┌─────────────────────────────────────────────────┐
│         Oikos.Common (Shared Kernel)            │
│    Helpers, Constants, Resources, Extensions    │
└─────────────────────────────────────────────────┘
```

### Layer Responsibilities

| Layer | Responsibility | Key Components |
|-------|---------------|----------------|
| **Domain** | Core business entities and rules | `User`, `Invoice`, `Subscription`, `CompanyCheckRequest` |
| **Application** | Business logic and use cases | Services: Authentication, Invoice Management, Subscription |
| **Infrastructure** | Data persistence and external APIs | EF Core DbContext, CreditSafe Client, Email Service |
| **Common** | Shared shared kernel and utilities | Helpers (Name, Channel), Constants, Localization Resources |
| **Web** | User interface and API endpoints | Blazor pages, Razor components, Controllers |

---

## ✨ Key Features

### 1. Invoice Management
- **Wizard-based Submission**: Multi-step form for invoice upload and data entry
- **OCR Extraction**: Automatic invoice data extraction using heuristic algorithms
- **Stage Tracking**: Track invoices through 12+ legal stages
  - Eingang (Submission)
  - Status Meldung (Status Update)
  - Final geprüft (Final Review)
  - Mahnantrag (Court Dunning)
  - Vollstreckungstitel (Enforcement Title)
- **Timeline View**: Visual history of invoice progress
- **Admin Dashboard**: Comprehensive overview with status-based filtering

### 2. Bonix - Company Credit Check Portal

**Bonix** ([bonix-auskunft.de](https://bonix-auskunft.de)) is a dedicated company credit check service running on the same platform with its own branding, domain, and user flow.

#### Key Features:
- **Anonymous Quick Checks**: One-time company checks with Stripe payment (€19.99 per report)
- **User Registration & Login**: Create account to access check history and past reports
- **Dedicated Dashboard**: User_Bonix role with simplified interface focused on company checks
- **CreditSafe Integration**: Real-time company search and credit report retrieval for German companies
- **SEPA Mandate Generation**: Automated PDF generation for payment mandates
- **Secure Report Downloads**: Token-based anonymous download links
- **Dual Email Branding**: Emails sent from `noreply@bonix-auskunft.de`
- **Separate Stripe Account**: Independent payment processing for Bonix transactions
- **Report History**: Registered users can view and re-download previous reports

#### User Journey:
1. Anonymous user visits bonix-auskunft.de
2. Quick company check flow with company search
3. Stripe checkout for one-time payment
4. Download report via secure token link
5. Optional: Register account to save history and access past reports

### 3. Company Credit Checks (Rechtfix Integration)
- **CreditSafe Integration**: Search and purchase company credit reports
- **Subscription-Based Access**: Included in Standard (3/month) and Pro (5/month) plans
- **Report Management**: Store and retrieve purchased reports
- **Secure Download Links**: Token-based report access

### 4. User & Subscription Management (Rechtfix)
- **Three-Tier Plans**:
  - **Basic**: 5 claims/month, €9/month
  - **Standard**: 10 claims + 3 company checks/month, €13/month
  - **Pro**: 15 claims + 5 company checks/month, €20/month
- **Stripe Integration**: Automated billing and webhook handling for recurring subscriptions
- **Team Seats**: Multi-user collaboration (Standard/Pro plans)
- **Usage Tracking**: Monthly claim and check limits

### 5. Authentication & Authorization
- **JWT-based Authentication**: Secure token-based auth with RSA signing
- **Type-Safe Role Management**: Enum-based roles (`RoleNames` enum in `Oikos.Common`)
  - **Admin**: Full system access, manages all invoices, users, and settings
  - **User**: Rechtfix customers with subscription-based invoice management and company checks
  - **User_Bonix**: Bonix customers with access only to company credit check features
- **Single Role Assignment**: Each user has exactly one role for simplified permission management
- **Context-Aware Authentication**: Login redirects to appropriate dashboard based on user role
- **Password Management**: Reset tokens with email verification and context-based email templates
- **Privacy Compliance**: GDPR-compliant user consent tracking

### 6. Partner Management
- **Insurance Partners**: Manage insurance provider partnerships
- **Banking Partners**: Banking partner network for customer referrals

---

## 🛠️ Technology Stack

### Backend
- **.NET 9.0**: Latest ASP.NET Core framework
- **Blazor Server**: Interactive server-side rendering
- **Entity Framework Core 9.0**: ORM with multi-database support
- **Serilog**: Structured logging framework

### Frontend
- **MudBlazor 8.7**: Material Design component library
- **Blazor-ApexCharts**: Interactive charts and data visualization
- **Cropper.Blazor**: Image cropping functionality
- **MudMarkdown**: Markdown rendering support

### Database Support
- **SQLite** (default for development)
- **SQL Server**
- **PostgreSQL**
- **MySQL**

### Third-Party Integrations
- **Stripe**: Dual integration for different business models
  - **Stripe (Rechtfix)**: Recurring subscription management
  - **Stripe_Bonix**: One-time payment processing for company checks
- **CreditSafe API**: Company credit checks and reports for German companies
- **Certifier.io**: Digital certification services
- **SMTP**: Dual email configuration for branded communications
  - **Email**: noreply@online-mahnantraege.de (Rechtfix)
  - **EmailBonix**: noreply@bonix-auskunft.de (Bonix)

### Development Tools
- **Mapster**: Object-to-object mapping
- **PdfSharpCore**: PDF generation
- **PdfPig**: PDF parsing and OCR
- **RestSharp**: HTTP client for API calls

---

## 🚀 Getting Started

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- Supported OS: Windows, Linux, macOS
- Git for version control
- (Optional) Visual Studio 2022 or JetBrains Rider

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/<your-org>/oikos-baden.git
   cd oikos-baden
   ```

2. **Restore dependencies**
   ```bash
   dotnet restore
   ```

3. **Update database connection** (optional)
   
   Edit `Oikos.Web/appsettings.json`:
   ```json
   "Application": {
     "DatabaseProvider": "Sqlite",
     "ConnectionString": "Data Source=./DB/Oikos.db"
   }
   ```

4. **Run the application**
   ```bash
   dotnet run --project Oikos.Web
   ```

5. **Access the application**
   
   Open your browser to: `http://localhost:37219`

### Default Credentials

#### Admin Account
- **Username**: `ModernPaper`
- **Password**: `Start2026!`
- **Role**: Admin (full system access)

#### Test User Account
- **Email**: `test@ModernPaper.de`
- **Password**: `Test1234!`
- **Role**: User (standard features + active subscription)

---

## 📁 Project Structure

```
oikos-baden/
├── Oikos.Domain/                    # Core domain layer
│   ├── Entities/
│   │   ├── Invoice/                 # Invoice, InvoiceStage, InvoiceStageHistory
│   │   ├── Rbac/                    # User, Role, UserRole
│   │   ├── Subscription/            # Subscription, SubscriptionPlan, UserSubscription
│   │   ├── CompanyCheck/            # CompanyCheckRequest
│   │   ├── Partner/                 # Partner entities
│   │   ├── Setting/                 # Setting, UserSetting
│   │   └── Log/                     # LoginLog, PasswordResetToken
│   ├── Enums/                       # InvoicePrimaryStatus, SubscriptionStatus
│   └── Constants/                   # Domain constants
│
├── Oikos.Application/               # Application business logic
│   ├── Services/
│   │   ├── Authentication/          # AuthenticationService, PasswordResetService
│   │   ├── Registration/            # RegistrationService
│   │   ├── Invoice/                 # InvoiceSubmissionService, InvoiceManagementService
│   │   ├── CompanyCheck/            # CompanyCheckWizardService
│   │   ├── Subscription/            # SubscriptionPlanService, SubscriptionReportService
│   │   ├── User/                    # UserManagementService, UserProfileService
│   │   ├── Role/                    # RoleManagementService
│   │   ├── Partner/                 # PartnerService
│   │   ├── Email/                   # Email services
│   │   ├── Stripe/                  # StripeWebhookService
│   │   └── Dashboard/               # DashboardService
│   └── Data/                        # IAppDbContext interface
│
├── Oikos.Infrastructure/            # Infrastructure implementation
│   ├── Data/
│   │   ├── OikosDbContext.cs        # EF Core DbContext
│   │   ├── InitialDataSeeder.cs     # Database seeding
│   │   └── DatabaseExtension.cs     # Database initialization
│   ├── Migrations/                  # EF Core migrations
│   ├── CompanyCheck/                # CreditSafe API client
│   ├── BackgroundServices/          # Invoice OCR background service
│   ├── Email/                       # SMTP email implementation
│   ├── Security/                    # Password hashing, JWT generation
│   └── Services/                    # Infrastructure service implementations
│
├── Oikos.Common/                    # Shared kernel
│   ├── Constants/
│   │   └── RoleNames.cs             # Type-safe role enum (Admin, User, User_Bonix)
│   │                                # with extension methods for conversion
│   ├── Helpers/                     # Utility classes
│   ├── Resources/                   # Localization resources (de-DE, en-US)
│   └── Extension/                   # Extension methods
│
└── Oikos.Web/                       # Presentation layer
    ├── Components/
    │   ├── Pages/
    │   │   ├── User/                # Rechtfix user pages (Role: User)
    │   │   │   ├── Dashboard.razor
    │   │   │   ├── Invoices/        # Invoice list and details
    │   │   │   ├── NewInvoiceWizard/
    │   │   │   ├── CompanyChecks/
    │   │   │   ├── BankPartners/
    │   │   │   └── InsurancePartners/
    │   │   ├── User_Bonix/          # Bonix user pages (Role: User_Bonix)
    │   │   │   ├── Dashboard.razor  # Simplified dashboard for Bonix users
    │   │   │   ├── QuickCompanyCheck.razor # Anonymous check flow
    │   │   │   ├── QuickCheckSuccess.razor # Payment success page
    │   │   │   └── CompanyChecks/   # Check history and details
    │   │   │       ├── Index.razor  # List of user's checks
    │   │   │       ├── History.razor
    │   │   │       └── Success.razor
    │   │   ├── Admin/               # Admin panel (Role: Admin)
    │   │   │   ├── Dashboard.razor
    │   │   │   ├── Invoices/        # Invoice management
    │   │   │   ├── InvoiceStages/   # Stage configuration
    │   │   │   ├── User/            # User management
    │   │   │   ├── Role/            # Role management
    │   │   │   ├── Subscription/    # Subscription management
    │   │   │   └── Setting/         # System settings
    │   │   ├── Login.razor
    │   │   └── ForgotPassword.razor
    │   └── Layout/
    │       ├── MainLayout.razor
    │       └── NavMenus/            # User and Admin navigation menus
    ├── Controllers/                 # API controllers
    │   ├── StripeWebhookController.cs      # Rechtfix subscription webhooks
    │   └── StripeBonixWebhookController.cs # Bonix payment webhooks
    ├── Program.cs                   # Application startup
    ├── appsettings.json             # Configuration
    └── wwwroot/                     # Static assets
```

---

## ⚙️ Configuration

### Application Settings

The main configuration file is `Oikos.Web/appsettings.json`:

```json
{
  "Application": {
    "DatabaseProvider": "Sqlite",
    "ConnectionString": "Data Source=/var/www/rechtfix/data/Oikos.db"
  },
  "Stripe": {
    "ApiKey": "sk_live_...",
    "WebhookSecret": "whsec_..."
  },
  "Stripe_Bonix": {
    "ApiKey": "sk_live_...",
    "WebhookSecret": "whsec_..."
  },
  "Bonix": {
    "ReportPrice": 19.99,
    "Currency": "EUR",
    "CreditSafeBaseUrl": "https://connect.creditsafe.com/v1/",
    "CreditSafeUsername": "buero@oikos-baden-baden.de",
    "CreditSafePassword": "***",
    "CreditSafeDefaultCountry": "DE"
  },
  "Email": {
    "SmtpHost": "webmail.your-server.de",
    "SmtpPort": 587,
    "SmtpUsername": "noreply@online-mahnantraege.de",
    "SmtpPassword": "***",
    "SenderAddress": "noreply@online-mahnantraege.de",
    "SenderName": "online-mahnantraege.de",
    "EnableSsl": true,
    "ResetTokenExpirationMinutes": 60
  },
  "EmailBonix": {
    "SmtpHost": "webmail.your-server.de",
    "SmtpPort": 587,
    "SmtpUsername": "noreply@bonix-auskunft.de",
    "SmtpPassword": "***",
    "SenderAddress": "noreply@bonix-auskunft.de",
    "SenderName": "bonix-auskunft.de",
    "EnableSsl": true,
    "SupportEmail": "support@bonix-auskunft.de"
  }
}
```

### Database Configuration

The application supports multiple database providers. Change the `DatabaseProvider` in `appsettings.json`:

**SQLite (Development)**
```json
{
  "Application": {
    "DatabaseProvider": "Sqlite",
    "ConnectionString": "Data Source=./DB/Oikos.db;Cache=Shared;Pooling=True"
  }
}
```

**SQL Server**
```json
{
  "Application": {
    "DatabaseProvider": "SqlServer",
    "ConnectionString": "Server=127.0.0.1,1433;Database=Oikos;User ID=sa;Password=YourPassword"
  }
}
```

**PostgreSQL**
```json
{
  "Application": {
    "DatabaseProvider": "PostgreSQL",
    "ConnectionString": "Host=127.0.0.1;Port=5432;Database=Oikos;Username=postgres;Password=YourPassword"
  }
}
```

**MySQL**
```json
{
  "Application": {
    "DatabaseProvider": "MySQL",
    "ConnectionString": "Server=127.0.0.1;Port=3306;Database=Oikos;Uid=root;Pwd=YourPassword"
  }
}
```

### Dual Email Configuration

The application uses **two separate email configurations** for branded communications:

#### Rechtfix Email (default)
```json
{
  "Email": {
    "SmtpHost": "webmail.your-server.de",
    "SmtpPort": 587,
    "SmtpUsername": "noreply@online-mahnantraege.de",
    "SmtpPassword": "***",
    "SenderAddress": "noreply@online-mahnantraege.de",
    "SenderName": "online-mahnantraege.de",
    "EnableSsl": true,
    "ResetTokenExpirationMinutes": 60
  }
}
```

#### Bonix Email  
```json
{
  "EmailBonix": {
    "SmtpHost": "webmail.your-server.de",
    "SmtpPort": 587,
    "SmtpUsername": "noreply@bonix-auskunft.de",
    "SmtpPassword": "***",
    "SenderAddress": "noreply@bonix-auskunft.de",
    "SenderName": "bonix-auskunft.de",
    "EnableSsl": true,
    "SupportEmail": "support@bonix-auskunft.de"
  }
}
```

The application automatically selects the correct email configuration based on:
- **User role**: User_Bonix users receive emails from bonix-auskunft.de
- **Context**: Company check emails use Bonix branding
- **Source parameter**: Password reset emails use context-aware sender detection

### Initial Data Seeding

On first run, the application automatically seeds:

1. **Roles**: Admin, User, User_Bonix (managed via type-safe `RoleNames` enum)
2. **Admin User**: ModernPaper / Start2026!
3. **Test User**: test@ModernPaper.de / Test1234! (with active subscription)
4. **Invoice Stages**: 12 predefined legal stages
5. **Subscription Plans**: Basic, Standard, Pro
6. **JWT Settings**: RSA key pair for token signing

### Localization

The application supports two languages:
- **German (de-DE)**: Default language
- **English (en-US)**: Secondary language

Resource files are located in `Oikos.Common/Resources/`.

---

## 🌐 Production Deployment

### Current Production Environment

| Component | Value |
|-----------|-------|
| **Domain** | [my.online-mahnantraege.de](https://my.online-mahnantraege.de) |
| **Hosting** | Hetzner Cloud (Germany) |
| **OS** | Ubuntu Linux (x64) |
| **Runtime** | ASP.NET Core Runtime 9.0 |
| **Web Server** | Nginx (reverse proxy) |
| **Database** | SQLite with WAL mode |
| **SSL/TLS** | Let's Encrypt (via Certbot) |
| **Application Path** | `/var/www/rechtfix/app` |
| **Data Path** | `/var/www/rechtfix/data` |
| **Service** | `rechtfix.service` (systemd) |
| **Backend Port** | `127.0.0.1:5000` |
| **Public Ports** | 80 (HTTP), 443 (HTTPS) |

### Deployment Workflow

#### 1. Build the Project

```bash
# On your local machine
cd oikos-baden
dotnet publish -c Release --output ./publish
```

The build output will be in: `bin/Release/net9.0/publish/`

#### 2. Upload to Server

Use **WinSCP (SFTP)** or `scp` to upload files:

```bash
scp -r ./bin/Release/net9.0/publish/* user@server:/var/www/rechtfix/app/
```

⚠️ **Important**: Upload the **contents** of the `publish` folder, not the folder itself.

#### 3. Set Permissions

```bash
# SSH into the server
ssh user@server

# Ensure directories exist
sudo mkdir -p /var/www/rechtfix/app
sudo mkdir -p /var/www/rechtfix/data
sudo mkdir -p /var/www/rechtfix/logs

# Set ownership
sudo chown -R www-data:www-data /var/www/rechtfix

# Set permissions
sudo chmod 770 /var/www/rechtfix/app/data
```

#### 4. Restart the Application

```bash
# Reload systemd configuration
sudo systemctl daemon-reload

# Restart the application
sudo systemctl restart rechtfix

# Verify the service is running
sudo systemctl status rechtfix --no-pager
```

#### 5. View Logs

```bash
# View recent logs
sudo journalctl -u rechtfix -n 100 --no-pager

# Follow logs in real-time
sudo journalctl -u rechtfix -f
```

#### 6. Verify Application

```bash
# Test backend directly
curl -I http://127.0.0.1:5000

# Test through Nginx
curl -I https://my.online-mahnantraege.de
```

### SSL Certificate Renewal

Certificates are automatically renewed by Certbot. To manually renew:

```bash
sudo certbot renew
sudo systemctl reload nginx
```

### Nginx Configuration Update

If you modify Nginx configuration:

```bash
# Test configuration
sudo nginx -t

# Reload if valid
sudo systemctl reload nginx
```

### Database Backup

```bash
# Create backup
cp /var/www/rechtfix/data/Oikos.db /backup/Oikos_$(date +%Y%m%d_%H%M%S).db

# Restore from backup
cp /backup/Oikos_20260123_120000.db /var/www/rechtfix/data/Oikos.db
sudo systemctl restart rechtfix
```

---

## 🔧 Development

### Running Locally

```bash
# Development mode with hot reload
dotnet watch run --project Oikos.Web
```

### Database Migrations

```bash
# Create a new migration
dotnet ef migrations add MigrationName --project Oikos.Infrastructure --startup-project Oikos.Web

# Update database
dotnet ef database update --project Oikos.Infrastructure --startup-project Oikos.Web

# Remove last migration (if not applied)
dotnet ef migrations remove --project Oikos.Infrastructure --startup-project Oikos.Web
```

### Building for Production

```bash
# Build optimized release version
dotnet build -c Release

# Publish self-contained
dotnet publish -c Release --output ./publish
```

### Code Style & Architecture

- **Clean Architecture**: Strict layer separation, dependencies flow inward
- **Dependency Injection**: All services registered in `Program.cs`
- **Async/Await**: All I/O operations use async patterns
- **Resource Management**: `using` statements for IDisposable objects
- **Localization**: All user-facing text uses resource files

### Testing

Currently, the project doesn't have automated tests. Verification is done through:
1. Manual UI testing
2. Database inspection
3. Log file analysis

---

## 🔌 API Integration

### Stripe Webhooks

The application uses **two separate Stripe integrations** for different business models:

#### Rechtfix Subscription Webhooks

**Endpoint**: `/api/stripe/webhook`  
**Configuration**: `Stripe` section in appsettings.json

Handles subscription lifecycle events:
- `checkout.session.completed` - New subscription activation
- `customer.subscription.created` - Subscription created
- `customer.subscription.updated` - Plan changes, renewals
- `customer.subscription.deleted` - Subscription cancellation

#### Bonix Payment Webhooks

**Endpoint**: `/api/stripe-bonix/webhook`  
**Configuration**: `Stripe_Bonix` section in appsettings.json

Handles one-time payment events:
- `checkout.session.completed` - Payment confirmation for company check
- Stores payment details in `CompanyCheckRequest.PaymentDetails` (JSON)
- Triggers report generation and delivery

### CreditSafe API

Base URL: `https://connect.creditsafe.com/v1/`

Key endpoints used:
- Company search
- Company report retrieval
- Credit score lookup

### Anonymous Report Download

Download company check reports without authentication:

```
GET /company-checks/report/{token}
```

---

## 📝 Outstanding Items

1. **Automated Testing**: Unit and integration test coverage
2. **API Documentation**: OpenAPI/Swagger documentation for external integrations
3. **Docker Support**: Containerization for easier deployment
4. **Multi-language Expansion**: Additional language support beyond de-DE and en-US
5. **Bonix Enhancements**: Additional features for Bonix portal based on user feedback
6. **Monitoring & Analytics**: Enhanced logging and analytics for business insights

---

## 📄 License

This project is proprietary software owned by Oikos Baden-Baden.

---

## 🤝 Support & Contact

For issues, questions, or feature requests:
- **Email**: buero@oikos-baden-baden.de
- **Website**: [online-mahnantraege.de](https://online-mahnantraege.de)

---

**Version**: 2.0  
**Last Updated**: February 2026  
**ASP.NET Core Version**: 9.0  
**Entity Framework Core Version**: 9.0.5
