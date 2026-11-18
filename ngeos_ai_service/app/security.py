import hmac
import hashlib
import time
from typing import Dict, Tuple
from fastapi import Header, HTTPException, status
from app.config import settings
from app.logger import logger

# In-memory rate limiting dictionary: {ip_or_identifier: (count, reset_epoch)}
_RATE_LIMIT_BUCKET: Dict[str, Tuple[int, float]] = {}
MAX_REQUESTS_PER_MINUTE = 120

# Common prompt injection signatures to sanitize and neutralize
INJECTION_PATTERNS = [
    "ignore previous instructions",
    "disregard all previous",
    "you are now an unfiltered",
    "jailbreak",
    "system prompt leak",
    "reveal internal api keys",
    "drop table",
    "delete from users",
    "exec xp_cmdshell",
]

def verify_internal_api_key(x_ngeos_internal_key: str = Header(None)) -> bool:
    """
    Validates the secret service-to-service key passed from ASP.NET Core.
    """
    if not x_ngeos_internal_key:
        logger.warning("auth_failed_missing_header")
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail="Missing required X-NGEOS-Internal-Key header."
        )

    # Constant-time comparison to prevent timing attacks
    is_valid = hmac.compare_digest(
        x_ngeos_internal_key.strip(), 
        settings.internal_api_key.strip()
    )
    if not is_valid:
        logger.warning("auth_failed_invalid_key")
        raise HTTPException(
            status_code=status.HTTP_403_FORBIDDEN,
            detail="Invalid service authentication credentials."
        )
    return True

def check_rate_limit(client_id: str, limit: int = MAX_REQUESTS_PER_MINUTE) -> bool:
    """
    Sliding window rate limit checker per client/IP.
    """
    now = time.time()
    if client_id not in _RATE_LIMIT_BUCKET:
        _RATE_LIMIT_BUCKET[client_id] = (1, now + 60)
        return True

    count, reset_time = _RATE_LIMIT_BUCKET[client_id]
    if now > reset_time:
        _RATE_LIMIT_BUCKET[client_id] = (1, now + 60)
        return True

    if count >= limit:
        return False

    _RATE_LIMIT_BUCKET[client_id] = (count + 1, reset_time)
    return True

def sanitize_user_input(text: str) -> Tuple[str, bool]:
    """
    Analyzes user text for prompt injection and malicious keywords.
    Returns (cleaned_text, is_flagged).
    """
    if not text:
        return "", False

    cleaned = text.strip()
    lower_text = cleaned.lower()
    is_flagged = False

    for pattern in INJECTION_PATTERNS:
        if pattern in lower_text:
            is_flagged = True
            logger.warning("prompt_injection_pattern_detected", pattern=pattern)
            break

    return cleaned, is_flagged
