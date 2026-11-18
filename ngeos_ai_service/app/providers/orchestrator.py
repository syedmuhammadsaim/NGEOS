from typing import List, Optional
from app.config import settings
from app.logger import logger
from app.providers.base import BaseAiProvider, ChatMessage, ProviderResponse
from app.providers.groq_provider import GroqProvider
from app.providers.gemini_provider import GeminiProvider

class AiOrchestrator:
    def __init__(self):
        self.groq_provider = GroqProvider()
        self.gemini_provider = GeminiProvider()

    async def execute(
        self,
        system_prompt: str,
        messages: List[ChatMessage],
        temperature: float = 0.4,
        max_tokens: int = 1024,
        forced_provider: Optional[str] = None
    ) -> ProviderResponse:
        primary_choice = (forced_provider or settings.primary_provider).lower()

        if primary_choice == "gemini":
            first, second = self.gemini_provider, self.groq_provider
        else:
            first, second = self.groq_provider, self.gemini_provider

        logger.info("orchestrator_attempting_primary", provider=first.__class__.__name__)
        resp = await first.generate_response(system_prompt, messages, temperature, max_tokens)

        if resp.is_success and resp.content:
            return resp

        # Automatic Failover
        logger.warning(
            "primary_provider_failed_initiating_failover",
            failed_provider=resp.provider,
            error=resp.error_message,
            next_provider=second.__class__.__name__
        )

        failover_resp = await second.generate_response(system_prompt, messages, temperature, max_tokens)
        if failover_resp.is_success:
            logger.info("failover_provider_succeeded", provider=failover_resp.provider)
            return failover_resp

        # If both fail, return structured error
        logger.error(
            "all_ai_providers_failed",
            first_error=resp.error_message,
            second_error=failover_resp.error_message
        )
        return ProviderResponse(
            content="I am currently experiencing service connectivity limitations. Please try again shortly.",
            provider="FallbackError",
            model="None",
            is_success=False,
            error_message=f"Primary ({resp.error_message}) and Secondary ({failover_resp.error_message}) failed."
        )

orchestrator = AiOrchestrator()
