import httpx
from typing import Dict, Any, Optional
from app.config import settings
from app.logger import logger

class DotNetBridgeClient:
    def __init__(self):
        self.base_url = settings.dotnet_base_url.rstrip("/")
        self.headers = {
            "X-NGEOS-Internal-Key": settings.internal_api_key,
            "Content-Type": "application/json",
            "User-Agent": "NGEOS-Autonomous-AI-Service/1.0"
        }
        self.timeout = float(settings.timeout_seconds)

    async def get_donation_summary(self) -> Dict[str, Any]:
        url = f"{self.base_url}/api/agent/internal/analytics/donations"
        try:
            async with httpx.AsyncClient(timeout=self.timeout) as client:
                res = await client.get(url, headers=self.headers)
                if res.status_code == 200:
                    return res.json()
                logger.warning("dotnet_bridge_donation_failed", status=res.status_code)
                return {}
        except Exception as ex:
            logger.error("dotnet_bridge_donation_exception", error=str(ex))
            return {}

    async def get_campaign_summary(self) -> Dict[str, Any]:
        url = f"{self.base_url}/api/agent/internal/analytics/campaigns"
        try:
            async with httpx.AsyncClient(timeout=self.timeout) as client:
                res = await client.get(url, headers=self.headers)
                if res.status_code == 200:
                    return res.json()
                return {}
        except Exception as ex:
            logger.error("dotnet_bridge_campaign_exception", error=str(ex))
            return {}

    async def get_system_health(self) -> Dict[str, Any]:
        url = f"{self.base_url}/api/agent/internal/health"
        try:
            async with httpx.AsyncClient(timeout=self.timeout) as client:
                res = await client.get(url, headers=self.headers)
                if res.status_code == 200:
                    return res.json()
                return {"status": "Degraded", "database": "Unknown"}
        except Exception as ex:
            logger.error("dotnet_bridge_health_exception", error=str(ex))
            return {"status": "Unreachable", "error": str(ex)}

    async def record_agent_report(self, report_payload: Dict[str, Any]) -> bool:
        url = f"{self.base_url}/api/agent/internal/reports"
        try:
            async with httpx.AsyncClient(timeout=self.timeout) as client:
                res = await client.post(url, headers=self.headers, json=report_payload)
                return res.status_code in [200, 201]
        except Exception as ex:
            logger.error("dotnet_bridge_report_post_exception", error=str(ex))
            return False

    async def record_security_event(self, event_payload: Dict[str, Any]) -> bool:
        url = f"{self.base_url}/api/agent/internal/security/events"
        try:
            async with httpx.AsyncClient(timeout=self.timeout) as client:
                res = await client.post(url, headers=self.headers, json=event_payload)
                return res.status_code in [200, 201]
        except Exception as ex:
            logger.error("dotnet_bridge_security_post_exception", error=str(ex))
            return False

    async def trigger_user_restriction(self, user_id: int, violation_category: str, reason: str) -> bool:
        url = f"{self.base_url}/api/agent/internal/users/{user_id}/restrict"
        payload = {
            "violationCategory": violation_category,
            "reason": reason
        }
        try:
            async with httpx.AsyncClient(timeout=self.timeout) as client:
                res = await client.post(url, headers=self.headers, json=payload)
                return res.status_code in [200, 201]
        except Exception as ex:
            logger.error("dotnet_bridge_restriction_post_exception", error=str(ex))
            return False

    async def broadcast_signalr(self, target_group: str, event_name: str, data: Any) -> bool:
        url = f"{self.base_url}/api/agent/internal/broadcast"
        payload = {
            "targetGroup": target_group,
            "eventName": event_name,
            "payload": data
        }
        try:
            async with httpx.AsyncClient(timeout=self.timeout) as client:
                res = await client.post(url, headers=self.headers, json=payload)
                return res.status_code == 200
        except Exception as ex:
            logger.error("dotnet_bridge_broadcast_exception", error=str(ex))
            return False

dotnet_bridge = DotNetBridgeClient()
