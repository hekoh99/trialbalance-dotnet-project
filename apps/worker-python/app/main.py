import logging
import threading
from contextlib import asynccontextmanager

from fastapi import FastAPI

from app.config import settings
from app.worker import run_worker

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

# --- Application Insights (OpenTelemetry) ---
# Must run before FastAPI is instantiated so auto-instrumentation (FastAPI,
# requests, logging) captures everything. No-op when connection string empty —
# lets the worker still run in tests / offline dev.
if settings.application_insights_connection_string:
    from azure.monitor.opentelemetry import configure_azure_monitor

    configure_azure_monitor(
        connection_string=settings.application_insights_connection_string,
        logger_name="app",  # route `app.*` loggers through OTel
    )
    logger.info("Azure Monitor OpenTelemetry configured")
else:
    logger.info("APPLICATIONINSIGHTS_CONNECTION_STRING not set — telemetry disabled")


@asynccontextmanager
async def lifespan(app: FastAPI):
    if settings.servicebus_connection_string:
        thread = threading.Thread(target=run_worker, daemon=True)
        thread.start()
        logger.info("Worker thread started")
    else:
        logger.warning("SERVICEBUS_CONNECTION_STRING not set — worker not started")
    yield
    # daemon thread exits on process exit; nothing to clean up explicitly.


app = FastAPI(title="TriBalance Worker", version="1.0.0", lifespan=lifespan)


@app.get("/health")
def health():
    return {"status": "healthy"}


@app.get("/config")
def config_check():
    """Show which services are configured (no secrets exposed)."""
    return {
        "azure_openai_endpoint": bool(settings.azure_openai_endpoint),
        "servicebus": bool(settings.servicebus_connection_string),
        "cosmos": bool(settings.cosmos_connection_string),
        "key_vault": bool(settings.key_vault_uri),
        "application_insights": bool(settings.application_insights_connection_string),
    }
