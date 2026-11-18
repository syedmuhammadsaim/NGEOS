# NGEOS Autonomous AI Ecosystem - Security & Warning Architecture

## 1. Multi-Layered Threat Defense

The Autonomous Security Engine operates using defense-in-depth:
1. **Deterministic Rule Checks:**
   - Detects known SQL injection fragments (`xp_cmdshell`, `drop table`, `or 1=1--`).
   - Detects automated bot patterns and malicious link payloads.
2. **AI Classification Engine:**
   - Evaluates contextual intent with strict zero-shot evaluation, generating a normalized `risk_score` (0.0 to 1.0).
3. **Prompt Injection Sanitization:**
   - Neutralizes phrases attempting to override system instructions or leak internal server credentials.

---

## 2. Real-Time Warnings & Three-Second Suspension Protocol

- **Level 1 Warning (`risk_score >= 0.50`):**
  - Displays a non-blocking modal warning the user to cease prohibited activity.
- **Level 2 Serious Violation (`risk_score >= 0.85`):**
  - Dispatches a real-time SignalR `SecurityCountdownAlert` to the user's browser.
  - Displays an unclosable full-screen countdown modal (`3 -> 2 -> 1 -> 0`).
  - Sets `User.IsActive = false` immediately upon completion.
  - Terminates session and redirects user to `/Account/Appeal`.
  - Dispatches automated notification email via `IEmailSender`.
  - Appends tamper-resistant `AgentReport` record.

---

## 3. Appeal & Human Review Workflow

- Restricted users can submit their explanation at `/Account/Appeal` (or via `/User/Appeal`).
- Appeals appear in `/Admin/SecurityIntelligence`.
- Platform administrators can **Approve & Restore Account** or **Reject** with a recorded explanation.
