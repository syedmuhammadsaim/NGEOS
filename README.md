<div align="center">



  <img src="NGEOS/wwwroot/images/ngeos-logo.svg" alt="NGEOS Logo" width="110" height="110" />



  # 🌟 NGEOS — Next-Generation NGO & Welfare Platform

  ### *Comprehensive Non-Governmental Charity Alliance & Desktop Management System*



  <p align="center">

    Empowering underprivileged communities worldwide across Education, Healthcare, Food Relief, Elderly & Women Welfare.

  </p>



  <!-- LIVE WEBSITE BADGES -->

  <p align="center">

    <a href="https://ngeos.runasp.net" target="_blank">

      <img src="https://img.shields.io/badge/🌐_LIVE_WEBSITE-ngeos.runasp.net-00C853?style=for-the-badge&logo=googlechrome&logoColor=white" alt="Live Website" />

    </a>

    <a href="https://ngeos.runasp.net" target="_blank">

      <img src="https://img.shields.io/badge/SSL_STATUS-Active_%26_Secured-blue?style=for-the-badge&logo=letsencrypt&logoColor=white" alt="SSL Active" />

    </a>

  </p>



  <!-- BADGES & ICONS -->

  <p align="center">

    <a href="https://dotnet.microsoft.com/">

      <img src="https://img.shields.io/badge/.NET_10-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" alt=".NET" />

    </a>

    <a href="https://learn.microsoft.com/en-us/dotnet/csharp/">

      <img src="https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=csharp&logoColor=white" alt="C#" />

    </a>

    <a href="https://www.microsoft.com/en-us/sql-server">

      <img src="https://img.shields.io/badge/SQL_Server-CC292B?style=for-the-badge&logo=microsoftsqlserver&logoColor=white" alt="SQL Server" />

    </a>

    <a href="https://learn.microsoft.com/en-us/sql/ssms/download-sql-server-management-studio-ssms">

      <img src="https://img.shields.io/badge/SSMS-0078D4?style=for-the-badge&logo=microsoft&logoColor=white" alt="SSMS" />

    </a>

    <a href="https://getbootstrap.com/">

      <img src="https://img.shields.io/badge/Bootstrap_5-7952B3?style=for-the-badge&logo=bootstrap&logoColor=white" alt="Bootstrap 5" />

    </a>

    <a href="https://developer.mozilla.org/en-US/docs/Web/JavaScript">

      <img src="https://img.shields.io/badge/JavaScript-F7DF1E?style=for-the-badge&logo=javascript&logoColor=black" alt="JavaScript" />

    </a>

    <a href="https://firebase.google.com/">

      <img src="https://img.shields.io/badge/Firebase_Auth_OTP-FFCA28?style=for-the-badge&logo=firebase&logoColor=black" alt="Firebase" />

    </a>

    <a href="https://www.microsoft.com/windows">

      <img src="https://img.shields.io/badge/Windows_Desktop_App-0078D6?style=for-the-badge&logo=windows&logoColor=white" alt="Windows App" />

    </a>

  </p>



  <p align="center">

    <a href="https://ngeos.runasp.net"><b>🌐 Live Website</b></a> •

    <a href="#-key-features">Key Features</a> •

    <a href="#%EF%B8%8F-tech-stack--tools">Tech Stack</a> •

    <a href="#-getting-started">Getting Started</a> •

    <a href="#%EF%B8%8F-database-setup-ssms">Database Setup (SSMS)</a> •

    <a href="#%EF%B8%8F-desktop-application-mode">Desktop App</a> •

    <a href="#-authentication--security">Security</a> •

    <a href="#-author">Author</a>

  </p>



</div>



---



## 🌐 Live Production Website



The production version of **NGEOS** is deployed and active with high availability, full HTTPS Let's Encrypt SSL encryption, and integrated MS SQL database hosting:



| Attribute | Details |

| :--- | :--- |

