<div align="center">

  <img src="NGEOS/wwwroot/images/ngeos-logo.svg" alt="NGEOS Logo" width="110" height="110" />

  # 🌟 NGEOS — Next-Generation NGO & Welfare Platform
  ### *Comprehensive Non-Governmental Charity Alliance & Desktop Management System*

  <p align="center">
    Empowering underprivileged communities worldwide across Education, Healthcare, Food Relief, Elderly & Women Welfare.
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

---

## 🛠️ Tech Stack & Tools

| Component | Technology | Description |
| :--- | :--- | :--- |
| **Backend Framework** | ![.NET](https://img.shields.io/badge/.NET_10-512BD4?style=flat-square&logo=dotnet&logoColor=white) | ASP.NET Core MVC with C# |
| **Database Engine** | ![SQL Server](https://img.shields.io/badge/SQL_Server-CC292B?style=flat-square&logo=microsoftsqlserver&logoColor=white) | Microsoft SQL Server (Local & Production ready) |
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

### Option A: Automatic Code-First Migration (Recommended)
1. Open `NGEOS/appsettings.json` and verify your SQL Server connection string:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=localhost;Database=NGEOSDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
   }
   ```
2. Apply migrations automatically via .NET CLI:
   ```bash
   dotnet ef database update --project NGEOS
   ```

### Option B: Using SQL Server Management Studio (SSMS)
1. Launch **SQL Server Management Studio (SSMS)** and connect to `localhost` (or `.\SQLEXPRESS`).
2. Open the script file provided in [`Database/NGEOSDb.sql`](Database/NGEOSDb.sql).
3. Execute the script to create `NGEOSDb` along with all required tables, foreign keys, and default seed data.

---

## ▶️ Running the Application

### 🌐 Web Mode (Visual Studio / Terminal)
To run in development mode:
```bash
cd NGEOS
dotnet run
```
Open your browser and navigate to:
```text
http://localhost:5125  or  http://localhost:5000
```

---

## 🖥️ Desktop Application Mode

NGEOS includes built-in standalone desktop packaging that runs locally without requiring a browser window:

1. **Launch Desktop App Silently:**
   - Run [`Launch-NGEOS.vbs`](Launch-NGEOS.vbs) or double-click the **NGEOS** Desktop shortcut.
   - It checks backend health, starts the background server silently if needed, and launches Microsoft Edge/Chrome in dedicated App Mode (`--app`).

2. **Single-Click Installer (`NGEOS-Setup.exe`):**
   - Located at [`NGEOS-Setup.exe`](NGEOS-Setup.exe) (and downloadable directly from the website footer).
   - Generates the desktop shortcut with the official embedded `.ico` and opens the application immediately without ZIP extraction.

---

## 🔒 Authentication & Security

- **Cookie Authentication:** Encrypted HTTP-only sliding session cookies (`NGEOS_AuthSession`).
- **Role-Based Access Control (RBAC):** Distinct authorization policies for `Admin`, `User`, and `NGO Partner`.
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
├── publish/                      # Compiled Release binaries for instant offline deployment
├── Create-Desktop-Shortcut.ps1   # PowerShell automation script for desktop shortcut
├── Launch-NGEOS.vbs              # Zero-console VBScript background launcher
├── NGEOS-Setup.exe               # Standalone WinExe setup installer with embedded icon
├── ngeos-desktop.ico             # Multi-resolution high-res official Windows icon
└── README.md                     # Comprehensive project documentation
```

---

## 👨‍💻 Author

**Syed Muhammad Saim**
- **GitHub:** [@syedmuhammadsaim](https://github.com/syedmuhammadsaim)
- **Email:** syedmuhammadsaim.contact@gmail.com
- **Project Repository:** [NGEOS on GitHub](https://github.com/syedmuhammadsaim/NGEOS)

---

<div align="center">
  <sub>Built with ❤️ for humanitarian welfare, education, and community empowerment.</sub>
</div>
