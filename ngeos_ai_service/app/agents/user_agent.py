import time
from typing import Dict, Any, List, Optional
from app.logger import logger
from app.providers.orchestrator import orchestrator
from app.providers.base import ChatMessage
from app.prompts.user_prompts import USER_AGENT_SYSTEM_PROMPT
from app.security import sanitize_user_input
from app.bridge.dotnet_client import dotnet_bridge

class UserAgent:
    def __init__(self):
        self.name = "NGEOS Personal User Agent"

    def detect_language(self, text: str) -> str:
        """
        Simple heuristic language detector (English, Urdu script, Roman Urdu).
        """
        # Check Arabic/Urdu Unicode range
        if any('\u0600' <= char <= '\u06FF' for char in text):
            return "ur"
        
        # Check Roman Urdu typical keywords
        roman_keywords = [
            "aap", "kya", "mera", "meri", "hum", "kaise", "batao", "mujhe", 
            "shukriya", "hai", "hain", "ke", "ki", "ko", "kar", "karna"
        ]
        words = text.lower().split()
        if any(w in words for w in roman_keywords):
            return "ur-PK" # Roman Urdu

        return "en"

    async def interact(
        self, 
        user_id: Optional[int], 
        user_name: str, 
        message: str,
        user_context: Optional[Dict[str, Any]] = None,
        history: Optional[List[Dict[str, str]]] = None
    ) -> Dict[str, Any]:
        start_time = time.perf_counter()

        cleaned_msg, is_flagged = sanitize_user_input(message)
        lang = self.detect_language(cleaned_msg)

        if is_flagged:
            # Report security observation
            await dotnet_bridge.record_security_event({
                "userId": user_id,
                "violationCategory": "MaliciousPayload",
                "detectionDetails": f"Suspicious prompt pattern detected in user input: {message[:100]}",
                "riskScore": 0.75,
                "severity": "Medium",
                "warningIssued": True
            })

            reply_msg = (
                "⚠️ Security Notice: Your message contains patterns that violate our safety policies. "
                "Please ask a valid platform inquiry."
            )
            if lang == "ur-PK":
                reply_msg = "⚠️ Security Warning: Aap ka paigham safety policy ke mutabiq nahi hai. Baraye meherbani platform se mutaliq sawal poochein."
            elif lang == "ur":
                reply_msg = "⚠️ سیکیورٹی وارننگ: آپ کا پیغام ہماری سیکیورٹی پالیسی کی خلاف ورزی کر رہا ہے۔ برائے مہربانی درست سوال پوچھیں۔"

            return {
                "reply": reply_msg,
                "language": lang,
                "providerUsed": "SecuritySanitizer",
                "model": "RuleEngine",
                "isSuccess": True,
                "latencyMs": (time.perf_counter() - start_time) * 1000
            }

        # Format user context securely
        user_info_str = f"User Name: {user_name}\n"
        if user_context:
            user_info_str += (
                f"Total Personal Donations: Rs {user_context.get('totalDonated', 0):,}\n"
                f"Supported Causes Count: {user_context.get('causesSupportedCount', 0)}\n"
                f"Active Inquiries Count: {user_context.get('queriesCount', 0)}\n"
            )

        system_instruction = (
            f"{USER_AGENT_SYSTEM_PROMPT}\n\n"
            f"AUTHENTICATED USER CONTEXT:\n{user_info_str}\n"
            f"DETECTED LANGUAGE: {lang}\n"
            f"Please respond in the corresponding language."
        )

        chat_messages: List[ChatMessage] = []
        if history:
            for h in history[-6:]:  # Keep recent context
                chat_messages.append(ChatMessage(role=h.get("role", "user"), content=h.get("content", "")))

        chat_messages.append(ChatMessage(role="user", content=cleaned_msg))

        resp = await orchestrator.execute(
            system_prompt=system_instruction,
            messages=chat_messages,
            temperature=0.4,
            max_tokens=800
        )

        return {
            "reply": resp.content,
            "language": lang,
            "providerUsed": resp.provider,
            "model": resp.model,
            "latencyMs": resp.latency_ms,
            "isSuccess": resp.is_success
        }

user_agent = UserAgent()
