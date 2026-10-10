# Domain Model

## Aggregates

### Engagement (Aggregate Root)
- ClientName, FiscalYearEnd
- Has many TrialBalances

### TrialBalance (Entity)
- FileName, TotalDebits, TotalCredits, IsBalanced
- Has many GLEntries

### GLEntry (Entity)
- AccountCode, AccountName, Debit, Credit, Balance

### ValidationJob (Entity)
- Status: Queued → Processing → Completed | Failed
- Links Engagement to TrialBalance for async processing
