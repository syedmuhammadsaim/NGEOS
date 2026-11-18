import time
from typing import List
import httpx
from app.config import settings
from app.logger import logger
from app.providers.base import BaseAiProvider, ChatMessage, ProviderResponse

class GeminiProvider(BaseAiProvider):
    def __init__(self, api_key: str = "", model: str = ""):
        self.api_key = api_key or settings.gemini_api_key
        self.model = model or settings.gemini_model
        self.base_url = f"https://generativelanguage.googleapis.com/v1beta/models/{self.model}:generateContent"

    async def generate_response(
        self,
        system_prompt: str,
        messages: List[ChatMessage],
        temperature: float = 0.4,
        max_tokens: int = 1024,
    ) -> ProviderResponse:
        start_time = time.perf_counter()

        if not self.api_key:
            return ProviderResponse(
                content="",
                provider="Gemini",
                model=self.model,
                latency_ms=(time.perf_counter() - start_time) * 1000,
                is_success=False,
                error_message="Gemini API Key is not configured."
            )

        # Build contents array
        contents = []
        for m in messages:
            role_name = "user" if m.role in ["user", "system"] else "model"
            contents.append({
                "role": role_name,
                "parts": [{"text": m.content}]
            })

        payload = {
            "systemInstruction": {
                "parts": [{"text": system_prompt}]
            },
            "contents": contents,
            "generationConfig": {
                "temperature": temperature,
                "maxOutputTokens": max_tokens,
            }
        }

        url = f"{self.base_url}?key={self.api_key}"

        try:
            async with httpx.AsyncClient(timeout=float(settings.timeout_seconds)) as client:
                res = await client.post(url, json=payload)
                latency = (time.perf_counter() - start_time) * 1000

                if res.status_code != 200:
                    logger.error("gemini_api_error_response", status_code=res.status_code, body=res.text)
                    return ProviderResponse(
                        content="",
                        provider="Gemini",
                        model=self.model,
                        latency_ms=latency,
                        is_success=False,
                        error_message=f"Gemini API returned status {res.status_code}: {res.text}"
                    )

                data = res.json()
                candidates = data.get("candidates", [])
                if not candidates:
                    return ProviderResponse(
                        content="",
                        provider="Gemini",
                        model=self.model,
                        latency_ms=latency,
                        is_success=False,
                        error_message="No candidates returned by Gemini."
                    )

                candidate = candidates[0]
                text_parts = candidate.get("content", {}).get("parts", [])
                reply_text = "".join(p.get("text", "") for p in text_parts)
                usage = data.get("usageMetadata", {})

                prompt_toks = usage.get("promptTokenCount", 0)
                comp_toks = usage.get("candidatesTokenCount", 0)

                return ProviderResponse(
                    content=reply_text,
                    provider="Gemini",
                    model=self.model,
                    prompt_tokens=prompt_toks,
                    completion_tokens=comp_toks,
                    total_tokens=prompt_toks + comp_toks,
                    latency_ms=latency,
                    is_success=True
                )
        except Exception as ex:
            latency = (time.perf_counter() - start_time) * 1000
            logger.error("gemini_call_exception", error=str(ex))
            return ProviderResponse(
                content="",
                provider="Gemini",
                model=self.model,
                latency_ms=latency,
                is_success=False,
                error_message=str(ex)
            )
