namespace Durably.E2E.Tests.Models;

public sealed class BranchState
{
    public bool Flag { get; set; }

    public string Kind { get; set; } = string.Empty;

    public string Path { get; set; } = string.Empty;
}
