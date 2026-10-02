namespace Cashere.Services;

/// <summary>The authenticated desktop operator used to attribute local database changes.</summary>
public static class CurrentCashierContext
{
    private static readonly object Sync = new();
    private static int? _cashierId;
    private static string? _displayName;

    public static (int? CashierId, string DisplayName) Snapshot()
    {
        lock (Sync) return (_cashierId, _displayName ?? "System");
    }

    public static void Set(int cashierId, string displayName)
    {
        lock (Sync)
        {
            _cashierId = cashierId;
            _displayName = displayName;
        }
    }

    public static void Clear()
    {
        lock (Sync)
        {
            _cashierId = null;
            _displayName = null;
        }
    }
}
