using CsvHelper.Configuration;

namespace TriBalance.Infrastructure.Persistence.PostgreSQL.CsvParsing;

public class GlEntryCsvRecord
{
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
}

public sealed class GlEntryCsvMap : ClassMap<GlEntryCsvRecord>
{
    public GlEntryCsvMap()
    {
        Map(m => m.AccountCode).Name("AccountCode", "account_code", "Account Code");
        Map(m => m.AccountName).Name("AccountName", "account_name", "Account Name");
        Map(m => m.Debit).Name("Debit", "debit");
        Map(m => m.Credit).Name("Credit", "credit");
    }
}
