# NGEOS Autonomous AI Ecosystem - Setup & Execution Guide

## 1. Prerequisites
- **Python 3.14** (or Python 3.10+) installed.
- **.NET 10.0 SDK** installed.
- **SQL Server** running with `NGEOSDb` database.

---

## 2. Python AI Service Setup

1. **Navigate to the AI service directory:**
   ```bash
   cd ngeos_ai_service
   ```

2. **Install dependencies:**
   ```bash
   pip install -r requirements.txt
   ```

3. **Configure Environment (`.env`):**
   The service automatically reads configuration from the repository root `.env` or `ngeos_ai_service/.env`.
   Ensure your API keys are provided:
   ```env
   GROQ_API_KEY=your_groq_api_key_here
   GROQ_MODEL=llama-3.3-70b-versatile
   GEMINI_API_KEY=your_gemini_api_key_here
   GEMINI_MODEL=gemini-2.0-flash
   AI_SERVICE_SECRET_KEY=NGEOS_AI_INTERNAL_SHARED_KEY_2026_SECURE_TOKEN_32X
   DOTNET_API_BASE_URL=http://localhost:5000
   ```

4. **Start the Python Autonomous AI Service:**
   ```bash
   python -m uvicorn app.main:app --host 127.0.0.1 --port 8001 --reload
   ```

---

## 3. ASP.NET Core Main Application Setup

1. **Run the web application:**
   ```bash
   dotnet run --project NGEOS/NGEOS.csproj
   ```

2. **Accessing Portals:**
   - **Main Website:** `http://localhost:5000`
   - **Admin Control Center:** `http://localhost:5000/Admin/AgentControl`
   - **Agent Audit Reports:** `http://localhost:5000/Admin/AgentReports`
   - **Donation Intelligence:** `http://localhost:5000/Admin/DonationIntelligence`
   - **Security Intelligence:** `http://localhost:5000/Admin/SecurityIntelligence`
   - **User Personal AI Assistant:** `http://localhost:5000/User/AiAssistant`
   - **Public Appeal Portal:** `http://localhost:5000/Account/Appeal`

---

## 4. Resilience & Fallback
If the Python service is offline, ASP.NET Core seamlessly falls back to its in-process Groq/Gemini services without crashing or interrupting website users.
