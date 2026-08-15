namespace Sample.AspNetCore.Api.Workflows.Oop.NotificationDispatch;

public sealed class NotificationDispatchState
{
    public string NotificationId { get; set; } = string.Empty;

    public string Priority { get; set; } = "normal";

    public string Channel { get; set; } = "email";

    public string Recipient { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string? DispatchedVia { get; set; }

    public bool Escalated { get; set; }

    public bool Completed { get; set; }
}
