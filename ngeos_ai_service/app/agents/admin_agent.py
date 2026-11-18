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
from app.services.free_intelligence_services import free_currency_service, free_disaster_service

class AdminAgent:
    def __init__(self):
        self.name = "NGEOS Autonomous Admin Agent"

    async def answer_admin_query(self, query: str, context: Optional[Dict[str, Any]] = None) -> Dict[str, Any]:
        start_time = time.perf_counter()
        
        # 1. Fetch live comprehensive 15-section ecosystem summary from ASP.NET Core
        sections_summary = await dotnet_bridge.get_ecosystem_sections_summary()
        donation_data = await dotnet_bridge.get_donation_summary()
        campaign_data = await dotnet_bridge.get_campaign_summary()
        health_data = await dotnet_bridge.get_system_health()
        pending_queries = await dotnet_bridge.get_pending_queries()
        pending_volunteers = await dotnet_bridge.get_pending_volunteers()

        # 2. Fetch live free market currency rates and humanitarian disaster indicators
        currency_rates = await free_currency_service.get_live_rates()
        weather_alerts = await free_disaster_service.get_humanitarian_weather_alerts()

        # Combine verified operational context across ALL 15 sections
        context_str = json.dumps({
            "sections_overview": sections_summary,
            "donations_ledger": donation_data,
            "campaigns": campaign_data,
            "pending_queries": pending_queries,
            "pending_volunteers": pending_volunteers,
            "system_health": health_data,
            "live_currency_exchange_pkr": currency_rates,
            "humanitarian_weather_alerts": weather_alerts,
            "extra_context": context or {}
        }, default=str)

        full_prompt = (
            f"{ADMIN_AGENT_SYSTEM_PROMPT}\n\n"
            f"VERIFIED PLATFORM DATA CONTEXT (ALL 15 ADMIN SECTIONS):\n{context_str}\n\n"
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
            "moduleName": "EcosystemIntelligence",
            "actionDescription": f"Processed executive query across 15 admin sections: {query[:100]}...",
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

    async def run_comprehensive_ecosystem_audit(self) -> Dict[str, Any]:
        """
        Runs an autonomous compliance sweep across all 15 Admin Sections:
        1. Recalculates campaign financial progress
        2. Archives stale job postings
        3. Monitors pending support queries and volunteer backlogs
        """
        start_time = time.perf_counter()
        
        # 1. Permitted routine operation: Recalculate campaign progress
        recalc_res = await dotnet_bridge.execute_permitted_action(
            action_name="RecalculateCampaignProgress",
            module_name="Causes",
            reason="Periodic autonomous financial consistency sync."
        )

        # 2. Permitted routine operation: Flag stale job vacancies
        jobs_res = await dotnet_bridge.execute_permitted_action(
            action_name="FlagExpiredJobs",
            module_name="Jobs",
            reason="Periodic autonomous vacancy freshness audit."
        )

        # 3. Pull live ecosystem summary
        summary = await dotnet_bridge.get_ecosystem_sections_summary()

        duration = (time.perf_counter() - start_time) * 1000

        await dotnet_bridge.record_agent_report({
            "agentName": self.name,
            "agentType": "AdminAgent",
            "actionName": "FullEcosystemAudit",
            "moduleName": "EcosystemManagement",
            "actionDescription": f"Completed 15-section ecosystem sweep. Recalculate result: {recalc_res.get('success')}, Jobs archive: {jobs_res.get('success')}.",
            "actionReason": "Scheduled Comprehensive Autonomous Audit",
            "riskLevel": "Low",
            "actionStatus": "Success",
            "executionDurationMs": duration
        })

        return {
            "status": "Success",
            "recalculatedCauses": recalc_res.get("success", False),
            "archivedStaleJobs": jobs_res.get("success", False),
            "summary": summary,
            "durationMs": duration
        }

admin_agent = AdminAgent()
