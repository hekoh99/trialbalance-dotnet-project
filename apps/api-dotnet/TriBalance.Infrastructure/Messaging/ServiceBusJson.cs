using System.Text.Json;

namespace TriBalance.Infrastructure.Messaging;

/// <summary>
/// Single source of truth for Service Bus JSON options. camelCase on the wire
/// so messages round-trip cleanly with the Python Worker.
/// </summary>
public static class ServiceBusJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };
}
