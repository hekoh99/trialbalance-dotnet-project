from pydantic_settings import BaseSettings


class Settings(BaseSettings):
    # Azure OpenAI — read from env vars (injected from Key Vault in production)
    azure_openai_endpoint: str = ""
    azure_openai_key: str = ""
    azure_openai_api_version: str = "2024-02-01"
    azure_openai_deployment: str = "gpt-4o-mini"

    # Service Bus
    servicebus_connection_string: str = ""
    validation_request_queue: str = "tb-validation-request"
    validation_result_queue: str = "tb-validation-result"

    # Cosmos DB
    cosmos_connection_string: str = ""
    cosmos_database_name: str = "tribalance"

    # Key Vault (optional — for production, reads secrets into env vars)
    key_vault_uri: str = ""

    # Shared thresholds — keep in sync with .NET BalanceTolerance.Epsilon
    balance_tolerance: float = 0.01
    low_confidence_threshold: float = 0.7

    model_config = {"env_file": ".env", "extra": "ignore"}


settings = Settings()
