using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace TriBalance.Infrastructure.Persistence.PostgreSQL;

/// <summary>
/// Forces every DateTime written to Postgres into UTC and every DateTime read back
/// into Kind=Utc. Necessary because Postgres' `timestamp with time zone` column
/// rejects DateTime.Kind=Unspecified (e.g. when ASP.NET JSON binding produces one
/// from a bare "2024-12-31" payload).
///
/// Applied globally via ConfigureConventions so every DateTime property across
/// every entity is normalized — no per-entity opt-in, no risk of forgetting on
/// new entities.
/// </summary>
public sealed class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    public UtcDateTimeConverter()
        : base(
            v => v.Kind == DateTimeKind.Utc ? v : DateTime.SpecifyKind(v, DateTimeKind.Utc),
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
    {
    }
}

public sealed class NullableUtcDateTimeConverter : ValueConverter<DateTime?, DateTime?>
{
    public NullableUtcDateTimeConverter()
        : base(
            v => v.HasValue
                ? (v.Value.Kind == DateTimeKind.Utc ? v : DateTime.SpecifyKind(v.Value, DateTimeKind.Utc))
                : v,
            v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v)
    {
    }
}