| **Live URL** | 🔗 **[https://ngeos.runasp.net](https://ngeos.runasp.net)** |

| **Cloud Hosting** | MonsterASP.NET Cloud Platform |

| **SSL / HTTPS** | Let's Encrypt 256-bit SSL Certificate (Active & Forced HTTPS) |

| **Authentication** | Google OAuth 2.0 + Email/Password + Firebase Phone SMS OTP |

| **Database** | Remote Cloud MS SQL Database (`db77413`) |



---



## 📖 Overview



**NGEOS** is an enterprise-grade digital charity and non-profit welfare platform designed to unite accredited NGOs, philanthropists, corporate benefactors, and community volunteers under a unified ecosystem. 



Built with **ASP.NET Core (C#)**, **Entity Framework Core**, and **Microsoft SQL Server**, NGEOS supports both modern browser access and **standalone Windows Desktop Application mode** with zero console windows, offline capabilities, and real-time online SMS OTP verification.



---



## 🚀 Key Features



### 1. 🎗️ Causes & Campaigns

- Dynamic charity campaigns with goal targets, real-time raised amounts, and percentage progress bars.

- Categorized relief programs: **Children Welfare, Universal Education, Healthcare & Medical Relief, Disabled Persons, Women Welfare, and Elderly Care**.



### 2. 🌙 Smart Zakat Calculator

- Automated calculation of Zakat based on Islamic guidelines.

- Live inputs for **Gold, Silver, Liquid Cash, Investments, and Business Merchandise** against Nisab threshold.



### 3. 💳 Instant Donations & Receipts

- Streamlined donation workflow with preset denominations ($25, $50, $100, $250) and custom inputs.

- Auto-formatted card details, input masking, and instant digital receipt confirmation.



### 4. 🖥️ Native Windows Desktop Application

- Built-in zero-terminal background launcher (`Launch-NGEOS.vbs`) and single-click executable installer (`NGEOS-Setup.exe`).

- Opens in clean standalone window mode without URL bars, tabs, or black CMD windows.

- Native branding with high-resolution Windows `.ico` packaging.



### 5. 🔐 Dual Forgot Password Verification

- **Email Recovery:** Sends secure, high-entropy 6-digit tokens via Gmail SMTP.

- **SMS Phone Recovery:** Real-time phone number OTP verification powered by **Google Firebase Authentication**.



### 6. 🎨 Elite Visual Experience

- Dark navy topbar and footer with seamless zero-bounce rubber-band overscroll protection.

- Responsive flexbox design with Bootstrap 5, Bootstrap Icons, and AOS scroll animations.



### 7. 🤖 AI Chatbot Assistant (Groq + Gemini Dual Provider)

- **Dual AI Providers:** High-speed primary inference via **Groq Cloud API** (`llama-3.3-70b-versatile`) with automatic failover to **Google Gemini API** (`gemini-2.0-flash`).

- **Live SQL Server Grounding:** Answers are dynamically grounded in live, verified `NGEOSDb` data via read-only EF Core queries (Active Causes, confirmed Donations, Programmes, FAQs, Volunteer Jobs, NGOs).

- **Strict Financial Accuracy:** Only verified transactions (`PaymentStatus == 'Completed'`) are counted ($321.00 USD verified). AI never guesses or fabricates financial figures.

- **Privacy & Security:** Zero PII leakage (card numbers, phone numbers, donor emails strictly excluded). Root-level `.env` configuration protected by `.gitignore`.

- **Multi-Language Intelligence:** Full multi-lingual fluency in **English, Urdu, and Roman Urdu**.



---



## 🛠️ Tech Stack & Tools



| Component | Technology | Description |

| :--- | :--- | :--- |

| **Backend Framework** | ![.NET](https://img.shields.io/badge/.NET_10-512BD4?style=flat-square&logo=dotnet&logoColor=white) | ASP.NET Core MVC with C# |

| **Cloud Hosting** | ![MonsterASP](https://img.shields.io/badge/MonsterASP.NET-00C853?style=flat-square&logo=googlechrome&logoColor=white) | Production Web & Database Cloud Hosting |

| **Database Engine** | ![SQL Server](https://img.shields.io/badge/SQL_Server-CC292B?style=flat-square&logo=microsoftsqlserver&logoColor=white) | Microsoft SQL Server (Local & Cloud Production) |

| **Database Tool** | ![SSMS](https://img.shields.io/badge/SSMS-0078D4?style=flat-square&logo=microsoft&logoColor=white) | SQL Server Management Studio (SSMS) |

| **ORM** | ![EF Core](https://img.shields.io/badge/EF_Core-512BD4?style=flat-square&logo=dotnet&logoColor=white) | Entity Framework Core (Code-First & Migrations) |

| **Frontend Styling** | ![Bootstrap](https://img.shields.io/badge/Bootstrap_5-7952B3?style=flat-square&logo=bootstrap&logoColor=white) | Bootstrap 5.3.3 + Custom Modern Design System |

| **Icons** | ![Bootstrap Icons](https://img.shields.io/badge/Bootstrap_Icons-7952B3?style=flat-square&logo=bootstrap&logoColor=white) | Bootstrap Icons 1.11.3 |

| **Desktop Mode** | ![Windows](https://img.shields.io/badge/Windows-0078D6?style=flat-square&logo=windows&logoColor=white) | PWA Standalone + Silent VBScript/WinExe Launcher |

| **SMS OTP** | ![Firebase](https://img.shields.io/badge/Firebase-FFCA28?style=flat-square&logo=firebase&logoColor=black) | Firebase Authentication Phone SMS Service |

| **Email SMTP** | ![Gmail](https://img.shields.io/badge/Gmail_SMTP-EA4335?style=flat-square&logo=gmail&logoColor=white) | Secure SSL SMTP Mailer Service |



---



## 💻 Getting Started



### 📋 Prerequisites

Ensure you have the following installed on your machine:

- [.NET 10 or .NET 8/9 SDK](https://dotnet.microsoft.com/download)

- [Microsoft SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) (Developer or Express edition)

- [SQL Server Management Studio (SSMS)](https://learn.microsoft.com/en-us/sql/ssms/download-sql-server-management-studio-ssms)

- [Visual Studio 2022](https://visualstudio.microsoft.com/) or [VS Code](https://code.visualstudio.com/)



---



### 📥 1. Clone the Repository

```bash

git clone https://github.com/syedmuhammadsaim/NGEOS.git

cd NGEOS

```



---



## 🗄️ Database Setup (SSMS)



The complete SQL database schema with initial seed data is provided in `Database/NGEOSDb.sql`.



1. Open **SQL Server Management Studio (SSMS)**.

2. Connect to your local SQL Server instance (`.` or `localhost`).

3. Open [`Database/NGEOSDb.sql`](Database/NGEOSDb.sql).

4. Click **Execute** (or press `F5`).

5. The `NGEOSDb` database will be created with all 18 tables:

   - `Users`, `Roles`, `Causes`, `Donations`, `AboutPages`, `ContactMessages`, `Events`, `FAQs`, `JobOpportunities`, `NGOs`, `Partners`, `PasswordResetTokens`, `Programmes`, `ProgrammeInterests`, `TeamMembers`, `UserQueries`, `ZakatCalculations`.



---



### ⚙️ 2. Configuration & AI Setup



#### 🔑 A. AI Chatbot Environment Variables (.env)

NGEOS features a dual-provider AI Assistant grounded in live database data. Configure your API keys in the root `.env` file:



```env

AI_PRIMARY_PROVIDER=Groq



# Provider 1: Groq API Key (https://console.groq.com/keys)

GROQ_API_KEY=your_actual_groq_api_key_here

GROQ_MODEL=llama-3.3-70b-versatile



# Provider 2: Google Gemini API Key (https://aistudio.google.com/app/apikey)

GEMINI_API_KEY=your_actual_gemini_api_key_here

GEMINI_MODEL=gemini-2.0-flash

```



> **Security Note:** The `.env` file is strictly excluded by `.gitignore`. Never commit API keys to GitHub.



#### 🗄️ B. Database Connection String

Update your connection string in `NGEOS/appsettings.json` if using custom SQL credentials:



```json

{

  "ConnectionStrings": {

    "DefaultConnection": "Server=.;Database=NGEOSDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"

  }

}

```



---



### ▶️ 3. Run the Application



#### Using Visual Studio:

- Open `NGEOS/NGEOS.slnx` in Visual Studio 2022.

- Press `F5` to start debugging with IIS Express or Kestrel.



#### Using .NET CLI:

```bash

cd NGEOS

dotnet run

```

Open [http://localhost:5000](http://localhost:5000) or [https://localhost:7179](https://localhost:7179) in your browser.



---



## 🖥️ Desktop Application Mode



NGEOS includes native Windows Desktop Integration:



1. **One-Click Installer:** Run `NGEOS-Setup.exe` to automatically create a desktop shortcut with the official high-resolution icon.

2. **Silent Background Launcher:** Double-click `Launch-NGEOS.vbs` to start the app silently without any CMD or console windows.

3. **Dedicated App Window:** Launches in a native standalone Chrome/Edge application window without browser URL bars or tabs.



---



## 👑 Admin Control Center & Portal Access



NGEOS features a comprehensive **Executive Admin Control Center** at /Admin providing enterprise-grade governance:



| Role | Email Address | Password | Access Rights |

| :--- | :--- | :--- | :--- |

| **Super Administrator** | dmin@ngeos.org | Admin@123 | Full Master Ledger, Causes, NGOs, Programmes, Members & CMS |

| **System Administrator** | 

geos@gmail.com | Admin@123 | Full Administrative & Operations Access |

| **Director Admin** | syedmuhammadsaim.contact@gmail.com | Admin@123 | Master Central Secretariat Access |



> **Quick Access:** Visit [/Account/Login](https://ngeos.runasp.net/Account/Login) and click the **Auto Fill** button under *Admin Credentials* to test administrative features instantly.



---



## 🔒 Authentication & Security



- **Dual Authentication:** Google OAuth 2.0 Single Sign-On + Email & Password authentication.

- **Role-Based Access Control (RBAC):** Admin and User authorization policies (`AdminOnly`, `UserOnly`).

- **Anti-Forgery Validation:** CSRF token verification across all forms and donation checkout requests.

- **Password Hashing:** PBKDF2 / Argon2 cryptographic password hashing via `PasswordHelper`.

- **Firebase SMS OTP Verification:** High-security one-time SMS verification directly to the user's mobile number.



---



## 📂 Project Structure



```text

NGEOS/

├── Database/                     # SQL Server schema and database scripts

│   └── NGEOSDb.sql

├── NGEOS/                        # Core ASP.NET Core MVC Application

│   ├── Controllers/              # MVC Action Controllers (Home, Causes, Donate, Account, etc.)

│   ├── Data/                     # EF Core ApplicationDbContext and DbInitializer

│   ├── Migrations/               # Entity Framework Core database migrations

│   ├── Models/                   # Domain entities (User, Cause, Donation, Programme, etc.)

│   ├── Services/                 # EmailSender, PasswordResetService, OTP helpers

│   ├── ViewModels/               # DTOs and ViewModels

│   ├── Views/                    # Razor Views & Layouts

│   └── wwwroot/                  # Static assets (CSS, JS, Logos, Icons, Manifest)

├── Create-Desktop-Shortcut.ps1   # PowerShell automation script for desktop shortcut

├── Launch-NGEOS.vbs              # Zero-console VBScript background launcher

├── NGEOS-Setup.exe               # Standalone WinExe setup installer with embedded icon

├── ngeos-desktop.ico             # Multi-resolution high-res official Windows icon

└── README.md                     # Comprehensive project documentation

```



---



## 💳 Multi-Payment Gateway Architecture (Pakistani Gateways)

NGEOS integrates four official Pakistani payment gateways configured via a centralized, decoupled provider factory (`IPaymentGateway`, `IPaymentGatewayFactory`):

| Gateway | Supported Channels | Integration Method | Security & Verification |
| :--- | :--- | :--- | :--- |
| **Safepay** | Visa, MasterCard, PayPak | Hosted Checkout / Tracker Session | Server-side verification + Webhook HMAC-SHA256 signature |
| **JazzCash** | Mobile Account, Debit Card, Voucher | Secure Post Form Redirect | HMAC-SHA256 `pp_SecureHash` signature + `DoInquiry` / `DoRefund` API |
| **Easypaisa** | Mobile Wallet, OTC Token, Visa/UnionPay | Hosted Checkout / Token | SHA-256 Checksum verification + `inquire-transaction` API |
| **PayFast** | 1Link, PayPak, UnionPay, Bank Transfer | Access Token Form Checkout | Bearer Token Auth + Server status query + Webhook signature |

### Key Features:
- **Zero Card Data Storage:** No CVV, card numbers, or banking credentials stored on NGEOS servers.
- **Idempotency Protection:** Double-donation prevention on page refreshes or duplicate webhook callbacks.
- **Cryptographic Verification:** Server-side verification on all callbacks before donation completion.
- **Audit-Proof Financial Ledger:** Detailed `PaymentTransactions` ledger with status tracking and refund processing.
- **Zero-Commit Environment Security:** Configuration loaded from root `.env` via `DotEnvLoader` and ignored by Git.

---

## 👨‍💻 Author



**Syed Muhammad Saim**

- **🌐 Live Platform:** [https://ngeos.runasp.net](https://ngeos.runasp.net)

- **GitHub:** [@syedmuhammadsaim](https://github.com/syedmuhammadsaim)

- **Email:** syedmuhammadsaim.contact@gmail.com

- **Project Repository:** [NGEOS on GitHub](https://github.com/syedmuhammadsaim/NGEOS)



---



<div align="center">

  <sub>Built with ❤️ for humanitarian welfare, education, and community empowerment.</sub>

</div>

