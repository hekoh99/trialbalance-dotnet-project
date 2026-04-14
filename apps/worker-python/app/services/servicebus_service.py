import json
import logging
from typing import Optional

from azure.servicebus import ServiceBusClient, ServiceBusMessage

from app.config import settings

logger = logging.getLogger(__name__)

_client: ServiceBusClient | None = None


def _get_client() -> ServiceBusClient:
    global _client
    if _client is None:
        _client = ServiceBusClient.from_connection_string(settings.servicebus_connection_string)
    return _client


def publish_result(
    validation_job_id: str,
    status: str,
    error_message: Optional[str] = None,
) -> None:
    """
    Publish validation result to the result queue.
    status: "processing" | "completed" | "failed"
    .NET API's result consumer uses this to transition validation_jobs state
    and push the update to the Angular dashboard via SignalR.
    """
    payload: dict = {
        "validationJobId": validation_job_id,
        "status": status,
    }
    if error_message is not None:
        payload["errorMessage"] = error_message

    client = _get_client()
    sender = client.get_queue_sender(queue_name=settings.validation_result_queue)
    with sender:
        message = ServiceBusMessage(json.dumps(payload))
        sender.send_messages(message)

    logger.info("Published result for job %s: %s", validation_job_id, status)
