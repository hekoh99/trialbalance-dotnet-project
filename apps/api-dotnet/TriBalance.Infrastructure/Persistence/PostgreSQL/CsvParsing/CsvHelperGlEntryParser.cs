using System.Globalization;
using CsvHelper;
using TriBalance.Application.Engagements;

namespace TriBalance.Infrastructure.Persistence.PostgreSQL.CsvParsing;

/// <summary>
/// Infrastructure-side adapter that fulfils the Application-layer
/// IGlEntryCsvParser contract using CsvHelper. Keeps CsvHelper's types out of
/// Application so the handler stays portable.
/// </summary>
public sealed class CsvHelperGlEntryParser : IGlEntryCsvParser
{
    public IReadOnlyList<ParsedGlEntry> Parse(Stream csvStream)
    {
        using var reader = new StreamReader(csvStream);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        csv.Context.RegisterClassMap<GlEntryCsvMap>();

        return csv.GetRecords<GlEntryCsvRecord>()
            .Select(r => new ParsedGlEntry(r.AccountCode, r.AccountName, r.Debit, r.Credit))
            .ToList();
    }
}
