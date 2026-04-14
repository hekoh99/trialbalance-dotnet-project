# Architecture

## Flow

```
CSV Upload → .NET API → PostgreSQL (GL entries)
                     → Service Bus → Python Worker → Azure OpenAI (gpt-4o-mini)
                                                   → Cosmos DB (results)
                                                   → Service Bus (completion)
                     ← SignalR ← Angular Dashboard
```

## Data Storage Strategy

| Data | Store | Reason |
|------|-------|--------|
| Engagements, Trial Balances, GL Entries | PostgreSQL | SUM/JOIN aggregation queries |
| Validation Jobs (status tracking) | PostgreSQL | State transitions, relational integrity |
| AI Classification Results | Cosmos DB | JSON-shaped, variable schema, partition by engagementId |
