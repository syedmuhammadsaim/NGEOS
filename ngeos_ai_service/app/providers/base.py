from abc import ABC, abstractmethod
from typing import List, Dict, Any, Optional
from pydantic import BaseModel

class ChatMessage(BaseModel):
    role: str  # "system", "user", "assistant"
    content: str

class ProviderResponse(BaseModel):
    content: str
    provider: str
    model: str
    prompt_tokens: int = 0
    completion_tokens: int = 0
    total_tokens: int = 0
    latency_ms: float = 0.0
    is_success: bool = True
    error_message: Optional[str] = None

class BaseAiProvider(ABC):
    @abstractmethod
    async def generate_response(
        self,
        system_prompt: str,
        messages: List[ChatMessage],
        temperature: float = 0.4,
        max_tokens: int = 1024,
    ) -> ProviderResponse:
        pass
