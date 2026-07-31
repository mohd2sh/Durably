namespace Durably.TestSupport;

/// <summary>Stable identifiers used by EF integration store/query scenarios.</summary>
public static class IntegrationTestConstants
{
    public const string RunnerId = "ef-int-runner";
    public const string OtherRunnerId = "ef-int-other";
    public const string FlowName = "orders";
    public const string InstanceId = "order-1";
    public const string EmptyContextJson = "{}";
    public const string ContextWithValue = "{\"value\":1}";

    public static TimeSpan LeaseDuration => TestLimits.DefaultLeaseDuration;
}
