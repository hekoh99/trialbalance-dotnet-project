# Scenario Testing

End-to-end runbook covering the three product scenarios. Run all three in order;
each verifies a different layer of the pipeline.

## Prerequisites

1. **API** running: `dotnet run --project TriBalance.Api` (port 5211)
2. **Worker** running: `cd apps/worker-python && source venv/bin/activate && uvicorn app.main:app --reload --port 8000`
3. **Angular** running: `cd apps/web-angular && npm start` (port 4200)
4. Key Vault URI configured (`appsettings.Development.json`) and `az login` successful
5. `.env` (worker) has `AZURE_OPENAI_KEY`, `SERVICEBUS_CONNECTION_STRING`, `COSMOS_CONNECTION_STRING`, `APPLICATIONINSIGHTS_CONNECTION_STRING`

Open http://localhost:4200, create an engagement named e.g. "Scenario Tests",
click into it.

---

## Scenario 1 — Normal, Balanced

**Goal**: CSV upload → classification completes → Balanced ✅ + summary rendered.

1. Drag `docs/sample-trial-balance.csv` onto the upload zone.
2. Observe the automatic navigation to the validation dashboard.
3. Status badge progresses: `Queued` → `Processing` → `Completed` (within ~5s for 10 rows).
4. Verify:
   - **Balance card** shows "Balanced ✓" and variance close to 0
   - Total debits and credits both = 220000.00
   - Five account-type cards (Asset, Liability, Equity, Revenue, Expense)
     show non-zero counts
   - Classifications table lists all 10 rows with ≥90% confidence
   - Flagged items section is empty (or only shows LLM-judgment flags if any)

**What this verifies**: API → Service Bus → Worker → Azure OpenAI → Cosmos →
Service Bus result → API → SignalR → dashboard end-to-end on the happy path.

---

## Scenario 2 — Anomaly Detection

**Goal**: Flag negative-balance Asset, Unclassified accounts, low-confidence
accounts; auditor can triage from the dashboard.

1. Drag `docs/sample-trial-balance-anomalies.csv`.
2. Wait for Completed.
3. Verify flagged items includes:
   - **`negative_balance`** — Cash (code 1000) has credit > debit, balance is
     negative. Rule-based flag fires deterministically regardless of LLM.
   - **`unclassified`** — Account codes 9999 (Misc Legacy Placeholder) and 9998
     (ZZZ Temp Bucket). LLM should return `Unclassified` for these;
     deterministic post-processing adds the `unclassified` flag.
   - **`low_confidence`** — Likely on the 9999/9998 rows if LLM confidence drops
     below 0.7.
4. Filter the flagged items by type (e.g. `Unclassified`) to show only those
   rows — dashboard UX is designed so the auditor can jump straight to the
   items needing manual review.
5. Check the affected table rows are highlighted (amber row background).

**What this verifies**:
- Worker's deterministic post-processing (`_deterministic_flags`) runs independent
  of LLM output
- Cross-layer `isBalanced` is consistent — anomalies CSV is still balanced
  (debits = credits = 180000), so only per-row flags fire
- Dashboard filter/sort logic works

---

## Scenario 3 — Failure + Retry

**Goal**: Worker error propagates to the dashboard as `Failed ❌` with an error
message; Retry creates a new job that succeeds.

### 3a. Force the failure

Stop the worker. Edit `apps/worker-python/.env` and blank `AZURE_OPENAI_KEY=`
(or set it to an obviously wrong value). Restart the worker.

```bash
cd apps/worker-python
# Ctrl-C the running worker
source venv/bin/activate
uvicorn app.main:app --reload --port 8000
```

### 3b. Trigger validation

Upload `docs/sample-trial-balance.csv` (or re-validate an existing trial balance
from the engagement detail page).

1. Dashboard should briefly show `Queued` → `Processing`, then flip to `Failed`
   with the underlying error (OpenAI auth / key / etc. surfaced via the
   `errorMessage` field).
2. Verify the "Retry validation" button is visible.

### 3c. Fix and retry

1. Restore `AZURE_OPENAI_KEY` in `.env` to the real value.
2. Restart the worker.
3. Click **Retry validation** on the dashboard.
4. Status should progress Queued → Processing → Completed.
5. Confirm `/api/engagements/{id}/status` returns the **new** job (different Id
   from the failed one) — each retry creates a fresh `validation_jobs` row,
   matching Scenario 3's "retry = new attempt" semantics.

**What this verifies**:
- Worker exception path publishes `status: failed` with error message
  (`servicebus_service.publish_result`)
- `.NET` `ValidationResultConsumer` + `ApplyValidationResultCommand` translate
  the failure into a `ValidationJob.MarkFailed` state transition
- SignalR reflects the transition in real time
- Retry creates a new job; old failed job is preserved in Postgres for audit

---

## Post-run observability check

If `APPLICATIONINSIGHTS_CONNECTION_STRING` is set on both services, open
Application Insights → Transaction search:

- `Request`: `POST /api/engagements/{id}/validate`
- `Dependency`: `openai.classify` (worker span with deployment/batch size attrs)
- `Trace`: `{CommandName} handled in {ms}` — emitted by `LoggingBehavior`
- `Exception` (Scenario 3): worker stack trace with `validationJobId` correlation

Latency breakdown should show: API request time ≈ Postgres INSERT + Service
Bus publish; most of the total runtime comes from the worker's
`openai.classify` span.
