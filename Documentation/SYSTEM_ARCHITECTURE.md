# NGEOS Autonomous AI Ecosystem - System Architecture

## 1. High-Level Architecture Overview

The **NGEOS Autonomous AI Ecosystem** is built as an enterprise, dual-runtime architecture combining:
1. **ASP.NET Core 10 Web Platform (C# 13 / EF Core 10 / SQL Server)**:
   - Primary user-facing portal, authentication, database transactions, ledger accounting, and SignalR real-time communications.
2. **Python Autonomous AI Service (Python 3.14 / FastAPI / APScheduler)**:
   - Houses the **NGEOS Admin Agent** and **NGEOS User Agent**.
   - Connects to primary provider **Groq** (`llama-3.3-70b-versatile`) with automatic failover to **Google Gemini** (`gemini-2.0-flash`).
   - Runs background schedulers for automated financial audits, website health checks, and security scans.
3. **Bridge Communication**:
   - Secure REST APIs authenticated with the constant-time `X-NGEOS-Internal-Key` cryptographic header.
   - Transparent in-process fallback in ASP.NET Core ensures the website remains 100% functional even if the Python service is offline.

```
┌────────────────────────────────────────────────────────────────────────┐
│                        NGEOS APPLICATION ECOSYSTEM                     │
├──────────────────────────────────────┬─────────────────────────────────┤
│    ASP.NET Core 10 Web Platform      │    Python AI Autonomous Service │
│  • Cookie & Google OAuth Auth        │  • FastAPI REST Endpoints       │
│  • SignalR Realtime (/hubs/ngeos)    │  • Dual AI: Groq ➔ Gemini       │
│  • EF Core 10 SQL Server Models      │  • APScheduler Background Jobs  │
│  • Agent Control Center & Reports    │  • Security Evaluation Engine   │
├──────────────────────────────────────┴─────────────────────────────────┤
│            Secure Internal Bridge (HMAC X-NGEOS-Internal-Key)          │
└────────────────────────────────────────────────────────────────────────┘
```

---

## 2. Core Components

### 2.1 Agent 1: NGEOS Admin Agent
- **Role:** Autonomous management, donation intelligence, campaign auditing, website health, and reporting.
- **Capabilities:**
  - Real-time funds mobilization tracking across periods (Today, Week, Month, Year).
  - Channel distribution analytics (Safepay, JazzCash, Easypaisa, PayFast).
  - Campaign performance tracking against financial targets.
  - Automated tamper-resistant audit report generation (`AgentReport`).

### 2.2 Agent 2: NGEOS User Agent
- **Role:** Personal AI Assistant for registered donors and members.
- **Languages Supported:**
  - English
  - Urdu (اردو)
  - Roman Urdu (e.g., *"Aap ka bohot shukriya, NGEOS mein khushamdeed!"*)
- **Capabilities:**
  - Guiding donors to urgent campaigns.
  - Explaining Zakat calculation rules.
  - Assisting with registration, login, and donation navigation.
  - Total isolation: Can never access or leak another donor's records.

### 2.3 Security Engine & Three-Second Suspension Protocol
- **Activity Classification:** Scans for SQL injection patterns, spam links, bot behavior, and credential stuffing.
- **Risk Thresholds:**
  - `RiskScore >= 0.50`: Emits real-time `SecurityWarningAlert` to the user.
  - `RiskScore >= 0.85`: Verified serious violation triggers the **Three-Second Countdown Modal** (`SecurityCountdownAlert`) via SignalR, followed by immediate reversible account restriction (`IsActive = false`), suspension email, and redirect to the Appeal interface (`/Account/Appeal`).
- **Human Review Safeguard:** Suspended users can submit an appeal explaining their case, reviewed by administrators at `/Admin/SecurityIntelligence`.

---

## 3. Database Schema Extensions

1. `AgentConfigurations`: Dynamic flags for Admin/User agents, background automation pause, and emergency kill-switch.
2. `AgentReports`: Append-only audit records with SHA256 integrity hash verification.
3. `AgentSecurityEvents`: Incident log tracking violation categories, risk scores, and detections.
4. `AgentUserRestrictions`: Reversible account restrictions with automated email logs.
5. `AgentAppeals`: Registered user explanations and human administrative decisions.
6. `AgentConversations` & `AgentMessages`: Session-isolated conversation threads.
7. `AgentAutomationJobs`: Execution tracking, timestamps, and error recovery metrics.
