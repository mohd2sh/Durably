namespace Durably.Core.UnitTests;

public sealed class BranchState
{
    public string Kind { get; set; } = string.Empty;
    public bool Flag { get; set; }
    public string Path { get; set; } = string.Empty;
}
