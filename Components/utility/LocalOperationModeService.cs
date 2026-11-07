namespace EEE.Components.Utility;

public static class LocalOperationModeService
{
    private const string ModeKey = "healthclique.local-operation-mode.v1";

    public static bool IsLocalEnvironment
    {
        get
        {
#if DEBUG
            return true;
#else
            return false;
#endif
        }
    }

    public static LocalOperationMode Mode
    {
        get => Preferences.Get(ModeKey, nameof(LocalOperationMode.Offline)) == nameof(LocalOperationMode.Online)
            ? LocalOperationMode.Online
            : LocalOperationMode.Offline;
        set
        {
            if (!Enum.IsDefined(value))
                throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown local operation mode.");

            Preferences.Set(ModeKey, value.ToString());
            ModeChanged?.Invoke(null, value);
        }
    }

    public static event Action<object?, LocalOperationMode>? ModeChanged;
}
