import os
from pathlib import Path
from typing import Optional
from pydantic_settings import BaseSettings
from dotenv import load_dotenv

# Automatically locate root repository .env
root_dir = Path(__file__).resolve().parent.parent.parent
root_env = root_dir / ".env"
local_env = Path(__file__).resolve().parent.parent / ".env"

if root_env.exists():
    load_dotenv(dotenv_path=root_env, override=False)
if local_env.exists():
    load_dotenv(dotenv_path=local_env, override=True)


class Settings(BaseSettings):
    service_name: str = "NGEOS-Autonomous-AI-Service"
    environment: str = os.getenv("ASPNETCORE_ENVIRONMENT", "Development")
    host: str = os.getenv("AI_SERVICE_HOST", "127.0.0.1")
    port: int = int(os.getenv("AI_SERVICE_PORT", "8001"))
    
    # Internal service-to-service secret token
    internal_api_key: str = os.getenv(
        "AI_SERVICE_SECRET_KEY", 
        "NGEOS_AI_INTERNAL_SHARED_KEY_2026_SECURE_TOKEN_32X"
    )
    
    # ASP.NET Core connection
    dotnet_base_url: str = os.getenv("DOTNET_API_BASE_URL", "http://localhost:5000")
    
    # AI Providers
    primary_provider: str = os.getenv("AI_PRIMARY_PROVIDER", "Groq")
    fallback_provider: str = os.getenv("AI_FALLBACK_PROVIDER", "Gemini")
    
    groq_api_key: str = os.getenv("GROQ_API_KEY", "")
    groq_model: str = os.getenv("GROQ_MODEL", "llama-3.3-70b-versatile")
    
    gemini_api_key: str = os.getenv("GEMINI_API_KEY", "")
    gemini_model: str = os.getenv("GEMINI_MODEL", "gemini-2.0-flash")
    
    # Operational intervals (in minutes)
    donation_monitor_interval_minutes: int = int(os.getenv("DONATION_MONITOR_INTERVAL_MINUTES", "15"))
    security_scan_interval_minutes: int = int(os.getenv("SECURITY_SCAN_INTERVAL_MINUTES", "10"))
    health_check_interval_minutes: int = int(os.getenv("HEALTH_CHECK_INTERVAL_MINUTES", "5"))
    
    timeout_seconds: int = int(os.getenv("CHATBOT_TIMEOUT_SECONDS", "15"))
    log_level: str = os.getenv("LOG_LEVEL", "INFO")

    class Config:
        case_sensitive = False


settings = Settings()
