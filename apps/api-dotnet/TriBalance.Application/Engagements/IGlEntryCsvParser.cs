namespace TriBalance.Application.Engagements;

/// <summary>
/// CSV parsing is an infrastructure concern (CsvHelper, encoding, culture),
/// but the upload command handler needs a way to turn a stream into parsed
/// rows. This seam lets the handler stay in Application while Infrastructure
/// owns the actual parsing implementation.
/// </summary>
public interface IGlEntryCsvParser
{
    IReadOnlyList<ParsedGlEntry> Parse(Stream csvStream);
}

public record ParsedGlEntry(
    string AccountCode,
    string AccountName,
    decimal Debit,
    decimal Credit);
