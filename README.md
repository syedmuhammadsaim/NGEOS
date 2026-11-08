<div align="center">

  <img src="NGEOS/wwwroot/images/ngeos-logo.svg" alt="NGEOS Logo" width="115" height="115" />

  # 🌟 NGEOS — Next-Generation NGO & Welfare Platform
  ### *Comprehensive Non-Governmental Charity Alliance, AI Support Assistant & Desktop Management Platform*

  <p align="center">
    Empowering underprivileged communities worldwide across Education, Clean Water, Healthcare, Food Relief, Zakat Management, and Women Empowerment.
  </p>

  <!-- LIVE WEBSITE BADGES -->
  <p align="center">
    <a href="https://ngeos.runasp.net" target="_blank">
      <img src="https://img.shields.io/badge/🌐_LIVE_WEBSITE-ngeos.runasp.net-00C853?style=for-the-badge&logo=googlechrome&logoColor=white" alt="Live Website" />
    </a>
    <a href="https://ngeos.runasp.net" target="_blank">
      <img src="https://img.shields.io/badge/SSL_STATUS-Active_%26_Secured-blue?style=for-the-badge&logo=letsencrypt&logoColor=white" alt="SSL Active" />
    </a>
    <a href="https://github.com/syedmuhammadsaim/NGEOS">
      <img src="https://img.shields.io/badge/GitHub-Repo_Synced-181717?style=for-the-badge&logo=github&logoColor=white" alt="GitHub Repo" />
    </a>
  </p>

  <!-- TECH STACK BADGES -->
  <p align="center">
    <a href="https://dotnet.microsoft.com/"><img src="https://img.shields.io/badge/.NET_10-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" alt=".NET" /></a>
    <a href="https://learn.microsoft.com/en-us/dotnet/csharp/"><img src="https://img.shields.io/badge/C%23_13-239120?style=for-the-badge&logo=csharp&logoColor=white" alt="C#" /></a>
    <a href="https://www.microsoft.com/en-us/sql-server"><img src="https://img.shields.io/badge/SQL_Server-CC292B?style=for-the-badge&logo=microsoftsqlserver&logoColor=white" alt="SQL Server" /></a>
    <a href="https://learn.microsoft.com/en-us/visualstudio/wpf/"><img src="https://img.shields.io/badge/WPF_Desktop-0078D4?style=for-the-badge&logo=windows&logoColor=white" alt="WPF" /></a>
    <a href="https://www.sqlite.org/"><img src="https://img.shields.io/badge/SQLite_Offline-003B57?style=for-the-badge&logo=sqlite&logoColor=white" alt="SQLite" /></a>
    <a href="https://getbootstrap.com/"><img src="https://img.shields.io/badge/Bootstrap_5.3-7952B3?style=for-the-badge&logo=bootstrap&logoColor=white" alt="Bootstrap 5" /></a>
    <a href="https://groq.com/"><img src="https://img.shields.io/badge/Groq_AI-F55036?style=for-the-badge&logo=openai&logoColor=white" alt="Groq" /></a>
    <a href="https://ai.google.dev/"><img src="https://img.shields.io/badge/Google_Gemini-4285F4?style=for-the-badge&logo=google&logoColor=white" alt="Gemini" /></a>
  </p>

  <p align="center">
    <a href="#-live-production-website"><b>🌐 Live Site</b></a> •
    <a href="#-system-authentication--credentials"><b>🔑 Authentication & Admin</b></a> •
    <a href="#%EF%B8%8F-api-keys--environment-variables-setup"><b>⚙️ API Keys Setup</b></a> •
    <a href="#-quick-start--installation"><b>🚀 Quick Start</b></a> •
    <a href="#%EF%B8%8F-database-setup-local-ssms--cloud"><b>🗄️ Database Setup</b></a> •
    <a href="#-desktop-application-mode-100-offline"><b>💻 Desktop Mode</b></a> •
    <a href="#-ai-support-assistant"><b>🤖 AI Assistant</b></a> •
    <a href="#-author"><b>👨‍💻 Author</b></a>
  </p>

</div>

---

## 🌐 Live Production Website

The production version of **NGEOS** is deployed and active with high availability, HTTPS Let's Encrypt SSL encryption, and integrated MS SQL database hosting:

