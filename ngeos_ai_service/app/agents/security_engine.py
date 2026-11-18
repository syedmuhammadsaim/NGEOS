import json
import time
from typing import Dict, Any, Optional
from app.logger import logger
from app.providers.orchestrator import orchestrator
from app.providers.base import ChatMessage
from app.prompts.security_prompts import SECURITY_CLASSIFIER_PROMPT
from app.bridge.dotnet_client import dotnet_bridge
from app.services.free_intelligence_services import free_geoip_service

class AutonomousSecurityEngine:
    def __init__(self):
        self.name = "NGEOS Autonomous Security Engine"

    async def evaluate_activity(
        self,
        user_id: Optional[int],
        user_email: Optional[str],
        ip_address: Optional[str],
        activity_type: str,
        activity_payload: str
    ) -> Dict[str, Any]:
        """
        Runs deterministic inspection, free IP threat analysis, and AI classification to score threat risk.
        """
        start_time = time.perf_counter()

        # Free IP Geolocation & Threat lookup
        geo_info = await free_geoip_service.lookup_ip(ip_address or "")
        is_proxy = geo_info.get("isProxy", False)
        country = geo_info.get("country", "Unknown")
        
        # 1. Deterministic checks
        lower_payload = activity_payload.lower()
        deterministic_violation = False
        violation_category = "None"
        reason = f"Normal Activity from {country}"

        if is_proxy:
            reason = f"Activity routed via anonymous proxy/hosting IP from {country}."

        # Explicit abuse or destructive SQL keywords
        if any(term in lower_payload for term in ["drop table", "xp_cmdshell", "or 1=1--", "script>alert("]):
            deterministic_violation = True
            violation_category = "Tampering"
            reason = f"Exploit or SQL Injection attempt detected from {country}."
        elif any(term in lower_payload for term in ["spam bot", "viagra", "free crypto", "telegram.me/"]):
            deterministic_violation = True
            violation_category = "Spam"
            reason = f"Automated promotional spam link identified from {country}."

        risk_score = 0.9 if deterministic_violation else (0.4 if is_proxy else 0.1)
        severity = "Critical" if deterministic_violation else ("Medium" if is_proxy else "Low")

        # If indeterminate, run AI evaluation
        if not deterministic_violation and len(activity_payload) > 10:
            prompt = SECURITY_CLASSIFIER_PROMPT.format(activity_context=json.dumps({
                "activity_type": activity_type,
                "payload": activity_payload[:500]
            }))
            resp = await orchestrator.execute(
                system_prompt="You are a strict security classifier. Return only valid JSON.",
                messages=[ChatMessage(role="user", content=prompt)],
                temperature=0.1,
                max_tokens=300
            )
            if resp.is_success and resp.content:
                try:
                    # Strip any accidental markdown fences
                    clean_json = resp.content.replace("```json", "").replace("```", "").strip()
                    parsed = json.loads(clean_json)
                    if parsed.get("is_violating"):
                        violation_category = parsed.get("violation_category", "PolicyViolation")
                        risk_score = float(parsed.get("risk_score", 0.5))
                        severity = parsed.get("severity", "Medium")
                        reason = parsed.get("reason", "AI detected platform policy infringement.")
                except Exception as ex:
                    logger.warning("security_ai_json_parse_failed", error=str(ex))

        # Check if serious policy violation triggers the 3-Second Countdown Protocol
        trigger_suspension = False
        should_issue_warning = False

        if risk_score >= 0.85:
            # Verified serious violation -> Trigger Three-Second Countdown Suspension Protocol
            trigger_suspension = True
        elif risk_score >= 0.5:
            # Moderate concern -> Issue security warning
            should_issue_warning = True

        duration = (time.perf_counter() - start_time) * 1000

        # Log security event in ASP.NET Core
        if risk_score >= 0.5:
            await dotnet_bridge.record_security_event({
                "userId": user_id,
                "userEmail": user_email,
                "ipAddress": ip_address,
                "violationCategory": violation_category,
                "detectionDetails": reason,
                "riskScore": risk_score,
                "severity": severity,
                "warningIssued": should_issue_warning,
                "restrictionTriggered": trigger_suspension
            })

            # Broadcast real-time warning via SignalR
            if user_id:
                if trigger_suspension:
                    # Signal 3-Second Countdown modal to user
                    await dotnet_bridge.broadcast_signalr(
                        target_group=f"User_{user_id}",
                        event_name="SecurityCountdownAlert",
                        data={
                            "countdownSeconds": 3,
                            "category": violation_category,
                            "reason": reason,
                            "message": "Your account has been flagged for a serious policy violation. Suspension initiating in 3 seconds."
                        }
                    )
                    # Enforce restriction after protocol initiation
                    await dotnet_bridge.trigger_user_restriction(
                        user_id=user_id,
                        violation_category=violation_category,
                        reason=reason
                    )
                elif should_issue_warning:
                    await dotnet_bridge.broadcast_signalr(
                        target_group=f"User_{user_id}",
                        event_name="SecurityWarningAlert",
                        data={
                            "category": violation_category,
                            "reason": reason,
                            "message": "SECURITY WARNING: Your recent activity may violate NGEOS platform policies. Please review guidelines."
                        }
                    )

        return {
            "riskScore": risk_score,
            "severity": severity,
            "violationCategory": violation_category,
            "warningIssued": should_issue_warning,
            "restrictionTriggered": trigger_suspension,
            "reason": reason,
            "latencyMs": duration
        }

security_engine = AutonomousSecurityEngine()
