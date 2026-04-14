import logging

from azure.servicebus import ServiceBusClient

from app.config import settings
from app.domain.models import ValidationRequest, ValidationResult
from app.services.classification_service import classify_gl_entries
from app.services.cosmos_service import save_classification_result
from app.services.servicebus_service import publish_result

logger = logging.getLogger(__name__)


def process_message(body: str) -> None:
    request = ValidationRequest.model_validate_json(body)
    logger.info("Processing validation for engagement %s", request.engagement_id)

    # Signal processing start so the API can flip validation_job to Processing
    # and SignalR pushes the transition to the dashboard.
    publish_result(request.validation_job_id, "processing")

    try:
        classifications = classify_gl_entries(request.gl_entries)

        total_debits = sum(e.debit for e in request.gl_entries)
        total_credits = sum(e.credit for e in request.gl_entries)
        variance = abs(total_debits - total_credits)
        is_balanced = variance < settings.balance_tolerance

        summary: dict[str, int] = {}
        for c in classifications:
            key = c.classified_as.value
            summary[key] = summary.get(key, 0) + 1

        flagged_items = []
        for c in classifications:
            for flag in c.flags:
                flagged_items.append({
                    **flag,
                    "accountCode": c.account_code,
                    "accountName": c.account_name,
                })

        result = ValidationResult(
            engagement_id=request.engagement_id,
            trial_balance_id=request.trial_balance_id,
            is_balanced=is_balanced,
            total_debits=total_debits,
            total_credits=total_credits,
            variance=variance,
            classifications=classifications,
            summary=summary,
            flagged_items=flagged_items,
        )
        save_classification_result(result)

        publish_result(request.validation_job_id, "completed")
        logger.info("Completed validation for engagement %s", request.engagement_id)

    except Exception as exc:
        # Scenario 3: surface failure to the user via result queue so the API
        # updates validation_jobs.status = Failed and SignalR pushes the Failed badge.
        logger.exception("Validation failed for job %s", request.validation_job_id)
        try:
            publish_result(request.validation_job_id, "failed", error_message=str(exc))
        except Exception:
            logger.exception("Failed to publish failure result for job %s", request.validation_job_id)
        raise


def run_worker() -> None:
    """Main loop: consume messages from the validation request queue."""
    logger.info("Starting worker, listening on queue: %s", settings.validation_request_queue)
    client = ServiceBusClient.from_connection_string(settings.servicebus_connection_string)

    with client:
        receiver = client.get_queue_receiver(queue_name=settings.validation_request_queue)
        with receiver:
            while True:
                messages = receiver.receive_messages(max_message_count=1, max_wait_time=30)
                for msg in messages:
                    try:
                        process_message(str(msg))
                        receiver.complete_message(msg)
                    except Exception:
                        # process_message already reported failure to result queue;
                        # abandon so Service Bus retries up to max_delivery_count,
                        # after which the message lands in DLQ.
                        receiver.abandon_message(msg)
