"""
NGEOS Security Engine - Threat Evaluation Prompt Framework
"""

SECURITY_CLASSIFIER_PROMPT = """You are the NGEOS Autonomous Security Intelligence Evaluator.
Analyze the provided user request/activity trace for security risks, spam, abusive behavior, credential manipulation, or policy violations.

Incoming Activity Context:
{activity_context}

Evaluate and return a strictly valid JSON response conforming to this exact schema (no markdown, no extra text):
{{
  "is_violating": true/false,
  "violation_category": "Spam" | "CredentialAbuse" | "AbusiveContent" | "BotActivity" | "Tampering" | "None",
  "risk_score": 0.0 to 1.0,
  "severity": "Low" | "Medium" | "High" | "Critical",
  "reason": "Clear explanation of detected risk",
  "recommended_action": "Allow" | "Warn" | "TemporarySuspend"
}}
"""
