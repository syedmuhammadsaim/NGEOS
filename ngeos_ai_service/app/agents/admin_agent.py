import json
import time
from typing import Dict, Any, List
from app.logger import logger
from app.providers.orchestrator import orchestrator
from app.providers.base import ChatMessage
from app.prompts.admin_prompts import (
    ADMIN_AGENT_SYSTEM_PROMPT,
    DONATION_ANALYSIS_PROMPT,
    CAMPAIGN_MONITORING_PROMPT
)
from app.bridge.dotnet_client import dotnet_bridge

class AdminAgent:
    def __init__(self):
        self.name = "NGEOS Autonomous Admin Agent"

    async def answer_admin_query(self, query: str, context: Optional[Dict[str, Any]] = None) -> Dict[str, Any]:
        start_time = time.perf_counter()
        
        # 1. Fetch live donation and campaign analytics from ASP.NET Core
        donation_data = await dotnet_bridge.get_donation_summary()
        campaign_data = await dotnet_bridge.get_campaign_summary()
        health_data = await dotnet_bridge.get_system_health()

        # Combine verified operational context
        context_str = json.dumps({
            "donations": donation_data,
            "campaigns": campaign_data,
            "system_health": health_data,
            "extra_context": context or {}
        }, default=str)

        full_prompt = (
            f"{ADMIN_AGENT_SYSTEM_PROMPT}\n\n"
            f"VERIFIED PLATFORM DATA CONTEXT:\n{context_str}\n\n"
            f"ADMIN INQUIRY:\n{query}"
        )

        messages = [ChatMessage(role="user", content=full_prompt)]
        resp = await orchestrator.execute(
            system_prompt=ADMIN_AGENT_SYSTEM_PROMPT,
            messages=messages,
            temperature=0.3,
            max_tokens=1500
        )

        duration = (time.perf_counter() - start_time) * 1000

        # Log an Agent Report entry
        await dotnet_bridge.record_agent_report({
            "agentName": self.name,
            "agentType": "AdminAgent",
            "actionName": "AdminExecutiveAnalysis",
            "moduleName": "DonationIntelligence",
            "actionDescription": f"Processed executive query: {query[:100]}...",
            "actionReason": "Authorized Admin Inquiry",
            "riskLevel": "Low",
            "actionStatus": "Success" if resp.is_success else "Failed",
            "executionDurationMs": duration,
            "errorDetails": resp.error_message
        })

        return {
            "reply": resp.content,
            "providerUsed": resp.provider,
            "model": resp.model,
            "latencyMs": resp.latency_ms,
            "isSuccess": resp.is_success
        }

    async def run_autonomous_donation_audit(self) -> Dict[str, Any]:
        """
        Periodically executed by the scheduler to analyze donations and detect irregularities.
        """
        start_time = time.perf_counter()
        data = await dotnet_bridge.get_donation_summary()
        if not data:
            return {"status": "Skipped", "reason": "No live donation data available."}

        prompt = DONATION_ANALYSIS_PROMPT.format(
            donation_context=json.dumps(data, default=str),
            query="Conduct automated financial audit, identify anomalies, and summarize current milestone progress."
        )

        resp = await orchestrator.execute(
            system_prompt=ADMIN_AGENT_SYSTEM_PROMPT,
            messages=[ChatMessage(role="user", content=prompt)],
            temperature=0.2,
            max_tokens=1024
        )

        duration = (time.perf_counter() - start_time) * 1000

        await dotnet_bridge.record_agent_report({
            "agentName": self.name,
            "agentType": "AdminAgent",
            "actionName": "PeriodicDonationAudit",
            "moduleName": "DonationIntelligence",
            "actionDescription": "Automated routine financial and ledger audit completed.",
            "actionReason": "Scheduled Autonomous Automation",
            "riskLevel": "Low",
            "actionStatus": "Success" if resp.is_success else "Failed",
            "executionDurationMs": duration
        })

        return {
            "status": "Success" if resp.is_success else "Failed",
            "summary": resp.content,
            "provider": resp.provider
        }

admin_agent = AdminAgent()
