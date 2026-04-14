using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace TriBalance.Infrastructure.Persistence.PostgreSQL.CsvParsing;

public class GlEntryCsvRecord
{
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
}

/// <summary>
/// Converts decimal fields that may be empty/whitespace/null into 0.
/// Accounting CSVs commonly leave Debit or Credit blank when only one side applies.
/// </summary>
internal sealed class NullableDecimalConverter : DecimalConverter
{
    public override object ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0m;
        return base.ConvertFromString(text, row, memberMapData)!;
    }
}

public sealed class GlEntryCsvMap : ClassMap<GlEntryCsvRecord>
{
    public GlEntryCsvMap()
    {
        Map(m => m.AccountCode).Name("AccountCode", "account_code", "Account Code");
        Map(m => m.AccountName).Name("AccountName", "account_name", "Account Name");
        Map(m => m.Debit).Name("Debit", "debit").TypeConverter<NullableDecimalConverter>();
        Map(m => m.Credit).Name("Credit", "credit").TypeConverter<NullableDecimalConverter>();
    }
}
