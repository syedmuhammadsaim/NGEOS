import time
from typing import List
from groq import AsyncGroq
from app.config import settings
from app.logger import logger
from app.providers.base import BaseAiProvider, ChatMessage, ProviderResponse

class GroqProvider(BaseAiProvider):
    def __init__(self, api_key: str = "", model: str = ""):
        self.api_key = api_key or settings.groq_api_key
        self.model = model or settings.groq_model
        self.client = AsyncGroq(api_key=self.api_key, timeout=float(settings.timeout_seconds)) if self.api_key else None

    async def generate_response(
        self,
        system_prompt: str,
        messages: List[ChatMessage],
        temperature: float = 0.4,
        max_tokens: int = 1024,
    ) -> ProviderResponse:
        start_time = time.perf_counter()

        if not self.client or not self.api_key:
            return ProviderResponse(
                content="",
                provider="Groq",
                model=self.model,
                latency_ms=(time.perf_counter() - start_time) * 1000,
                is_success=False,
                error_message="Groq API Key is not configured."
            )

        formatted_messages = [{"role": "system", "content": system_prompt}]
        for m in messages:
            formatted_messages.append({"role": m.role, "content": m.content})

        try:
            response = await self.client.chat.completions.create(
                model=self.model,
                messages=formatted_messages,
                temperature=temperature,
                max_tokens=max_tokens,
            )

            latency = (time.perf_counter() - start_time) * 1000
            choice = response.choices[0]
            content = choice.message.content or ""
            usage = response.usage

            prompt_toks = usage.prompt_tokens if usage else 0
            comp_toks = usage.completion_tokens if usage else 0

            return ProviderResponse(
                content=content,
                provider="Groq",
                model=self.model,
                prompt_tokens=prompt_toks,
                completion_tokens=comp_toks,
                total_tokens=prompt_toks + comp_toks,
                latency_ms=latency,
                is_success=True
            )
        except Exception as ex:
            latency = (time.perf_counter() - start_time) * 1000
            logger.error("groq_generation_failed", error=str(ex), model=self.model)
            return ProviderResponse(
                content="",
                provider="Groq",
                model=self.model,
                latency_ms=latency,
                is_success=False,
                error_message=str(ex)
            )
