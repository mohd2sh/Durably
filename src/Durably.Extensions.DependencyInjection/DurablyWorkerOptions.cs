namespace Durably;

public sealed class DurablyWorkerOptions
{
    public bool Enabled { get; set; } = true;

    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(2);

    public int BatchSize { get; set; } = 16;

    public int MaxDegreeOfParallelism { get; set; } = 4;

    public TimeSpan LeaseDuration { get; set; } = TimeSpan.FromSeconds(30);

    public string? RunnerId { get; set; }
}
