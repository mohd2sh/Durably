namespace Durably.Engine;
/// <summary>Derives stable persisted flow identifiers from CLR types.</summary>
internal static class FlowIdentity
{
    public static string FromType(Type type) => type.FullName ?? type.Name;

    public static string ForState<TState>() => FromType(typeof(TState));

    public static string ForFlow<TFlow>() => FromType(typeof(TFlow));
}
