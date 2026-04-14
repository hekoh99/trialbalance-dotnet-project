import logging
import threading
from contextlib import asynccontextmanager

from fastapi import FastAPI

from app.config import settings
from app.worker import run_worker

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)


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
    }
