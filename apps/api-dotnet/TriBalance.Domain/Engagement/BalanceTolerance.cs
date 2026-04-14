namespace TriBalance.Domain.Engagement;

/// <summary>
/// Shared tolerance for debit/credit balance comparison.
/// Matches the Python Worker's balance_tolerance so both services
/// produce the same IsBalanced verdict for a given trial balance.
/// Floating-point / rounding artifacts in CSV sources require a small epsilon
/// instead of strict equality.
/// </summary>
public static class BalanceTolerance
{
    public const decimal Epsilon = 0.01m;
}