| Attribute | Details |
| :--- | :--- |
| **Live URL** | 🔗 **[https://ngeos.runasp.net](https://ngeos.runasp.net)** |
| **Cloud Host** | MonsterASP.NET Cloud Platform |
| **SSL / HTTPS** | Let's Encrypt 256-bit SSL Certificate (Active & Forced HTTPS) |
| **Authentication** | Google OAuth 2.0 + Email/Password + Firebase Phone SMS OTP |
| **Database** | Remote Cloud MS SQL Database (`db77413`) |
| **Status** | 🟢 **100% Operational & Live** |

---

## 🔑 System Authentication & Credentials

### 👑 Official Administrator Account
NGEOS features **one dedicated master administrator account** for full platform oversight, master ledger auditing, cause management, and CMS control:

| Attribute | Details |
| :--- | :--- |
| **Email Address** | `admin@ngeos.org` |
| **Password** | `Admin@12` *(or `Admin@123`)* |
| **Role & Permissions** | Super Administrator (Full Central Secretariat, Causes, Ledger, NGO Approval & CMS Control) |
| **Admin Portal** | 🔗 **[`https://ngeos.runasp.net/Admin`](https://ngeos.runasp.net/Admin)** |

> 💡 **Admin Login Tip:** Navigate to [`/Account/Login`](https://ngeos.runasp.net/Account/Login) and enter the Admin credentials above to access the Executive Control Center.

---

### 👤 User & Donor Accounts (Self-Registration)
There is **no pre-fixed or demo user account**. All donors, volunteers, and humanitarian supporters create their own authentic personal accounts via self-registration:

1. **Navigate to Register:** Open 🔗 **[`/Account/Register`](https://ngeos.runasp.net/Account/Register)** or click *"Create an Account"* on the login page.
2. **Fill Your Profile:**
   * **Personal Details:** Full Name, Email, Mobile Phone, Residential Address/City.
   * **Professional Details:** Profession / Occupation and Organization / Employer.
   * **Account Security:** Password (minimum 6 characters).
3. **Instant Activation & Auto Sign-In:** Upon clicking *"Create Account"*, the system immediately registers your credentials, signs you in, and redirects straight to your personal **User Dashboard (`/User/Dashboard`)**.
4. **Member Capabilities:**
   * Track your cumulative donations and contribution milestones.
   * Download official tax-exempt PDF receipts for any completed donation.
   * Bookmark welfare causes and enroll in NGO volunteer programmes.
   * Update profile details and manage contact preferences.

---

## ⚙️ API Keys & Environment Variables Setup

NGEOS supports a secure zero-commit architecture. All secret API keys, payment tokens, and mail credentials are kept in a root-level `.env` file (protected by `.gitignore`) or in `appsettings.json`.

### 📍 File Location:
Create a file named `.env` in the root folder of the project (`NGEOS\.env` or root checkout):

```env
# ==============================================================================
# NGEOS PLATFORM CONFIGURATION & API KEYS
# ==============================================================================

# ------------------------------------------------------------------------------
# 1. AI CHATBOT ASSISTANT (Groq Cloud + Google Gemini Dual Engine)
# ------------------------------------------------------------------------------
# Set Primary Provider: 'Groq' or 'Gemini'
AI_PRIMARY_PROVIDER=Groq

# Groq Cloud API Key (Get free at: https://console.groq.com/keys)
GROQ_API_KEY=gsk_your_groq_api_key_here
GROQ_MODEL=llama-3.3-70b-versatile

# Google Gemini API Key (Get free at: https://aistudio.google.com/app/apikey)
GEMINI_API_KEY=AIzaSy_your_gemini_api_key_here
GEMINI_MODEL=gemini-2.0-flash

# Note: If no API keys are provided, NGEOS automatically falls back to its 
# intelligent Grounded Knowledge Engine (works 100% offline in Roman Urdu & English)!

# ------------------------------------------------------------------------------
# 2. EMAIL SMTP (Password Reset & Tax Receipts via Gmail)
# ------------------------------------------------------------------------------
# Create an App Password from Google Account > Security > App Passwords (16 characters)
GMAIL_SMTP_HOST=smtp.gmail.com
GMAIL_SMTP_PORT=587
GMAIL_SMTP_USERNAME=syedmuhammadsaim.contact@gmail.com
GMAIL_SMTP_APP_PASSWORD=your_16_char_google_app_password
GMAIL_SMTP_FROM_EMAIL=syedmuhammadsaim.contact@gmail.com
GMAIL_SMTP_SENDER_NAME=NGEOS Foundation

# ------------------------------------------------------------------------------
# 3. GOOGLE OAUTH 2.0 (Single Sign-On)
# ------------------------------------------------------------------------------
# Setup in Google Cloud Console > APIs & Services > Credentials
GOOGLE_CLIENT_ID=your_google_client_id.apps.googleusercontent.com
GOOGLE_CLIENT_SECRET=your_google_client_secret

# ------------------------------------------------------------------------------
# 4. PAYMENT GATEWAYS (Safepay, JazzCash, Easypaisa, PayFast)
# ------------------------------------------------------------------------------
# Enable Simulation Mode for instant testing without real money (true / false)
PAYMENT_SIMULATION_MODE=true
PAYMENT_GLOBAL_ENVIRONMENT=sandbox
PAYMENT_TIMEOUT_SECONDS=60

# Safepay (https://www.getsafepay.com/)
SAFEPAY_API_KEY=sec_your_safepay_sandbox_key
SAFEPAY_SECRET_KEY=your_safepay_secret_key
SAFEPAY_WEBHOOK_SECRET=your_safepay_webhook_secret

# JazzCash (https://sandbox.jazzcash.com.pk/)
JAZZCASH_MERCHANT_ID=MC12345
JAZZCASH_PASSWORD=your_password
JAZZCASH_INTEGRITY_SALT=your_salt

# Easypaisa (https://easypay.easypaisa.com.pk/)
EASYPAISA_STORE_ID=12345
EASYPAISA_USERNAME=your_username
EASYPAISA_PASSWORD=your_password
EASYPAISA_SECRET_KEY=your_secret_key

# PayFast (https://www.gopayfast.com/)
PAYFAST_MERCHANT_ID=1000
PAYFAST_SECURED_KEY=your_secured_key
```

---

## 🗄️ Database Setup (Local SSMS & Cloud)

### 🖥️ Option A: Local SQL Server via SSMS
1. Open **SQL Server Management Studio (SSMS)**.
2. Connect to your local server instance (`.` or `localhost`).
3. Open [`Database/NGEOSDb.sql`](Database/NGEOSDb.sql).
4. Click **Execute** (`F5`).
5. Database `NGEOSDb` will be created with all 19 relational tables, indexes, and initial data.

### ☁️ Option B: MonsterASP Cloud Database
1. Log in to your MonsterASP Control Panel.
2. Open **Database** ➔ click on your database ➔ open **Run T-SQL**.
3. Copy and paste the contents of [`Database/RUN_THIS_ON_MONSTERASP.sql`](Database/RUN_THIS_ON_MONSTERASP.sql).
4. Click **Run / Execute** to apply all idempotent tables and schema patches safely.

---

## 🚀 Quick Start & Installation

### 📋 Prerequisites
* [.NET 10 or .NET 8 SDK](https://dotnet.microsoft.com/download)
* [Microsoft SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) (Express or Developer)
* [Visual Studio 2022](https://visualstudio.microsoft.com/) or [VS Code](https://code.visualstudio.com/)

### 1. Clone the Repository
```bash
git clone https://github.com/syedmuhammadsaim/NGEOS.git
cd NGEOS
```

### 2. Configure Connection String
In `NGEOS/appsettings.json`:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=.;Database=NGEOSDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
}
```

### 3. Run the Web Application
```bash
cd NGEOS
dotnet run
```
* **HTTP:** [http://localhost:5125](http://localhost:5125)
* **HTTPS:** [https://localhost:7179](https://localhost:7179)

---

## 💻 Desktop Application Mode (100% Offline)

NGEOS offers two flexible desktop modes:

### 🌟 Mode 1: Full-Stack Standalone Offline Launcher (Exact Website as App)
Run the full website interface with 100% of its animations, chatbot, and forms as a standalone Windows Desktop App:
* Simply double-click **`NGEOS Full Desktop App.lnk`** on your desktop, OR run **`NGEOS-Desktop-Silent.vbs`**.
* It starts the local offline server in the background and opens in **Native Windows App Mode** (Edge/Chromium standalone window without browser URL bars, tabs, or address bars).
* Completely usable offline without internet!

### 🖥️ Mode 2: Dedicated WPF Desktop Management Suite
For high-volume offline management, auditing, and ledger operations:
* Path: `desktop_publish/NGEOS.Desktop.exe` (or extract `NGEOS-Desktop-v1.0.0.zip`).
* **Features:**
  * Embedded local **SQLite database (`ngeos_offline.db`)** that stores all causes, donations, queries, and users.
  * **Excel (.xlsx) Export** & Official Printable Tax-Exempt Audit Reports.
  * Offline cause creation and donation logging with automatic 2-way cloud sync when internet connects.
  * Light / Dark Mode theme switcher.

---

## 🤖 AI Support Assistant

NGEOS features an enterprise-grade AI chatbot assistant available at all times on the bottom right corner of the screen:

* **Dual Cloud LLMs:** Groq Cloud (`llama-3.3-70b-versatile`) with automatic failover to Google Gemini (`gemini-2.0-flash`).
* **Grounded Knowledge Fallback:** If API keys are missing or quota is exhausted, NGEOS automatically switches to its local Grounded Knowledge Engine (`providerUsed: "NGEOS Grounded Knowledge"`).
* **Multi-Language Fluency:** Answers accurately in **Roman Urdu**, **Urdu script**, and **English**.
* **Financial Accuracy:** Dynamically computes verified amounts strictly from `PaymentStatus == 'Completed'` transactions.

---

## 📱 Mobile-Responsive Executive Admin Center

The Admin Center at `/Admin` is built with a touch-first architecture:
* **Adaptive Navigation:** Clean docked sidebar on desktop, transitioning into a smooth slide-out drawer on mobile.
* **Smart Toggle:** Header menu button dynamically flips from `☰` to `✕` when opened.
* **Zero-Blur Backdrop:** Clean, non-blocking overlay allows full visibility of background stats without CPU/GPU lag.
* **Touch-Optimized:** `touch-action: manipulation` guarantees instant response on modern phones (e.g., Samsung Galaxy, iPhone, Pixel).

---

## 💳 Supported Payment Gateways

| Gateway | Channels | Verification |
| :--- | :--- | :--- |
| **Safepay** | Visa, MasterCard, PayPak | Hosted Checkout + Webhook HMAC-SHA256 signature |
| **JazzCash** | Mobile Account, Debit Card, Voucher | Form Redirect + HMAC-SHA256 `pp_SecureHash` signature |
| **Easypaisa** | Mobile Wallet, OTC Token, Card | Hosted Checkout + SHA-256 Checksum verification |
| **PayFast** | 1Link, PayPak, UnionPay, Bank Transfer | Bearer Token Auth + Server status inquiry |

---

## 📂 Project Structure

```text
NGEOS/
├── Database/
│   ├── NGEOSDb.sql                   # Full SQL Server master database schema
│   └── RUN_THIS_ON_MONSTERASP.sql    # Idempotent cloud migration script
├── NGEOS/                            # Core ASP.NET Core Full-Stack Application
│   ├── Controllers/                  # MVC & API Controllers (Admin, Causes, Donate, Chatbot, etc.)
│   ├── Data/                         # EF Core DbContext & Initializer
│   ├── Hubs/                         # SignalR Realtime Hubs
│   ├── Models/                       # Database Entities & Domain Models
│   ├── Services/                     # AI Providers, Grounding, Payment Gateways, PDF Receipt Engine
│   ├── Views/                        # Razor Views & Layouts
│   └── wwwroot/                      # CSS, JavaScript, Images, Brand SVG Assets
├── NGEOS.Desktop/                    # WPF .NET Desktop Application
│   ├── Data/                         # Offline SQLite DbContext
│   ├── Models/                       # Local Entities
│   ├── Services/                     # Offline Sync, Realtime, Report Services
│   ├── ViewModels/                   # MVVM ViewModels
│   └── Views/                        # WPF XAML Views (Admin & User Dashboards, Ledger, Settings)
├── desktop_publish/                  # Published ready-to-run desktop executable (NGEOS.Desktop.exe)
├── NGEOS-Desktop-Launcher.bat        # Standalone Full-Stack Offline Desktop Launcher
├── NGEOS-Desktop-Silent.vbs          # Silent background execution script
└── README.md                         # Project documentation
```

---

## 👨‍💻 Author

**Syed Muhammad Saim**
* **🌐 Live Platform:** [https://ngeos.runasp.net](https://ngeos.runasp.net)
* **GitHub:** [@syedmuhammadsaim](https://github.com/syedmuhammadsaim)
* **Email:** syedmuhammadsaim.contact@gmail.com
* **Repository:** [syedmuhammadsaim/NGEOS](https://github.com/syedmuhammadsaim/NGEOS)

---

<div align="center">
  <sub>Built with ❤️ for humanitarian welfare, education, healthcare, and transparent non-profit governance.</sub>
</div>
