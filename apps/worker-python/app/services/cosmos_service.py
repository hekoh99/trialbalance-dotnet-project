import logging
from datetime import datetime, timezone

from azure.cosmos import CosmosClient

from app.config import settings
from app.domain.models import ValidationResult

logger = logging.getLogger(__name__)

_client: CosmosClient | None = None


def _get_container(container_name: str):
    global _client
    if _client is None:
        _client = CosmosClient.from_connection_string(settings.cosmos_connection_string)
    db = _client.get_database_client(settings.cosmos_database_name)
    return db.get_container_client(container_name)


def save_classification_result(result: ValidationResult) -> None:
    container = _get_container("classification-results")
    container.upsert_item({
        "id": result.trial_balance_id,
        "engagementId": result.engagement_id,
        "isBalanced": result.is_balanced,
        "totalDebits": result.total_debits,
        "totalCredits": result.total_credits,
        "variance": result.variance,
        "classifications": [c.model_dump(by_alias=True) for c in result.classifications],
        "summary": result.summary,
        "flaggedItems": result.flagged_items,
        "processedAt": datetime.now(timezone.utc).isoformat(),
    })
    logger.info("Saved classification result for engagement %s", result.engagement_id)
