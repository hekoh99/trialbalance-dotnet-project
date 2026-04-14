import json
import logging

from openai import AzureOpenAI

from app.config import settings
from app.domain.models import GLEntryInput, ClassificationResult, AccountType

logger = logging.getLogger(__name__)

_client: AzureOpenAI | None = None


def _get_client() -> AzureOpenAI:
    global _client
    if _client is None:
        _client = AzureOpenAI(
            azure_endpoint=settings.azure_openai_endpoint,
            api_key=settings.azure_openai_key,
            api_version=settings.azure_openai_api_version,
        )
    return _client


def classify_gl_entries(entries: list[GLEntryInput]) -> list[ClassificationResult]:
    """
    Classify GL entries using Azure OpenAI, then apply deterministic post-processing
    for anomaly flags that must not depend on LLM judgment (low_confidence,
    negative_balance on Asset, unclassified). LLM handles the classification decision;
    rule-based flags guarantee reproducibility for auditor review.
    """
    client = _get_client()

    prompt = f"""You are an accounting expert. Classify each GL entry into one of:
Asset, Liability, Equity, Revenue, Expense, or Unclassified.

Use Unclassified when the account name is ambiguous or you cannot confidently determine its type.
You may add reasoning. Do NOT include rule-based flags (negative balance, low confidence,
unclassified) — those are applied separately. Only include LLM-judgment flags such as
"unusual_account_naming".

Return ONLY valid JSON matching this schema:
{{
  "classifications": [
    {{
      "account_code": "string",
      "account_name": "string",
      "classified_as": "Asset|Liability|Equity|Revenue|Expense|Unclassified",
      "confidence": 0.0-1.0,
      "flags": [{{ "type": "string", "severity": "string", "detail": "string" }}],
      "reasoning": "string"
    }}
  ]
}}

GL Entries:
{json.dumps([e.model_dump() for e in entries], indent=2)}"""

    response = client.chat.completions.create(
        model=settings.azure_openai_deployment,
        messages=[{"role": "user", "content": prompt}],
        response_format={"type": "json_object"},
        temperature=0.1,
    )

    result = json.loads(response.choices[0].message.content)
    by_code = {e.account_code: e for e in entries}

    classifications: list[ClassificationResult] = []
    for c in result["classifications"]:
        try:
            classified_as = AccountType(c["classified_as"])
        except ValueError:
            classified_as = AccountType.UNCLASSIFIED

        confidence = float(c.get("confidence", 0.5))
        flags = list(c.get("flags", []))

        entry = by_code.get(c["account_code"])
        if entry is not None:
            flags.extend(_deterministic_flags(classified_as, confidence, entry))

        classifications.append(ClassificationResult(
            account_code=c["account_code"],
            account_name=c.get("account_name", entry.account_name if entry else ""),
            classified_as=classified_as,
            confidence=confidence,
            flags=flags,
            reasoning=c.get("reasoning"),
        ))

    logger.info("Classified %d GL entries via Azure OpenAI", len(classifications))
    return classifications


def _deterministic_flags(
    classified_as: AccountType,
    confidence: float,
    entry: GLEntryInput,
) -> list[dict]:
    """
    Rule-based flags that must fire independently of LLM output.
    Scenario 2 (anomaly detection) relies on these being reproducible.
    """
    flags: list[dict] = []

    if classified_as == AccountType.UNCLASSIFIED:
        flags.append({
            "type": "unclassified",
            "severity": "warning",
            "detail": f"Account {entry.account_code} could not be classified",
        })

    if confidence < settings.low_confidence_threshold:
        flags.append({
            "type": "low_confidence",
            "severity": "warning",
            "threshold": settings.low_confidence_threshold,
            "actual": confidence,
        })

    if classified_as == AccountType.ASSET and entry.balance < 0:
        flags.append({
            "type": "negative_balance",
            "severity": "warning",
            "detail": f"Asset account has negative balance: {entry.balance}",
        })

    return flags
