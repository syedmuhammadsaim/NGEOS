from contextlib import asynccontextmanager
from typing import Dict, Any, List, Optional
from fastapi import FastAPI, Depends, HTTPException, Body
from pydantic import BaseModel
from app.config import settings
from app.logger import setup_logging, logger
from app.security import verify_internal_api_key
from app.agents.admin_agent import admin_agent
from app.agents.user_agent import user_agent
from app.agents.security_engine import security_engine
from app.automation.scheduler import start_background_automation, stop_background_automation

@asynccontextmanager
async def lifespan(app: FastAPI):
    setup_logging()
    logger.info("ngeos_ai_service_starting", host=settings.host, port=settings.port)
    start_background_automation()
    yield
    logger.info("ngeos_ai_service_stopping")
    stop_background_automation()

app = FastAPI(
    title="NGEOS Autonomous AI Ecosystem API",
    version="1.0.0",
    description="Dual AI Agents (Admin & User) | Intelligent Automation | Security Warning Engine",
    lifespan=lifespan
)

# ----------------- Request Models -----------------
class AdminQueryRequest(BaseModel):
    query: str
    context: Optional[Dict[str, Any]] = None

class UserChatRequest(BaseModel):
    userId: Optional[int] = None
    userName: str = "Valued Member"
    message: str
    userContext: Optional[Dict[str, Any]] = None
    history: Optional[List[Dict[str, str]]] = None

class SecurityEvaluateRequest(BaseModel):
    userId: Optional[int] = None
    userEmail: Optional[str] = None
    ipAddress: Optional[str] = None
    activityType: str = "GenericUserAction"
    payload: str

class AgentControlRequest(BaseModel):
    action: str  # "enable_admin", "disable_admin", "enable_user", "disable_user", "pause_automation", "resume_automation", "kill_switch"

# ----------------- Endpoints -----------------

@app.get("/health")
async def health_check():
    return {
        "status": "Healthy",
        "service": settings.service_name,
        "environment": settings.environment,
        "primaryProvider": settings.primary_provider,
        "fallbackProvider": settings.fallback_provider
    }

@app.post("/api/v1/admin/query", dependencies=[Depends(verify_internal_api_key)])
async def execute_admin_agent_query(req: AdminQueryRequest):
    """
    Executes autonomous executive analysis and donation intelligence for platform administrators.
    """
    try:
        result = await admin_agent.answer_admin_query(req.query, req.context)
        return result
    except Exception as ex:
        logger.error("admin_query_endpoint_error", error=str(ex))
        raise HTTPException(status_code=500, detail=str(ex))

@app.post("/api/v1/user/chat", dependencies=[Depends(verify_internal_api_key)])
async def execute_user_agent_chat(req: UserChatRequest):
    """
    Personal AI assistant for registered users with multi-lingual support (English, Urdu, Roman Urdu).
    """
    try:
        result = await user_agent.interact(
            user_id=req.userId,
            user_name=req.userName,
            message=req.message,
            user_context=req.userContext,
            history=req.history
        )
        return result
    except Exception as ex:
        logger.error("user_chat_endpoint_error", error=str(ex))
        raise HTTPException(status_code=500, detail=str(ex))

@app.post("/api/v1/security/evaluate", dependencies=[Depends(verify_internal_api_key)])
async def evaluate_security_activity(req: SecurityEvaluateRequest):
    """
    Evaluates incoming user activity, detects threats, and triggers the 3-Second Warning / Suspension Protocol.
    """
    try:
        result = await security_engine.evaluate_activity(
            user_id=req.userId,
            user_email=req.userEmail,
            ip_address=req.ipAddress,
            activity_type=req.activityType,
            activity_payload=req.payload
        )
        return result
    except Exception as ex:
        logger.error("security_evaluate_endpoint_error", error=str(ex))
        raise HTTPException(status_code=500, detail=str(ex))

if __name__ == "__main__":
    import uvicorn
    uvicorn.run("app.main:app", host=settings.host, port=settings.port, reload=True)
