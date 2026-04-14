from enum import Enum
from typing import Optional

from pydantic import BaseModel, ConfigDict
from pydantic.alias_generators import to_camel


class AccountType(str, Enum):
    ASSET = "Asset"
    LIABILITY = "Liability"
    EQUITY = "Equity"
    REVENUE = "Revenue"
    EXPENSE = "Expense"
    UNCLASSIFIED = "Unclassified"


class _WireModel(BaseModel):
    """
    Base model that (de)serializes with camelCase keys on the wire — matching
    the .NET API's message contract — while keeping snake_case Python
    attributes. populate_by_name also allows snake_case input for local tests.
    """
    model_config = ConfigDict(
        alias_generator=to_camel,
        populate_by_name=True,
    )


class GLEntryInput(_WireModel):
    account_code: str
    account_name: str
    debit: float
    credit: float
    balance: float


class ValidationRequest(_WireModel):
    engagement_id: str
    trial_balance_id: str
    validation_job_id: str
    gl_entries: list[GLEntryInput]


class ClassificationResult(_WireModel):
    account_code: str
    account_name: str
    classified_as: AccountType
    confidence: float
    flags: list[dict]
    reasoning: Optional[str] = None


class ValidationResult(_WireModel):
    engagement_id: str
    trial_balance_id: str
    is_balanced: bool
    total_debits: float
    total_credits: float
    variance: float
    classifications: list[ClassificationResult]
    summary: dict
    flagged_items: list[dict]
